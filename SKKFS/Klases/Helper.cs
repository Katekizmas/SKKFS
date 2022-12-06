namespace SKKFS.Klases
{
    static class Helper
    {

        //Nuskaityti tekstą į binary string
        public static string ToBinaryString(this string str)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            return ConvertToBinaryString(bytes);
        }

        //Nuskaityti failą į binary string
        public static string ReadFileToBinaryString(string filename)
        {
            byte[] fileBytes = File.ReadAllBytes(filename);
            return ConvertToBinaryString(fileBytes);
        }

        //Išsaugoti bitų stringą į failą
        public static void WriteBinaryStringToFile(string path, string fileName, string binaryString)
        {
            File.WriteAllBytes($"{path}/{fileName}", FromBinaryString(binaryString));
        }
        
        // Paversti baitų masyvą į bitų stringą
        public static string ConvertToBinaryString(byte[] byteArray)
        {
            return string.Join("", byteArray.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        // Paversti bitų stringą į baitų masyvą
        public static byte[] FromBinaryString(string binaryString)
        {
            int count = binaryString.Length / 8;
            var b = new byte[count];
            for (int i = 0; i < count; i++)
                b[i] = Convert.ToByte(binaryString.Substring(i * 8, 8), 2);

            return b;
        }
        public static long Modulo(long x, long N)
        {
            return (x % N + N) % N;
        }

        public static bool IsWithin(this long value, long minimum, long maximum)
        {
            return value >= minimum && value <= maximum;
        }
    }
}
