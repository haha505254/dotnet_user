using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("醫師時段查詢")]
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(IDoctorService doctorService, ILogger<DoctorController> logger)
        {
            _doctorService = doctorService;
            _logger = logger;
        }

        // 醫師時段查詢首頁
        public IActionResult Index()
        {
            var strDate = _doctorService.GetStartDate(0);
            ViewData["StrDate"] = strDate;
            return View();
        }

        // 獲取醫師時段記錄
        [HttpGet("doctor")]
        public async Task<IActionResult> Record([FromQuery] string strDate)
        {
            try
            {
                // 調用 DoctorService 的 GetDoctorRecords 方法獲取醫師時段記錄
                var result = await _doctorService.GetDoctorRecords(strDate);
                // 將結果以 JSON 格式返回
                return Json(result);
            }
            catch (Exception ex)
            {
                // 記錄錯誤日誌
                _logger.LogError(ex, "獲取醫師時段記錄時發生錯誤");
                // 返回錯誤信息
                return Json(new { error = "處理您的請求時發生錯誤。" });
            }
        }
    }
}