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
    [Route("診斷書統計")]
    public class DiagnosisController : Controller
    {
        private readonly DateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiagnosisController> _logger;
        public DiagnosisController(DateService dateService, IConfiguration configuration, ILogger<DiagnosisController> logger)
        {
            _configuration = configuration;
            _dateService = dateService;
            _logger = logger;
        }
        public IActionResult Index()
        {
            var strDate = _dateService.GetFirstDayOfLastMonth();
            var endDate = _dateService.GetLastDayOfLastMonth();

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("diagnosis")]
        public async Task<IActionResult> Record(string str_date, string end_date)
        {

            // Your existing logic to format and process dates goes here
            str_date = str_date.Replace("-", "");
            end_date = end_date.Replace("-", "");
            // 處理 str_date
            if (!string.IsNullOrEmpty(str_date) && DateTime.TryParseExact(str_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate))
            {
                str_date = parsedStartDate.ToString("yyyyMMdd");
            }
            else
            {
                str_date = _dateService.GetStartDate(0);
            }

            // 處理 end_date
            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParseExact(end_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                end_date = parsedEndDate.ToString("yyyyMMdd");
            }
            else
            {
                end_date = _dateService.GetCurrentDate();
            }

            // 將西元年份轉換為民國年份
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);
            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");

            List<dynamic> result = new List<dynamic>();
            List<dynamic> resultSecond = new List<dynamic>();

            using (var connection = new SqlConnection(connectionStringTgsql))
            {
                // First Query
                string query = @"SELECT LEFT(診斷日期, 5) AS 月份, COUNT(*) AS 數量
                         FROM 診斷證明檔
                         WHERE 診斷日期 BETWEEN @strDate AND @endDate
                         GROUP BY LEFT(診斷日期, 5)
                         ORDER BY 月份";

                var records = await connection.QueryAsync<dynamic>(query, new { strDate = str_date, endDate = end_date });
                result.AddRange(records);

                // Second Query
                string querySecond = @"SELECT LEFT(a.診斷日期, 5) AS 月份, b.姓名, COUNT(*) AS 數量
                               FROM 診斷證明檔 AS a
                               JOIN 人事資料檔 AS b ON a.醫師代號 = b.人事代號
                               WHERE a.診斷日期 BETWEEN @strDate AND @endDate
                               GROUP BY LEFT(a.診斷日期, 5), b.姓名
                               ORDER BY LEFT(a.診斷日期, 5), b.姓名";

                var recordsSecond = await connection.QueryAsync<dynamic>(querySecond, new { strDate = str_date, endDate = end_date });
                resultSecond.AddRange(recordsSecond);
            }

            return Json(new { FirstRecord = result, SecondRecord = resultSecond });
        }
    }
}
