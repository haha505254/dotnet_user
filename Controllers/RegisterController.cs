using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("預掛名單")]
    public class RegisterController : Controller
    {
        private readonly IRegisterService _registerService; // 注入 IRegisterService 介面
        private readonly ILogger<RegisterController> _logger; // 注入 ILogger 介面,用於記錄日誌

        public RegisterController(IRegisterService registerService, ILogger<RegisterController> logger)
        {
            _registerService = registerService;
            _logger = logger;
        }

        // 首頁 Action,返回預掛名單的檢視
        public IActionResult Index()
        {
            var strDate = _registerService.GetStartDate(-1); // 取得前一天的日期
            ViewData["StrDate"] = strDate;
            return View();
        }

        // 獲取預掛名單的 Action,接收 id 和 str_date 參數
        [HttpGet("register")]
        public async Task<IActionResult> Register(string id, string str_date = "")
        {
            try
            {
                // 呼叫 _registerService.GetRegisterRecords 方法取得預掛名單記錄
                var result = await _registerService.GetRegisterRecords(id, str_date);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving register records");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }
    }
}