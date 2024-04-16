using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;
using MimeKit;
using System.Text.Json;

namespace dotnet_user.Controllers
{
    [Route("薪資條查詢")]
    public class SalaryController : Controller
    {
        private readonly IDateService _dateService;
        private readonly ISalaryService _salaryService;
        private readonly ILogger<SalaryController> _logger;
        public SalaryController(IDateService dateService, ISalaryService salaryService, ILogger<SalaryController> logger)
        {
            _dateService = dateService;
            _salaryService = salaryService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var year = int.Parse(_dateService.GetCurrentDate().Substring(0, 4)) - 1911;
            ViewData["Year"] = year;
            return View();
        }

        [HttpGet("salary")]
        public async Task<IActionResult> Record(int id, string password, int year)
        {
            var results = await _salaryService.RecordSalaryAndBonus(id, year, password);

            return Ok(results);
        }

        [HttpGet("salaryDetail")]
        public async Task<IActionResult> SalaryDetail(int id, int year)
        {
            var results = await _salaryService.GetSalaryDetail(id, year);

            if (results == null)
            {
                return NotFound();
            }

            return Ok(results);
        }

        [HttpGet("bonusDetail")]
        public async Task<IActionResult> BonusDetail(int id, int year)
        {
            var results = await _salaryService.GetBonusDetail(id, year);

            if (results == null)
            {
                return NotFound();
            }

            return Ok(results);
        }

        [HttpGet("doctorDetail")]
        public async Task<IActionResult> DoctorDetail(int id, int year)
        {
            var results = await _salaryService.GetDoctorDetail(id, year);

            if (results == null)
            {
                return NotFound();
            }

            return Ok(results);
        }

        [HttpPost("sendEmail")]
        public async Task<IActionResult> SendEmail(int id, [FromBody] JsonElement request)
        {
            string email = request.GetProperty("email").GetString() ?? string.Empty;
            string title = request.GetProperty("title").GetString() ?? string.Empty;
            string content = request.GetProperty("content").GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
            {
                return BadRequest("Missing required fields: email, title, or content.");
            }

            var result = await _salaryService.SendEmail(id, email, title, content);
            return Ok(result);
        }

        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword(int id, [FromForm] string old_pw, [FromForm] string new_pw)
        {
            if (string.IsNullOrEmpty(old_pw) || string.IsNullOrEmpty(new_pw))
            {
                return BadRequest("Missing required fields: old_pw or new_pw.");
            }

            var result = await _salaryService.ChangePassword(id, old_pw, new_pw);
            return Ok(result);
        }
    }
}