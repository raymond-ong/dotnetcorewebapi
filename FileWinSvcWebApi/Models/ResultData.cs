using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsaePrmDwApi.Models
{
    public class ResultData
    {
        public string HierarchyPath;
        public Dictionary<string, string> Dimensions;
        public List<KpiData> Measures;
    }
}
