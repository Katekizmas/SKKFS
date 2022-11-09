using System;
namespace SKKFS.Klases
{
    public class Failas
    {
        public Failas(){ }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string LastAccessed { get; set; }
        public string FileCreated { get; set; }
        public string LastModified { get; set; }
        public long Size { get; set; }
        public long Address { get; set; } // inode numeris
        public string FullPath { get; set; }

        public long[] Sectors { get; set; }

        public List<Klasteris> Clusters { get; set; } = new List<Klasteris>();
    }
}
