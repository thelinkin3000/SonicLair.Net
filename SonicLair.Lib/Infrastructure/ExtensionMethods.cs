using System;
using System.Collections.Generic;

namespace SonicLair.Lib.Infrastructure
{
    public static class ExtensionMethods
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string GetAsMMSS(this int duration)
        {
            if (duration < 3600)
            {
                return $"{Math.Floor(duration / 60d):00}:{duration % 60:00}";
            }
            else
            {
                return $"{Math.Floor(duration / 3600d):00}:{Math.Floor((duration % 3600d) / 60d):00}:{(duration % 3600d) % 60:00}";
            }
        }

        public static int Clamp(this int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

        //This is wrong.
        public static float Clamp(this float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }
}