using System.Drawing;

namespace Hyphaeizer
{
    public static class Util
    {
        public static double GetLuminance(Color color) => GetLuminance(color.R, color.G, color.B);
        public static double GetLuminance(byte R, byte G, byte B) => (R * 0.3) + (G * 0.59) + (B * 0.11);

        public static int SumByteArray(byte[] arr, int begin = 0, int end = -1)
        {
            if (end == -1) end = arr.Length - 1;
            int sum = 0;
            for (int i = begin; i < end; i++)
                sum += arr[i];
            return sum;
        }
    }
}
