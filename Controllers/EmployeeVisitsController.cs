using dotnet_user.Services;
using dotnet_user.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Globalization;

namespace dotnet_user.Controllers
{
    [Route("院內看診明細")]
    public class EmployeeVisitsController : Controller
    {
        private readonly IDateService _dateService;
        private readonly EmployeeVisitsService _employeeVisitsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeVisitsController> _logger;

        public EmployeeVisitsController(IDateService dateService, IConfiguration configuration, ILogger<EmployeeVisitsController> logger, EmployeeVisitsService employeeVisitsService)
        {
            _configuration = configuration;
            _dateService = dateService;
            _employeeVisitsService = employeeVisitsService;
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


        [HttpGet("employeeVisits")]
        public async Task<IActionResult> EmployeeVisits(string str_date = "", string end_date = "")
        {

            str_date = str_date.Replace("-", "");
            end_date = end_date.Replace("-", "");
            // 處理 str_date
            if (!string.IsNullOrEmpty(str_date) && DateTime.TryParseExact(str_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate))
            {
                str_date = parsedStartDate.ToString("yyyyMMdd");
            }
            else
            {
                str_date = _dateService.GetStartDate(30);
            }

            // 處理 end_date
            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParseExact(end_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                end_date = parsedEndDate.ToString("yyyyMMdd");
            }
            else
            {
                end_date = _dateService.GetCurrentDate();
            }

            // 將西元年份轉換為民國年份
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            _logger.LogInformation("{str_date} {end_date}", str_date, end_date);

            var combinedRecords = await _employeeVisitsService.GetEmployeeVisitsData(str_date, end_date);

            var result = combinedRecords.Select(record => new
            {
                類別 = record.類別.ToString(),
                醫師代號 = record.醫師代號.ToString(),
                醫師姓名 = record.醫師姓名.ToString(),
                病歷號碼 = record.病歷號碼.ToString(),
                患者姓名 = record.患者姓名.ToString(),
                就診日 = record.就診日.ToString(),
                結束日 = record.結束日.ToString()
            }).ToList();

            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            return Content(json, "application/json");
        }


        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "")
        {

            // Your existing logic to format and process dates goes here
            str_date = str_date.Replace("-", "");
            end_date = end_date.Replace("-", "");
            // 處理 str_date
            if (!string.IsNullOrEmpty(str_date) && DateTime.TryParseExact(str_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate))
            {
                str_date = parsedStartDate.AddDays(-1).ToString("yyyyMMdd");
            }
            else
            {
                str_date = _dateService.GetStartDate(30);
            }

            // 處理 end_date
            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParseExact(end_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                end_date = parsedEndDate.ToString("yyyyMMdd");
            }
            else
            {
                end_date = _dateService.GetCurrentDate();
            }

            // 將西元年份轉換為民國年份
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            _logger.LogInformation("{str_date} {end_date}", str_date, end_date);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var dynamicData = await _employeeVisitsService.GetEmployeeVisitsData(str_date, end_date);

            var records = dynamicData.Select(item => new
            {
                item.類別,
                item.醫師代號,
                item.醫師姓名,
                item.病歷號碼,
                item.患者姓名,
                item.就診日,
                item.結束日
            }).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("EmployeeVisits Data");

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

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeVisitsData.xlsx");
            }
        }
    }
}
