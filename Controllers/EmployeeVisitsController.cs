using dotnet_user.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace dotnet_user.Controllers
{
    [Route("院內看診明細")]
    public class EmployeeVisitsController : Controller
    {
        private readonly DateService _dateService;
        private readonly EmployeeVisitsService _employeeVisitsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeVisitsController> _logger;

        public EmployeeVisitsController(DateService dateService, IConfiguration configuration, ILogger<EmployeeVisitsController> logger, EmployeeVisitsService employeeVisitsService)
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


    }
}
