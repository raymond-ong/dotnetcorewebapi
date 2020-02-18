using Accessors;
using IsaePrmDwApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsaePrmDwApi.Controllers
{
    // For returning the metadata needed for configuring the data of controls
    [Route("api/[controller]")]
    [ApiController]
    public class MetaController : ControllerBase
    {
        [HttpPost]
        public ActionResult<IEnumerable<ResultData>> Post(RequestData request)
        {
            IsaeDwAccessor accessor = new IsaeDwAccessor("localhost");
            Console.WriteLine("Post");
            //List<ResultData> retData = accessor.queryData(request);

            return null;
        }
    }
}
