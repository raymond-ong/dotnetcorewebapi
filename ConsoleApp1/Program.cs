using Accessors;
using System;
using IsaePrmDwApi.Models;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            IsaeDwAccessor accessor = new IsaeDwAccessor();
            accessor.GenerateDummyVendorStatusData(new RequestData() { RequestParams  = new List<RequestParam>()});
        }
    }
}
