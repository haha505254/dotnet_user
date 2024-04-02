using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHomeService homeService, ILogger<HomeController> logger)
        {
            _homeService = homeService;
            _logger = logger;
        }

        // 首頁 Action,傳入 id 參數
        public async Task<IActionResult> Index(int? id)
        {
            try
            {
                // 呼叫 _homeService.GetUserRights 方法獲取用戶權限
                var userRights = await _homeService.GetUserRights(id);
                ViewData["UserRights"] = userRights;
                return View();
            }
            catch (Exception ex)
            {
                // 記錄異常
                _logger.LogError(ex, "An error occurred while processing the request.");

                // 處理異常情況
                return View("Error");
            }
        }
    }
}