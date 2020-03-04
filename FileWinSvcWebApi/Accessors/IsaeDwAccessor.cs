using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using IsaePrmDwApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Accessors
{
    public class IsaeDwAccessor : IDisposable
    {
        //private const string CONNECTION_STRING = @"server='{0}\ISAESQLSERVER';Initial Catalog=IsaeDw;user ID='IsaeDw';password='IsaeDw'";
        private const string CONNECTION_STRING = @"server='{0}';Initial Catalog=IsaeDw;user ID='IsaeDw';password='IsaeDw'";

        private SqlConnection connection;
        private bool disposed = false;


        //private string getDimColumnsCsv(RequestData request, string prefix=null)
        //{
        //    if (request.Dimensions == null || request.Dimensions.Count == 0)
        //    {
        //        return null;
        //    }

        //    if (string.IsNullOrEmpty(prefix))
        //    {
        //        return string.Join(",", request.Dimensions);
        //    }
        //    return string.Join(",", $"{prefix}.{request.Dimensions}" );
        //}

        //internal List<ResultData> queryDataOld(RequestData request)
        //{
            //string paramDimensionColumns = getDimColumnsCsv(request);
            //string paramKpiColumns = getDimColumnsCsv(request, null);

            //string sqlQuery = @"DECLARE  @LatestCollectionKey INT = (select top 1 CollectionDateKey from FactDataCollection order by CollectionDateKey desc);" +
            //                "select d.HierarchyPath ,d.KpiStatus, d.KpiStatusValue, d.KpiValue," +
            //                $"{ }" + 
            //                "k.Name as KpiName, " +
            //                "kg.Name as KpiGroupName " +
            //                "JOIN DimHierarchy h ON h.Fullpath = d.HierarchyPath " +
            //                "where d.CollectionDateKey = @LatestCollectionKey " +
            //                "AND h.CollectionDateKey = @LatestCollectionKey " +
            //                "AND h.";
            //throw new NotImplementedException();
        //}

        public List<Dictionary<string, object>> queryDataOld2()
        {
            // Just want to test if it's possible to return proper JSON data using Dictionary, without declaring a class.
            List<string> Dims = new List<string>() { "deviceId", "vendor", "model", "ODE" };
            Dictionary<string, List<string>> VendorModels = new Dictionary<string, List<string>>()
            {
                {"Yokogawa", new List<string>() { "EJA", "EJX", "YVP", "YTA"} },
                {"Apple", new List<string>() { "iPhone", "Macbook", "iPad", "iMac", "Watch"} },
                {"Samsung", new List<string>() { "Galaxy S", "Galaxy Note", "Galaxy Tab", } },
                {"Huawei", new List<string>() { "P30", "Mate 30", } },
                {"Xiaomi", new List<string>() { "Mi 9", "Redmi", "Mi Box", } },
            };

            List<Dictionary<string, object>> retList = new List<Dictionary<string, object>>();

            for (int iVendor = 0; iVendor < VendorModels.Count; iVendor++)
            {
                var Models = VendorModels.ElementAt(iVendor).Value;
                for (int iModel = 0; iModel < Models.Count; iModel++)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dictionary<string, object> item = new Dictionary<string, object>()
                        {
                            { Dims.ElementAt(0), $"DeviceId_{i}" },
                            { Dims.ElementAt(1), $"{VendorModels.ElementAt(iVendor).Key}" },
                            { Dims.ElementAt(2), $"{Models.ElementAt(iModel)}" },
                            { Dims.ElementAt(3), i * 3.14159 },
                            // For Hierarchical data
                            // Option 1: Using Anonymous types
                            // Disadvantages: 
                            // 1. The property name must be known during compile time
                            // 2. In the actual API, the first letter is changed to lowercase (e.g. "Plant" becomes "plant")
                            { "PlantHierarchy", new {Plant="Plant1", SiteX="Site1", Area="Areal", Unit="Unit1"} },
                            // Option 2: Using Dictionary (this is better)
                            { "NetworkHierarchy", new Dictionary<string, object>() { {"FCS", "FCS01" }, { "Node", "Node01"}, { "Slot", "Slot01"} } },                            
                        };

                        retList.Add(item);
                    }
                }
            }

            return retList;
        }

        public Dictionary<string, object> GetDeviceDetails(RequestData request)
        {
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            List<Dictionary<string, object>> retList = new List<Dictionary<string, object>>();
            // Not a good design...anyways this is just a temp solution
            // Ideal approach: bottom up (generate devices first and decide the props while generating)
            // Current approach: decide the number of counts first per vendor/model/status, then generate the devices
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();
            Dictionary<string, string> modelCommTypeLookup = GetModelCommTypeLookup();
            Dictionary<string, string> modelCategoryLookup = GetModelCategoryLookup();
            string[] priorities = new string[] { "Low", "Medium", "High", "High+" };
            string[] plants = new string[] { "PRM001" };
            string[] areas = new string[] { "Area001", "Area002", "Area003", "Area004" };
            string[] units = new string[] { "Unit001", "Unit002", "Unit003", "Unit004", "Unit005", "Unit006", "Unit007", "Unit008" };

            var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
            var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
            var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");

            foreach (var kvpModelStatus in modelStatusCount)
            {
                string model = kvpModelStatus.Key;
                string vendor = VendorModels.First(kvp => kvp.Value.Contains(model)).Key;
                
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }
                
                if (modelFilter != null && modelFilter.Value != model)
                {
                    continue;
                }

                var statusCountDict = kvpModelStatus.Value;
                foreach(var kvpStatusCount in statusCountDict)
                {
                    string statusName = kvpStatusCount.Key;
                    int count = kvpStatusCount.Value;

                    
                    if (statusFilter != null && statusFilter.Value != statusName)
                    {
                        continue;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        Dictionary<string, object> item = new Dictionary<string, object>();

                        AddItem(request.Columns, item, "DeviceId", $"DeviceId_{ i}");
                        AddItem(request.Columns, item, "DeviceTag", $"DeviceTag_{i}");
                        AddItem(request.Columns, item, "CommType", modelCommTypeLookup[model]);
                        AddItem(request.Columns, item, "DevicePath", $"MYPJT-0101-10111-{i}");
                        AddItem(request.Columns, item, "Category", modelCategoryLookup[model]);
                        AddItem(request.Columns, item, "Priority", priorities[i % priorities.Length]);
                        AddItem(request.Columns, item, "Vendor", vendor);
                        AddItem(request.Columns, item, "Model", model);
                        AddItem(request.Columns, item, "Revision", "1");
                        AddItem(request.Columns, item, "Plant", plants[i % plants.Length]);
                        AddItem(request.Columns, item, "Area", areas[i % areas.Length]);
                        AddItem(request.Columns, item, "Unit", units[i % units.Length]);
                        AddItem(request.Columns, item, "PRM Device Status", statusName);

                        retList.Add(item);
                    }
                }
            }

            retDict["data"] = retList;
            return retDict;
        }

        private void AddItem(List<string> columns, Dictionary<string, object> item, string key, object value)
        {
            if (!columns.Contains(key))
            {
                return;
            }

            item.Add(key, value);
        }

        public Dictionary<string, object> queryData(RequestData request)
        {
            if (request.RequestType == "GetDeviceDetails")
            {
                return GetDeviceDetails(request);
            }
            else if (request.RequestType == "GetComments")
            {
                return GetAlarmComments(request);
            }
            else if (request.RequestType == "GetPlantKpi")
            {
                return GetDummyImageMapData(request);
            }
            else // GetDeviceCounts for now
            {
                // TODO: The pre-grouped data must be flat in order to group it easily
                // For now, just hardcode each scenario since this is just a temp solution.
                if (request.Groupings.Count == 1)
                {
                    //return GenerateDataByVendor(request);
                    if (request.Groupings[0] == "Vendor")
                    {
                        //return GenerateDataFor1Group(request);
                        return GenerateDataByVendor(request);
                    }
                    else
                    {
                        return GenerateDataByModel(request);
                    }
                }

                if (request.Groupings.Count == 2)
                {
                    if (request.Groupings[0] == "Vendor" && request.Groupings[1] == "Model")
                    {
                        return GenerateDummyPieChartData(request);
                    }
                    else if (request.Groupings[0] == "Vendor" && request.Groupings[1] == "PRM Device Status")
                    {
                        return GenerateDummyVendorStatusData(request);
                    }
                    else if (request.Groupings[0] == "Model" && request.Groupings[1] == "PRM Device Status")
                    {
                        return GenerateDummyModelStatusData(request);
                    }
                }

                if (request.Groupings.Count == 3)
                {
                    return GenerateDummyBarChartData(request);
                }
            }

            return null;
        }

        private Dictionary<string, object> GetAlarmComments(RequestData request)
        {
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            string firstRequestValue = request.RequestParams.Count > 0 ? request.RequestParams[0].Value : "";

            retDict["data"] = new List<string>() { string.Format("[{0} - Alarm Detail]\r\n-    The device of sensor 2 related condition is in Failure.\r\n\r\n[Possible cause]\r\n-    There is a breakage in sensor 2, or sensor 2 is disconnected from the terminals. It might affect the whole plant operation.....", 
                firstRequestValue) };
            return retDict;
        }

        private Dictionary<string, object> GetDummyImageMapData(RequestData request)
        {
            Dictionary<string, double> lookupKpis = new Dictionary<string, double>()
            {
                {"North America", 89.5 },
                {"South America", 95.5 },
                {"China", 91.5 },
                {"Japan", 90.5 },
                {"Middle East", 89.5 },
            };

            Dictionary<string, object> retDict = new Dictionary<string, object>();
            List<Dictionary<string, object>> retList = new List<Dictionary<string, object>>();
            retDict["data"] = retList;
            var hotspotsQ = request.RequestParams.Find(x => x.Name == "Hotspots");
            if (hotspotsQ == null)
            {
                return retDict;
            }

            foreach (var hotspot in hotspotsQ.Values)
            {
                foreach (var col in request.Columns)
                {
                    if (lookupKpis.ContainsKey(hotspot))
                    {
                        retList.Add(new Dictionary<string, object>() {
                            { "name", hotspot },
                            { col, lookupKpis[hotspot]}
                        });
                    }
                    else
                    {
                        retList.Add(new Dictionary<string, object>() {
                            { "name", hotspot },
                            { col, 0.0}
                        });
                    }
                }
            }

            return retDict;
        }

        private string GetDataGroupKey(RequestData request, Dictionary<string, object> data)
        {
            List<string> vals = new List<string>();
            foreach(var group in request.Groupings)
            {
                vals.Add(string.Format("{0}", data[group]));
            }

            return string.Join("/", vals);
        }

        public Dictionary<string, object> GenerateDataFor1Group(RequestData request)
        {
            Dictionary<string, object> pregroupedData = GenerateDummyBarChartData(new RequestData() { RequestParams = new List<RequestParam>() });
            List<Dictionary<string, object>> data = pregroupedData["data"] as List<Dictionary<string, object>>;
            var groupedList = data.GroupBy(d => GetDataGroupKey(request, d),
                                            d => Convert.ToInt32(d["count"]),
                                            (vendor, counts) => new {
                                                X = vendor,
                                                Count = counts.Sum(f => f)
                                            });
            foreach (var grp in groupedList)
            {
                var key = grp.X;
            }

            return null;
        }

        public Dictionary<string, object> GenerateDummyModelStatusData(RequestData request)
        {
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

            Dictionary<string, object> retDict = new Dictionary<string, object>();

            foreach (var kvp in VendorModels)
            {
                string vendor = kvp.Key;
                List<string> models = kvp.Value;                

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                foreach (string model in models)
                {
                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    foreach (var kvpStatus in statusDict)
                    {
                        string status = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != status)
                        {
                            continue;
                        }

                        int count = kvpStatus.Value;
                        dataList.Add(new Dictionary<string, object>()
                        {
                            {"Model",  model}, { "PRM Device Status", status}, { "count", count}
                        });
                    }

                }



            }

            retDict["data"] = dataList;
            return retDict;
        }
        public Dictionary<string, object> GenerateDummyVendorStatusData(RequestData request)
        {
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

            Dictionary<string, object> retDict = new Dictionary<string, object>();
            
            foreach (var kvp in VendorModels)
            {
                string vendor = kvp.Key;
                List<string> models = kvp.Value;
                Dictionary<string, int> currVendorByStatus = new Dictionary<string, int>(); // key: status, val: total

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                foreach (string model in models)
                {
                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    foreach (var kvpStatus in statusDict)
                    {
                        string status = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != status)
                        {
                            continue;
                        }

                        int count = kvpStatus.Value;
                        if (!currVendorByStatus.ContainsKey(status))
                        {
                            currVendorByStatus[status] = count;
                        }
                        else
                        {
                            currVendorByStatus[status] += count;
                        }
                    }
                }

                foreach(var kvpStatusCount in currVendorByStatus)
                {
                    dataList.Add(new Dictionary<string, object>()
                        {
                            {"Vendor",  vendor}, { "PRM Device Status", kvpStatusCount.Key}, { "count", kvpStatusCount.Value}
                        });
                }

            }

            retDict["data"] = dataList;
            return retDict;
        }

        /*
        // Attempt to use IEnumerable groupby to perform grouping
        // Prob: cannot retrieve the original group objects if need to group by multiple fields (same prob as crossfilter, but we can use JSON.parse/stringify there)
        public Dictionary<string, object> GenerateDummyVendorStatusData(RequestData request)
        {
            Dictionary<string, object> pregroupedData = GenerateDummyBarChartData(new RequestData() { RequestParams = new List<RequestParam>()});
            var x = pregroupedData["data"];
            List<Dictionary<string, object>> data = pregroupedData["data"] as List<Dictionary<string, object>>;
            var groupedList = data.GroupBy(d => string.Format("{0}/{1}", d["Vendor"], d["PRM Device Status"]),
                                            d=> Convert.ToInt32(d["count"]),
                                            (vendor, counts) => new { 
                                                X = vendor,
                                                Count = counts.Sum(f => f)
                                            });
            foreach (var grp in groupedList)
            {
                var key = grp.X;
            }

            return null;
        }
        */

        private Dictionary<string, object> GenerateDataByVendor(RequestData request)
        {
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();
            foreach (var kvp in VendorModels)
            {
                string vendor = kvp.Key;

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                int total = 0;
                List<string> models = kvp.Value;
                foreach (string model in models)
                {
                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    
                    foreach (var kvpStatus in statusDict)
                    {
                        string kpiStaus = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != kpiStaus)
                        {
                            continue;
                        }

                        total += kvpStatus.Value;
                    }
                }

                dataList.Add(new Dictionary<string, object>()
                        {
                            {"Vendor",  vendor}, { "count", total}
                        });
            }

            retDict["data"] = dataList;

            return retDict;
        }

        private Dictionary<string, object> GenerateDataByModel(RequestData request)
        {
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();
            foreach (var kvp in VendorModels)
            {
                string vendor = kvp.Key;

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                
                List<string> models = kvp.Value;
                foreach (string model in models)
                {
                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    int total = 0;
                    foreach (var kvpStatus in statusDict)
                    {
                        string kpiStaus = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != kpiStaus)
                        {
                            continue;
                        }

                        total += kvpStatus.Value;
                    }

                    dataList.Add(new Dictionary<string, object>()
                        {
                            {"Model",  model}, { "count", total}
                        });

                }

            }

            retDict["data"] = dataList;

            return retDict;
        }

        //private Dictionary<string, object> GenerateDummyPieChartDataSimple()
        //{
        //    Dictionary<string, object> retDict = new Dictionary<string, object>();

        //    retDict["data"] = new List<Dictionary<string, object>>() {
        //        new Dictionary<string, object>() { { "Vendor", "Yokogawa" }, { "Model", "EJX" }, { "count", 100} },
        //        new Dictionary<string, object>() { { "Vendor", "Yokogawa" }, { "Model", "EJA" }, { "count", 200} },
        //        new Dictionary<string, object>() { { "Vendor", "Yokogawa" }, { "Model", "YTA" }, { "count", 300} },
        //        new Dictionary<string, object>() { { "Vendor", "Honeywell" }, { "Model", "HW001" }, { "count", 400} },
        //        new Dictionary<string, object>() { { "Vendor", "Honeywell" }, { "Model", "HW002" }, { "count", 150} },
        //        new Dictionary<string, object>() { { "Vendor", "Honeywell" }, { "Model", "HW003" }, { "count", 250} },
        //    };

        //    return retDict;
        //}

        private Dictionary<string, object> GenerateDummyPieChartData(RequestData request)
        {
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();
            foreach (var kvp in VendorModels)
            {
                string vendor = kvp.Key;
                List<string> models = kvp.Value;

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                foreach (string model in models)
                {
                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    int total = 0;

                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    foreach (var kvpStatus in statusDict)
                    {
                        string kpiStaus = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != kpiStaus)
                        {
                            continue;
                        }

                        total += kvpStatus.Value;
                    }

                    dataList.Add(new Dictionary<string, object>()
                        {
                            {"Vendor",  vendor}, {"Model", model}, { "count", total}
                        });
                }
            }

            retDict["data"] = dataList;

            return retDict;
        }

        private Dictionary<string, List<string>> GetSampleVendorModelData()
        {
            return new Dictionary<string, List<string>>()
            {
                {"Yokogawa", new List<string>() { "EJA", "EJX", "YTA"} },
                {"Honeywell", new List<string>() { "HW001", "HW002", "HW003", "HW004" } },
                {"Fisher Controls", new List<string>() { "FC001", "FC002", "FC003" } },
            };
        }

        private Dictionary<string, Dictionary<string, int>> GetSampleModelStatusCount()
        {
            return new Dictionary<string, Dictionary<string, int>>()
            {
                {"EJA", new Dictionary<string, int>(){ { "Normal", 40 },  { "Error", 20 }, { "Warning", 10}, { "Communication Error", 1} } },
                {"EJX", new Dictionary<string, int>(){ { "Normal", 30 },  { "Error", 21 }, { "Warning", 11}, { "Communication Error", 2} } },
                {"YTA", new Dictionary<string, int>(){ { "Normal", 30 },  { "Error", 22 }, { "Warning", 12 }, { "Communication Error", 3} } },
                {"HW001", new Dictionary<string, int>(){ { "Normal", 50 },  { "Error", 23 }, { "Warning", 13 }, { "Communication Error", 4} } },
                {"HW002", new Dictionary<string, int>(){ { "Normal", 60 },  { "Error", 24 }, { "Warning", 14 }, { "Communication Error", 5} } },
                {"HW003", new Dictionary<string, int>(){ { "Normal", 55 },  { "Error", 25 }, { "Warning", 15 }, { "Communication Error", 5} } },
                {"HW004", new Dictionary<string, int>(){ { "Normal", 45 },  { "Error", 26 }, { "Warning", 16 }, { "Communication Error", 6} } },
                {"FC001", new Dictionary<string, int>(){ { "Normal", 35 },  { "Error", 27 }, { "Warning", 17 }, { "Communication Error", 7} } },
                {"FC002", new Dictionary<string, int>(){ { "Normal", 20 },  { "Error", 28 }, { "Warning", 18 }, { "Communication Error", 8} } },
                {"FC003", new Dictionary<string, int>(){ { "Normal", 10 },  { "Error", 29 }, { "Warning", 19 }, { "Communication Error", 9} } },
            };
        }

        // For now, assume CommType is tied up to the model
        // I think CommType depends on the Device Path (which FCS the device it's currently connected to)
        // A device can support multiple CommTypes based on some product brochures
        private Dictionary<string, string> GetModelCommTypeLookup()
        {
            return new Dictionary<string, string>()
            {
                {"EJA", "HART" },
                {"EJX",  "FF-H1" },
                {"YTA", "FF-H1" },
                {"HW001", "HART" },
                {"HW002", "FF-H1" },
                {"HW003", "FF-H1" },
                {"HW004", "ISA100" },
                {"FC001", "FF-H1" },
                {"FC002", "ISA100" },
                {"FC003", "Profibus" },
            };
        }

        private Dictionary<string, string> GetModelCategoryLookup()
        {
            return new Dictionary<string, string>()
            {
                {"EJA", "Device" },
                {"EJX",  "Valve" },
                {"YTA", "Pressure Transmitter" },
                {"HW001", "Valve" },
                {"HW002", "Valve" },
                {"HW003", "Pump" },
                {"HW004", "Rotating Equipment" },
                {"FC001", "Pump" },
                {"FC002", "Valve" },
                {"FC003", "Valve" },
            };
        }

        private Dictionary<string, object> GenerateDummyBarChartData(RequestData request)
        {            
            Dictionary<string, List<string>> VendorModels = GetSampleVendorModelData();
            Dictionary<string, Dictionary<string, int>> modelStatusCount = GetSampleModelStatusCount();

            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

            Dictionary<string, object> retDict = new Dictionary<string, object>();
            foreach(var kvp in VendorModels)
            {
                string vendor = kvp.Key;
                List<string> models = kvp.Value;

                var vendorFilter = request.RequestParams.Find(r => r.Name == "Vendor");
                if (vendorFilter != null && vendorFilter.Value != vendor)
                {
                    continue;
                }

                foreach (string model in models)
                {
                    var modelFilter = request.RequestParams.Find(r => r.Name == "Model");
                    if (modelFilter != null && modelFilter.Value != model)
                    {
                        continue;
                    }

                    Dictionary<string, int> statusDict = modelStatusCount[model];
                    foreach(var kvpStatus in statusDict)
                    {
                        string status = kvpStatus.Key;

                        var statusFilter = request.RequestParams.Find(r => r.Name == "PRM Device Status");
                        if (statusFilter != null && statusFilter.Value != status)
                        {
                            continue;
                        }

                        int count = kvpStatus.Value;
                        dataList.Add(new Dictionary<string, object>()
                        {
                            {"Vendor",  vendor}, {"Model", model}, { "PRM Device Status", status}, { "count", count}
                        });
                    }
                }
            }

            retDict["data"] = dataList;
            return retDict;
        }

        public IsaeDwAccessor()
        {

        }
        public IsaeDwAccessor(string serverName)
        {
            try
            {
                string connStr = string.Format(CONNECTION_STRING, serverName);
                connection = new SqlConnection(connStr);
                connection.Open();
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Connection error. " + ex.ToString());
                throw;
            }
        }

        private static bool CheckDatabaseExists(SqlConnection sqlConnection, string databaseName)
        {
            using (SqlCommand command = new SqlCommand(string.Format("SELECT db_id('{0}')", databaseName), sqlConnection))
            {
                return (command.ExecuteScalar() != DBNull.Value);
            }
        }

        public List<HierarchyBase> GetConsolidatedHierarchy()
        {
            // Dillema: do we send it out in hierarchical structure or flat structure?
            // For this function, just give the flat structure
            // Maybe make it a parameter whether to give it as flat or hierarchical
            List<HierarchyBase> retList = new List<HierarchyBase>();

            using (SqlCommand command = new SqlCommand(@"SELECT * FROM DimHierarchyConsolidated", connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        HierarchyBase newHier = new HierarchyBase()
                        {
                            Id = Convert.ToInt64(reader["Id"]),
                            //ParentId = reader["ParentId"] == DBNull.Value ? null : Convert.ToInt64(reader["ParentId"]),
                            ParentId = null,
                            ServerName = reader["ServerName"].ToString(),
                            NodeName = reader["NodeName"].ToString(),
                            FullPath = reader["FullPath"].ToString(),
                            UnitType = reader["UnitType"].ToString(),
                            Children = new List<HierarchyBase>()
                        };

                        if (reader["ParentId"] != DBNull.Value)
                        {
                            newHier.ParentId = Convert.ToInt64(reader["ParentId"]);
                        }

                        retList.Add(newHier);
                    }
                }
            }

            return retList;
        }

        // Will only return the direct KPI's associated to a hierarchy.
        // If there is a need to retrive all the KPI's under a folder, the Javascript client will take care of consolidating.
        public Dictionary<string, List<KpiInfo>> GetConsolidatedHierarchyKpiOrig()
        {
            Dictionary<string, List<KpiInfo>> retDict = new Dictionary<string, List<KpiInfo>>();
            using (SqlCommand command = new SqlCommand(@"select FullPath, NodeName, g.Name as KpiGroupName, k.Name as KpiName " +
                                                        "from DimHierarchyKpiConsolidated conso " +
                                                        "JOIN DimHierarchyConsolidated h ON conso.HierarchyConsoId = h.Id " +
                                                        "JOIN DimKpi k ON conso.KpiConsoId = k.Id " +
                                                        "JOIN DimKpiGroup g ON g.Id = k.KpiGroupId ",
                                                        connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string FullPath = reader["FullPath"].ToString();
                        List<KpiInfo> currKpiList = null;

                        if (!retDict.TryGetValue(FullPath, out currKpiList))
                        {
                            currKpiList = new List<KpiInfo>();
                            retDict[FullPath] = currKpiList;
                        }

                        KpiInfo newData = new KpiInfo()
                        {
                            KpiGroupName = reader["KpiGroupName"].ToString(),
                            KpiName = reader["KpiName"].ToString(),
                        };

                        // We can assume that all items from the query are unique
                        currKpiList.Add(newData);
                    }
                }
            }

            return retDict;
        }

        // Will only return the direct KPI's associated to a hierarchy.
        // If there is a need to retrive all the KPI's under a folder, the Javascript client will take care of consolidating.
        // Dict Key: Path
        // Dict Value: Dict of Kpi Group and Kpi List
        // Note: this API is meant for lookup purposes, so keep it small.
        public Dictionary<string, Dictionary<string, List<string>>> GetConsolidatedHierarchyKpi()
        {
            Dictionary<string, Dictionary<string, List<string>>> retDict = new Dictionary<string, Dictionary<string, List<string>>>();
            using (SqlCommand command = new SqlCommand(@"select FullPath, NodeName, g.Name as KpiGroupName, k.Name as KpiName " +
                                                        "from DimHierarchyKpiConsolidated conso " +
                                                        "JOIN DimHierarchyConsolidated h ON conso.HierarchyConsoId = h.Id " +
                                                        "JOIN DimKpi k ON conso.KpiConsoId = k.Id " +
                                                        "JOIN DimKpiGroup g ON g.Id = k.KpiGroupId ",
                                                        connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // [1] Find the KPI grouo, given the path
                        string FullPath = reader["FullPath"].ToString();
                        string KpiGroupName = reader["KpiGroupName"].ToString();
                        string KpiName = reader["KpiName"].ToString();

                        Dictionary<string, List<string>> kpiGroupsList = null;

                        if (!retDict.TryGetValue(FullPath, out kpiGroupsList))
                        {
                            kpiGroupsList = new Dictionary<string, List<string>>();
                            retDict[FullPath] = kpiGroupsList;
                        }

                        // [2] Find the KPI given the KPI Groups list
                        List<string> kpisList = null;
                        if (!kpiGroupsList.TryGetValue(KpiGroupName, out kpisList))
                        {
                            kpisList = new List<string>();
                            kpiGroupsList[KpiGroupName] = kpisList;
                        }

                        // [3] If KPI is not yet in the list, add it
                        if (!kpisList.Contains(KpiName))
                        {
                            kpisList.Add(KpiName);
                        }
                    }
                }
            }

            return retDict;
        }

        public Dictionary<string, List<KpiInfo>> GetKpiMaster()
        {
            throw new NotImplementedException();
        }

        public List<TableProps> GetDimensions()
        {
            List<TableProps> retList = new List<TableProps>();

            using (SqlCommand command = new SqlCommand(@"dbo.GetTableProps", connection))
            {
                command.Parameters.AddWithValue("@TableName", "DimHierarchy");
                command.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TableProps tableProps = new TableProps()
                        {
                            ColumnName = reader["ColumnName"].ToString(),
                            Datatype = reader["Datatype"].ToString(),
                            /* Remove unnecessary props
                            MaxLength = Convert.ToUInt16(reader["MaxLength"]),
                            Precision = Convert.ToUInt16(reader["Precision"]),
                            Scale = Convert.ToUInt16(reader["Scale"]),
                            Nullable = Convert.ToBoolean(reader["Nullable"]),
                            PrimaryKey = Convert.ToBoolean(reader["PrimaryKey"]),
                            */
                        };

                        retList.Add(tableProps);
                    }
                }
            }

            return retList;
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            { //Close SQLAccessor to release resources.
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                }
            }

            disposed = true;
        }
    } //class AprSqlAccessor
}
