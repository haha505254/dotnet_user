using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace dotnet_user.Controllers
{
    [Route("診斷書統計")]
    public class DiagnosisController : Controller
    {
        private readonly IDiagnosisService _diagnosisService;
        private readonly ILogger<DiagnosisController> _logger;

        public DiagnosisController(IDiagnosisService diagnosisService, ILogger<DiagnosisController> logger)
        {
            _diagnosisService = diagnosisService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var strDate = _diagnosisService.GetFirstDayOfLastMonth();
            var endDate = _diagnosisService.GetLastDayOfLastMonth();

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("diagnosis")]
        public async Task<IActionResult> Record(string str_date, string end_date)
        {
            try
            {
                var result = await _diagnosisService.GetDiagnosisRecords(str_date, end_date);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving diagnosis records");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }
    }
}