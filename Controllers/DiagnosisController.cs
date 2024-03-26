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
            var strDate = _diagnosisService.GetFirstDayOfLastMonth(); //取得上個月第一天
            var endDate = _diagnosisService.GetLastDayOfLastMonth(); //取得上個月最後一天

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        // 取得診斷書記錄的 API 端點,接受開始日期和結束日期的查詢參數
        [HttpGet("diagnosis")]
        public async Task<IActionResult> Record(string str_date, string end_date)
        {
            try
            {
                // 呼叫診斷書服務取得診斷書記錄,並以 JSON 格式回傳結果
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