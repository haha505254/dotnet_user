using dotnet_user.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace dotnet_user.Controllers
{
    [Route("門診看診人數")]
    public class OutpatientVisitsController : Controller
    {
        private readonly DateService _dateService;
        private readonly OutpatientVisitsService _outpatientVisitsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutpatientVisitsController> _logger;
        public OutpatientVisitsController(DateService dateService, IConfiguration configuration, ILogger<OutpatientVisitsController> logger, OutpatientVisitsService outpatientVisitsService)
        {
            _configuration = configuration;
            _dateService = dateService;
            _outpatientVisitsService = outpatientVisitsService;
            _logger = logger;
            
        }
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(30);
            var endDate = _dateService.GetCurrentDate();
            var count = 60;
            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;
            ViewData["Count"] = count;
            return View();
        }


        [HttpGet("outpatientVisits")]
        public async Task<IActionResult> OutpatientVisits(string str_date = "", string end_date = "", string count = "")
        {
            var Records = await _outpatientVisitsService.GetOutpatientVisitsData(str_date, end_date, count);

            var result = Records.Select(record => new
            {
                掛號日期 = record.掛號日期.ToString(),
                看診班別 = record.看診班別.ToString(),
                醫師姓名 = record.醫師姓名.ToString(),
                看診人數 = record.看診人數.ToString(),

            }).ToList();

            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            return Content(json, "application/json");
        }


        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "", string count = "")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var dynamicData = await _outpatientVisitsService.GetOutpatientVisitsData(str_date, end_date, count);

            var records = dynamicData.Select(item => new
            {
                item.掛號日期,
                item.看診班別,
                item.醫師姓名,
                item.看診人數,
            }).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("OutpatientVisits Data");

                var properties = records.FirstOrDefault()?.GetType().GetProperties();
                int row = 1;

                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = properties[i].Name;
                    }

                    foreach (var record in records)
                    {
                        row++;
                        for (int i = 0; i < properties.Length; i++)
                        {
                            worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                        }
                    }
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OutpatientVisitsData.xlsx");
            }
        }
    }
}
