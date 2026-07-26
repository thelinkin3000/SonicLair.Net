using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicLair.Cli.Services
{
    internal static class MpvNative
    {
        private const string LibName = "mpv";
        public const int MPV_FORMAT_STRING = 1;

        public enum MpvEventId
        {
            None = 0,
            Shutdown = 1,
            EndFile = 7,
            PropertyChange = 22,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEvent
        {
            public int EventId;
            public int Error;
            public ulong ReplyUserdata;
            public IntPtr Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEventProperty
        {
            public IntPtr Name;
            public int Format;
            public IntPtr Data;
        }

        static MpvNative()
        {
            NativeLibrary.SetDllImportResolver(typeof(MpvNative).Assembly, Resolve);
        }

        private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (name != LibName)
            {
                return IntPtr.Zero;
            }
            foreach (var candidate in new[] { "libmpv.so.2", "libmpv.so.1", "libmpv.so", "mpv-2.dll", "libmpv.2.dylib", "libmpv.dylib" })
            {
                if (NativeLibrary.TryLoad(candidate, out var handle))
                {
                    return handle;
                }
            }
            return IntPtr.Zero;
        }

        // mpv's option/number parsing assumes the C locale; if the process locale uses e.g. a comma
        // decimal point, mpv_create() returns NULL. Embedders are expected to force this themselves.
        private const int LC_NUMERIC_LINUX = 1;

        [DllImport("libc", EntryPoint = "setlocale")]
        private static extern IntPtr setlocale_libc(int category, string locale);

        public static void EnsureCNumericLocale()
        {
            if (OperatingSystem.IsLinux())
            {
                setlocale_libc(LC_NUMERIC_LINUX, "C");
            }
        }

        [DllImport(LibName)]
        public static extern IntPtr mpv_create();

        [DllImport(LibName)]
        public static extern int mpv_initialize(IntPtr ctx);

        [DllImport(LibName)]
        private static extern int mpv_set_option_string(IntPtr ctx, byte[] name, byte[] data);

        [DllImport(LibName)]
        private static extern int mpv_set_property_string(IntPtr ctx, byte[] name, byte[] data);

        [DllImport(LibName)]
        private static extern IntPtr mpv_get_property_string(IntPtr ctx, byte[] name);

        [DllImport(LibName)]
        private static extern void mpv_free(IntPtr data);

        [DllImport(LibName)]
        private static extern int mpv_command(IntPtr ctx, IntPtr args);

        [DllImport(LibName)]
        public static extern int mpv_observe_property(IntPtr ctx, ulong replyUserdata, byte[] name, int format);

        [DllImport(LibName)]
        public static extern IntPtr mpv_wait_event(IntPtr ctx, double timeout);

        [DllImport(LibName)]
        public static extern void mpv_terminate_destroy(IntPtr ctx);

        [DllImport(LibName)]
        private static extern IntPtr mpv_error_string(int error);

        private static byte[] Utf8Z(string s) => Encoding.UTF8.GetBytes(s + "\0");

        public static string GetErrorString(int error) =>
            Marshal.PtrToStringAnsi(mpv_error_string(error)) ?? $"mpv error {error}";

        public static void SetOptionString(IntPtr ctx, string name, string value)
        {
            var result = mpv_set_option_string(ctx, Utf8Z(name), Utf8Z(value));
            if (result < 0)
            {
                throw new InvalidOperationException($"mpv_set_option_string({name}={value}) failed: {GetErrorString(result)}");
            }
        }

        public static void SetPropertyString(IntPtr ctx, string name, string value)
        {
            mpv_set_property_string(ctx, Utf8Z(name), Utf8Z(value));
        }

        public static string? GetPropertyString(IntPtr ctx, string name)
        {
            var ptr = mpv_get_property_string(ctx, Utf8Z(name));
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            var value = Marshal.PtrToStringAnsi(ptr);
            mpv_free(ptr);
            return value;
        }

        public static double GetPropertyDouble(IntPtr ctx, string name, double fallback = 0)
        {
            var s = GetPropertyString(ctx, name);
            return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        public static void ObserveProperty(IntPtr ctx, ulong replyUserdata, string name)
        {
            mpv_observe_property(ctx, replyUserdata, Utf8Z(name), MPV_FORMAT_STRING);
        }

        public static void Command(IntPtr ctx, params string[] args)
        {
            var ptrs = new IntPtr[args.Length + 1];
            for (var i = 0; i < args.Length; i++)
            {
                ptrs[i] = Marshal.StringToHGlobalAnsi(args[i]);
            }
            ptrs[args.Length] = IntPtr.Zero;

            var arr = Marshal.AllocHGlobal(IntPtr.Size * ptrs.Length);
            try
            {
                for (var i = 0; i < ptrs.Length; i++)
                {
                    Marshal.WriteIntPtr(arr, i * IntPtr.Size, ptrs[i]);
                }
                mpv_command(ctx, arr);
            }
            finally
            {
                Marshal.FreeHGlobal(arr);
                for (var i = 0; i < args.Length; i++)
                {
                    Marshal.FreeHGlobal(ptrs[i]);
                }
            }
        }

        public static string? ReadObservedString(MpvEventProperty prop)
        {
            if (prop.Format != MPV_FORMAT_STRING || prop.Data == IntPtr.Zero)
            {
                return null;
            }
            var strPtr = Marshal.ReadIntPtr(prop.Data);
            return strPtr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(strPtr);
        }
    }
}
