using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("生理量測記錄")]
    public class BodyController : Controller
    {
        private readonly IBodyService _bodyService; // 注入 IBodyService 介面
        private readonly ILogger<BodyController> _logger; // 注入 ILogger 介面,用於記錄日誌


        public BodyController(IBodyService bodyService, ILogger<BodyController> logger)
        {
            _bodyService = bodyService;
            _logger = logger;
        }

        // 首頁 Action,返回生理量測記錄的檢視
        public IActionResult Index()
        {
            var strDate = _bodyService.GetStartDate(30); // 取得起始日期(30天前)
            var endDate = _bodyService.GetCurrentDate(); // 取得當前日期

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View(); 
        }

        // 獲取生理量測記錄的 Action,接收 id、str_date 和 end_date 參數
        [HttpGet("record")]
        public async Task<IActionResult> Record(string id, string str_date, string end_date)
        {
            try
            {
                // 呼叫 _bodyService.GetBodyRecords 方法取得生理量測記錄
                var result = await _bodyService.GetBodyRecords(id, str_date, end_date);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving body records"); 
                return Json(new { error = "An error occurred while processing your request." }); 
            }
        }
    }
}