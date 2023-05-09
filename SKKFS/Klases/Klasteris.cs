namespace SKKFS.Klases
{
    public class Klasteris
    {
        public Klasteris() { }
        public Klasteris (long start, long end, string nextSectorNumber)
        {
            Start = start;
            End = end;
            NextSectorNumber = nextSectorNumber;
            Length = end - start + 1;
        }
        public long Start { get; set; }
        public long End { get; set; }
        public long Length { get; set; }
        public string NextSectorNumber { get; set; }

        public override string ToString()
        {
            return $"{Start}-{End} ({Length}) -> {NextSectorNumber}";
        }
    }
}
