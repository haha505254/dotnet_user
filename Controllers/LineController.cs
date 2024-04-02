using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services.Interface;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace dotnet_user.Controllers
{
    [Route("通知管理系統")]
    public class LineController : Controller
    {
        private readonly ILineService _lineService; // 注入 ILineService 介面
        private readonly ILogger<LineController> _logger; // 注入 ILogger 介面,用於記錄日誌

        public LineController(ILineService lineService, ILogger<LineController> logger)
        {
            _lineService = lineService;
            _logger = logger;
        }

        // 首頁 Action,返回通知管理系統的檢視
        public async Task<IActionResult> Index(string id)
        {
            try
            {
                // 呼叫 _lineService.GetUserData 方法取得使用者資料
                var userData = await _lineService.GetUserData(id);

                ViewData["Counter"] = userData.Counter;
                ViewData["Email"] = userData.Email;
                ViewData["UserSetting"] = userData.UserSetting;
                ViewData["Items"] = userData.Items;

                // 檢查是否有來自 UpdateUserSetting 方法的重定向
                if (TempData["UpdateSuccess"] != null)
                {
                    ViewBag.Result = "設定已更新成功。";
                }

                return View();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user data");
                return View();
            }
        }

        // 更新使用者設定的 Action
        [HttpPost]
        public async Task<IActionResult> UpdateUserSetting(string counter, string email, IDictionary<string, string> notifySettings)
        {
            try
            {
                // 呼叫 _lineService.UpdateUserSetting 方法更新使用者設定
                await _lineService.UpdateUserSetting(counter, email, notifySettings);

                // 設定 TempData,表示更新成功
                TempData["UpdateSuccess"] = true;

                // 重定向到 Index 方法,並傳遞 id 參數
                return RedirectToAction("Index", new { id = counter });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user settings");
                return Content("Error updating user settings.");
            }
        }
    }
}