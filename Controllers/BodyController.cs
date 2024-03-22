using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace dotnet_user.Controllers
{
    [Route("生理量測記錄")]
    public class BodyController : Controller
    {
        private readonly IBodyService _bodyService;
        private readonly ILogger<BodyController> _logger;

        public BodyController(IBodyService bodyService, ILogger<BodyController> logger)
        {
            _bodyService = bodyService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var strDate = _bodyService.GetStartDate(30);
            var endDate = _bodyService.GetCurrentDate();

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("record")]
        public async Task<IActionResult> Record(string id, string str_date, string end_date)
        {
            try
            {
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