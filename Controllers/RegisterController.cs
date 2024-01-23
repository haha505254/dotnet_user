using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services; // 確保引用了 DateService 所在的命名空間

using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;
namespace dotnet_user.Controllers
{
    [Route("預掛名單")]
    public class RegisterController : Controller
    {

        private readonly DateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterController> _logger;
        public RegisterController(DateService dateService, IConfiguration configuration, ILogger<RegisterController> logger)
        {
            _configuration = configuration;
            _dateService = dateService;
            _logger = logger;
        }
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(-1);
            ViewData["StrDate"] = strDate;
            return View();
        }

        [HttpGet("register")]
        public async Task<IActionResult> Register(string id, string str_date = "")
        {
            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");
            if (string.IsNullOrWhiteSpace(str_date))
            {
                var startDate = _dateService.GetStartDate(-1);
                DateTime.TryParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);
                str_date = parsedDate.ToString("yyyyMMdd");
            }
            else
            {
                str_date = str_date.Replace("-", "");
            }
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);

            using (var connection = new SqlConnection(connectionStringTgsql))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT b.病歷號碼, b.身份證字號, a.患者姓名, a.掛號序號, c.代碼內容 as 科別, d.代碼內容 as 診別, e.代碼內容 as 班別, f.姓名
                    FROM 掛號檔 as a
                    JOIN 病患檔 as b ON a.病患檔_counter = b.counter
                    JOIN 代碼檔 as c ON a.科別代碼 = c.代碼
                    JOIN 代碼檔 as d ON a.診別代碼 = d.代碼
                    JOIN 代碼檔 as e ON a.班別代碼 = e.代碼
                    JOIN 人事資料檔 as f ON a.醫師代號 = f.人事代號
                    WHERE a.掛號日期 = @str_date AND a.分派類別 = 'A' AND a.科別代碼 NOT IN (27, 28) AND 
                    c.代碼名稱 = '科別代碼' AND d.代碼名稱 = '診別代碼' AND e.代碼名稱 = '班別代碼'";

                var records = await connection.QueryAsync(query, new { str_date });

                var result = records.Select(record => new
                {
                    病歷號碼 = record.病歷號碼,
                    身份證字號 = record.身份證字號,
                    患者姓名 = record.患者姓名,
                    掛號序號 = record.掛號序號,
                    科別 = record.科別,
                    診別 = record.診別,
                    班別 = record.班別,
                    姓名 = record.姓名
                }).ToList();

                return Json(result);
            }
        }
    }
}
