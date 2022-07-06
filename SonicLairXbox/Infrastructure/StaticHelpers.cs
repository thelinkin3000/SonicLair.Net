using Microsoft.Toolkit.Uwp.Notifications;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace SonicLairXbox.Infrastructure
{
    public class StaticHelpers
    {
        protected StaticHelpers() { }
        public static void ShowError(string message)
        {
            new ToastContentBuilder()
                .AddText(message)
                .Show();
        }
        public static Tuple<int, int> GetRowCol(int index, List<int> sizes)
        {
            int i = 0;
            while (sizes[i] <= index)
            {
                i++;
            }
            var row = i;
            var col = index - ((i == 0) ? 0 : sizes[i - 1]);
            return new Tuple<int, int>(row, col);
        }
        public static BitmapImage GetFromBitmapImage(Bitmap image)
        {
            using (var memory = new MemoryStream())
            {
                image.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.SetSource(memory.AsRandomAccessStream());
                return bitmapImage;
            }
        }
        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            };

            return new JsonSerializerSettings()
            {
                ContractResolver = contractResolver,
                Culture = new System.Globalization.CultureInfo("en-US")
            };

        }
        public static string GetLocalIp()
        {
            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null
                    && localHostName.Type == HostNameType.Ipv4
                    && (localHostName.ToString().StartsWith("192.168")
                    || localHostName.ToString().StartsWith("10.")
                    || localHostName.ToString().StartsWith("172.")))
                {
                    return localHostName.ToString();
                }
            }
            return "127.0.0.1";
        }

        public static byte[] GetLocalSubnetMask()
        {
            int subnetmask = 0;
            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null
                    && localHostName.Type == HostNameType.Ipv4
                    && (localHostName.ToString().StartsWith("192.168")
                    || localHostName.ToString().StartsWith("10.")
                    || localHostName.ToString().StartsWith("172.")))
                {
                    subnetmask = (int)localHostName.IPInformation.PrefixLength;
                    break;
                }
            }
            var ret = new byte[4];
            for (int i = 0; i < ret.Length; i++)
            {
                if (subnetmask >= 8)
                {
                    ret[i] = (byte)255;
                }
                else
                {
                    switch (subnetmask)
                    {
                        case 0:
                            ret[i] = (byte)0;
                            break;
                        case 1:
                            ret[i] = (byte)128;
                            break;
                        case 2:
                            ret[i] = (byte)192;
                            break;
                        case 3:
                            ret[i] = (byte)224;
                            break;
                        case 4:
                            ret[i] = (byte)240;
                            break;
                        case 5:
                            ret[i] = (byte)248;
                            break;
                        case 6:
                            ret[i] = (byte)252;
                            break;
                        case 7:
                            ret[i] = (byte)254;
                            break;
                        default:
                            ret[i] = (byte)0;
                            break;
                    }
                }
                subnetmask -= 8;
            }
            return ret;
        }

        public static string GetLocalBroadcastIp()
        {
            byte[] ipAdressBytes = GetLocalIpAsArray();
            byte[] subnetMaskBytes = GetLocalSubnetMask();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            StringBuilder sb = new StringBuilder();
            foreach (var b in broadcastAddress)
            {
                sb.Append(((int)b).ToString());
                sb.Append(".");

            }
            var ret = sb.ToString();
            return ret.Substring(0, ret.Length - 1);
        }

        public static byte[] GetLocalIpAsArray()
        {
            var strings = GetLocalIp().Split(".");
            if (strings.Length != 4)
            {
                return new byte[0];
            }
            var ret = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = (byte)int.Parse(strings[i]);
            }
            return ret;
        }
        public static async Task GoToList(List<ListViewBase> listviews, ScrollViewer sv, ListViewBase from, ListViewBase target)
        {
            target.Focus(FocusState.Keyboard);
            object nextItem;
            if (from != null)
            {
                nextItem = target.Items[Math.Clamp(Math.Min(from.SelectedIndex, target.Items.Count - 1), 0, 50)];
                from.SelectedIndex = -1;
            }
            else
            {
                nextItem = target.Items[0];
            }
            await target.ScrollToItem(nextItem);
            Debug.WriteLine(listviews.IndexOf(target) * sv.ExtentHeight / listviews.Count);
            sv.ChangeView(0, listviews.IndexOf(target) * sv.ExtentHeight / listviews.Count, 1);
            target.SelectedItem = nextItem;
        }


        public static void GoToSidebar(ListViewBase from)
        {
            from.SelectedIndex = -1;
            ((App)App.Current).NotifyObservers("focusSidebar");
        }

        public static ListViewBase ListViewKeyDown(object sender, KeyRoutedEventArgs e, List<ListViewBase> listviews, ScrollViewer sv, Control controlTop = null, Control controlBottom = null)
        {
            Debug.WriteLine(e.Key.ToString());
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Down:
                    if (listviews.IndexOf((ListViewBase)sender) < listviews.Count - 1)
                    {
                        _ = StaticHelpers.GoToList(listviews,
                            sv,
                            listviews[listviews.IndexOf((ListViewBase)sender)],
                            listviews[listviews.IndexOf((ListViewBase)sender) + 1]);
                        return listviews[listviews.IndexOf((ListViewBase)sender) + 1];
                    }
                    else if (controlBottom != null)
                    {
                        controlBottom.Focus(FocusState.Keyboard);
                        listviews[listviews.IndexOf((ListViewBase)sender)].SelectedIndex = -1;
                    }
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Up:
                    if (listviews.IndexOf((ListViewBase)sender) == 0 && controlTop != null)
                    {
                        controlTop.Focus(FocusState.Keyboard);
                        listviews[listviews.IndexOf((ListViewBase)sender)].SelectedIndex = -1;
                        e.Handled = true;
                        break;
                    }
                    _ = StaticHelpers.GoToList(listviews,
                        sv,
                        listviews[listviews.IndexOf((ListViewBase)sender)],
                        listviews[listviews.IndexOf((ListViewBase)sender) - 1]);
                    return listviews[listviews.IndexOf((ListViewBase)sender) - 1];
                case Windows.System.VirtualKey.Left:
                    if (((ListViewBase)sender).SelectedIndex == 0)
                    {
                        StaticHelpers.GoToSidebar((ListViewBase)sender);
                        return (ListViewBase)sender;
                    }
                    e.Handled = true;
                    break;
                default:
                    return null;
            }
            return null;
        }
    }
}
