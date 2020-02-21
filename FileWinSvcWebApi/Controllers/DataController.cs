using Accessors;
using IsaePrmDwApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IsaePrmDwApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        //[HttpPost]
        ////public List<Dictionary<string, object>> Post(RequestData request)
        //public List<Dictionary<string, object>> Post()
        //{
        //    IsaeDwAccessor accessor = new IsaeDwAccessor("192.168.56.130\\ISAESQLSERVER");
        //    Console.WriteLine("Post");
        //    List<Dictionary<string, object>> retList = accessor.queryData(null);

        //    return retList;
        //}

        [HttpPost]
        public Dictionary<string, object> Post(RequestData requestData)
        {
            IsaeDwAccessor accessor = new IsaeDwAccessor("192.168.56.130\\ISAESQLSERVER");
            Console.WriteLine("Post");
            Dictionary<string, object> retDict = accessor.queryData(requestData);
            Thread.Sleep(300);
            return retDict;
        }
    }
}
