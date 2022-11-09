using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKKFS.Klases
{
    public class FailuSistema
    {
        public FailuSistema() { }
        public FailuSistema(long sectorSize, long clusterSize)
        {
            SectorSize = sectorSize;
            ClusterSize = clusterSize;
            SectorsAssignedPerCluster = clusterSize / sectorSize;
        }

        public long SectorSize { get; set; }
        public long ClusterSize { get; set; }
        public long SectorsAssignedPerCluster { get; set; }
        public long NextFreeSector { get; set; }
        public long FreeSectorCount { get; set; }
        public string VolumeId { get; set; }
        public string VolumeLabel { get; set; }
        public long ClusterAreaStart { get; set; }
        public long ClusterAreaEnd { get; set; }

        public List<Klasteris> Clusters { get; set; } = new List<Klasteris>();
    }
}