using System;
using System.IO;

namespace SonicLair.Infrastructure
{
    public static class Const
    {
        public static string ConfigFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SonicLair");
    }
}