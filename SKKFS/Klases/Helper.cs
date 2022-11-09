namespace SKKFS.Klases
{
    static class Helper
    {
        public static string ToBinaryString(this string str)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            return string.Join(" ", bytes.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }
    }
}
