using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4net.Redis
{
    internal class Config
    {
        public string Hosts { get; set; }
        public string Password { get; set; }
        public string Key { get; set; }
        public int Period { get; set; }
        public int BatchSize { get; set; }
        public int MaxBatchPeriod { get; set; }
        public bool PurgeOnConnectionFailure { get; set; }
    }
}
