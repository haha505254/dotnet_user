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
        private readonly IEmployeeVisitsService _employeeVisitsService; // 注入 IEmployeeVisitsService 介面
        private readonly IDateService _dateService; // 注入 IDateService 介面
        private readonly ILogger<EmployeeVisitsController> _logger; // 注入 ILogger 介面,用於記錄日誌

        public EmployeeVisitsController(IEmployeeVisitsService employeeVisitsService, IDateService dateService, ILogger<EmployeeVisitsController> logger)
        {
            _employeeVisitsService = employeeVisitsService;
            _dateService = dateService;
            _logger = logger;
        }

        // 首頁 Action,返回院內看診明細的檢視
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(30); // 取得起始日期(30天前)
            var endDate = _dateService.GetCurrentDate(); // 取得當前日期
            var count = 60;

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;
            ViewData["Count"] = count;

            return View();
        }

        // 獲取院內看診明細的 Action
        [HttpGet("employeeVisits")]
        public async Task<IActionResult> EmployeeVisits(string str_date = "", string end_date = "")
        {
            try
            {
                var result = await _employeeVisitsService.GetEmployeeVisitsData(str_date, end_date);
                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving employee visits data");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }

        // 下載院內看診明細的 Action
        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "")
        {
            try
            {
                var stream = await _employeeVisitsService.DownloadEmployeeVisitsData(str_date, end_date);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeVisitsData.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading employee visits data");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }

        // 格式化日期並轉換為民國年格式
        private string FormatDate(string dateString, string defaultDate, bool subtractOneDay = false)
        {
            dateString = dateString.Replace("-", "");

            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                if (subtractOneDay)
                {
                    parsedDate = parsedDate.AddDays(-1);
                }
                dateString = parsedDate.ToString("yyyyMMdd");
            }
            else
            {
                dateString = defaultDate;
            }

            return (int.Parse(dateString.Substring(0, 4)) - 1911).ToString() + dateString.Substring(4, 4);
        }
    }
}