using dotnet_user.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

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
    }
}
