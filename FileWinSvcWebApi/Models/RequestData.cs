﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsaePrmDwApi.Models
{
    public class RequestData
    {
        public string RequestType { get; set; }

        public List<string> Groupings { get; set; }

        public string Granularity { get; set; }

        // Filters
        public List<RequestParam> RequestParams { get; set; }

        // For pagination
        public int startIndex { get; set; }

        public int size { get; set; }

    }
}
