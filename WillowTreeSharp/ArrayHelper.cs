using System;

namespace WillowTree
{
    public static class ArrayHelper
    {
        public static void ResizeArrayLarger(ref string[,] Input, int rows, int cols)
        {
            string[,] newArray = new string[rows, cols];
            Array.Copy(Input, newArray, Input.Length);
            Input = newArray;
        }

        public static void ResizeArraySmaller(ref string[,] Input, int rows, int cols)
        {
            string[,] newArray = new string[rows, cols];
            Array.Copy(Input, 0, newArray, 0, (long)(rows * cols));
            Input = newArray;
        }

        public static void ResizeArrayLarger(ref string[] Input, int rows)
        {
            string[] newArray = new string[rows];
            Array.Copy(Input, newArray, Input.Length);
            Input = newArray;
        }

        public static void ResizeArraySmaller(ref string[] Input, int rows)
        {
            string[] newArray = new string[rows];
            Array.Copy(Input, 0, newArray, 0, (long)rows);
            Input = newArray;
        }

        public static void ResizeArrayLarger(ref int[,] Input, int rows, int cols)
        {
            int[,] newArray = new int[rows, cols];
            Array.Copy(Input, newArray, Input.Length);
            Input = newArray;
        }

        public static void ResizeArraySmaller(ref int[,] Input, int rows, int cols)
        {
            int[,] newArray = new int[rows, cols];
            Array.Copy(Input, 0, newArray, 0, (long)((rows) * cols));
            Input = newArray;
        }

        public static void ResizeArrayLarger(ref int[] Input, int rows)
        {
            int[] newArray = new int[rows];
            Array.Copy(Input, newArray, Input.Length);
            Input = newArray;
        }

        public static void ResizeArraySmaller(ref int[] Input, int rows)
        {
            int[] newArray = new int[rows];
            Array.Copy(Input, 0, newArray, 0, (long)rows);
            Input = newArray;
        }

        public static void ResizeArrayLarger(ref float[] Input, int rows)
        {
            float[] newArray = new float[rows];
            Array.Copy(Input, newArray, Input.Length);
            Input = newArray;
        }

        public static void ResizeArraySmaller(ref float[] Input, int rows)
        {
            float[] newArray = new float[rows];
            Array.Copy(Input, 0, newArray, 0, (long)rows);
            Input = newArray;
        }

        public static bool CheckIfNull(string[] Array)
        {
            try
            {
                if (Array.Length > 0)
                    return false;
                else
                    return true;
            }
            catch { return true; }
        }

        public static int IndexOf(this string[] Array, string value)
        {
            int count = Array.Length;
            for (int i = 0; i < count; i++)
            {
                if (Array[i] == value)
                    return i;
            }

            return -1;
        }
    }
}
