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
    [Route("生理量測記錄")]
    public class BodyController : Controller
    {
        private readonly DateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BodyController> _logger;
        public BodyController(DateService dateService, IConfiguration configuration, ILogger<BodyController> logger)
        {
            _configuration = configuration;
            _dateService = dateService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(30);
            var endDate = _dateService.GetCurrentDate();

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("record")]
        public async Task<IActionResult> Record(string id, string str_date, string end_date)
        {
            if (string.IsNullOrWhiteSpace(str_date))
            {
 
                var startDate = _dateService.GetStartDate(30);
                if (DateTime.TryParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    parsedDate = parsedDate.AddDays(1);
                    str_date = parsedDate.ToString("yyyyMMdd");
                }
                else
                {
                    return Json(new { error = $"無法解析的開始日期格式: {startDate}" });
                }
            }
            else
            {
                str_date = str_date.Replace("-", "");
            }

            end_date = !string.IsNullOrWhiteSpace(end_date) ? end_date.Replace("-", "") : _dateService.GetCurrentDate();

            var daysDifference = _dateService.GetDaysDifference(str_date, end_date) + 1; 
            var dateArray = _dateService.GetDateArray(end_date, daysDifference).Cast<IDictionary<string, object>>().ToList();

            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");


            using var connection = new SqlConnection(connectionStringTgsql);

            var idResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT 身份證字號 FROM 人事資料檔 WHERE counter = @Id",
                new { Id = id });

            if (idResult == null) return Json(new { error = "No data found." });

            var counterResult = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT counter FROM 病患檔 WHERE 身份證字號 = @Id AND 病歷號碼 LIKE '1%' AND (刪除否 IS NULL OR 刪除否 = 0)",
                new { Id = idResult });


            if (counterResult == 0) return Json(new { error = "No counter found." });

            var records = await connection.QueryAsync<dynamic>(
                "SELECT 日期, Data1, Data2, Data3 FROM 血糖血壓記錄檔 WHERE 病患檔_Counter = @Counter AND 日期 BETWEEN @StrDate AND @EndDate ORDER BY 日期 ASC, 時間 ASC",
                new { Counter = counterResult, StrDate = str_date, EndDate = end_date });


            foreach (var record in records)
            {
                var rocDateString = record.日期.ToString();

                int rocYear = 0, month = 0, day = 0; 
                DateTime recordDate = DateTime.MinValue; 

                if (rocDateString.Length == 7 && int.TryParse(rocDateString.Substring(0, 3), out rocYear)
                    && int.TryParse(rocDateString.Substring(3, 2), out month)
                    && int.TryParse(rocDateString.Substring(5, 2), out day))
                {
                    int westernYear = rocYear + 1911; 
                    try
                    {
                        recordDate = new DateTime(westernYear, month, day);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "提供的日期無效: {Message}", ex.Message);
                    }
                }
                else
                {
                    _logger.LogError("無法解析的日期格式");
                }
                foreach (var date in dateArray)
                {
                    _logger.LogInformation("Date in Array: {Date}, Data1: {Data1}, Data2: {Data2}, Data3: {Data3}, Data4: {Data4}",
                           date["日期"], date["data1"], date["data2"], date["data3"], date["data4"]);

                    // 將民國年轉換為西元年
                    string convertedDate = _dateService.ConvertToGregorian(date["日期"]?.ToString() ?? string.Empty);

                    if (convertedDate.StartsWith(recordDate.ToString("yyyyMMdd")))
                    {
                        date["data1"] = record.Data1 != null ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                        date["data2"] = record.Data2 != null ? Convert.ToDouble(record.Data2).ToString("0.0") : "0";
                        date["data3"] = record.Data3 != null ? Convert.ToDouble(record.Data3).ToString("0.0") : "0";
                        date["data4"] = record.Data2 != null && Convert.ToDouble(record.Data2) < 1 ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                    }
                }

            }

            var result = new
            {
                date = dateArray.Select(d =>
                {
                    var dateString = d["日期"] as string;
                    return dateString != null ? dateString.Substring(dateString.Length - 4) : string.Empty;
                }).ToList(),
                data1 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data1"]), 1).ToString("0.0")).ToList(),
                data2 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data2"]), 1).ToString("0.0")).ToList(),
                data3 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data3"]), 1).ToString("0.0")).ToList(),
                data4 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data4"]), 1).ToString("0.0")).ToList(),
                str_date = str_date,
                end_date = end_date
            };


            return Json(result);
        }
    }
}