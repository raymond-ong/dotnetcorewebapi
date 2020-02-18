using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsaePrmDwApi.Models
{
    public class LayoutData
    {
        public string Name { get; set; }
        
        public string LayoutJson { get; set; }

        public DateTime LastUpdateDate { get; set; }

        public short NumRows { get; set; }

        public short NumCols { get; set; }
        //public int Revision { get; set; }

    }
}
