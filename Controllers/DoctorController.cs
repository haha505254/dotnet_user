using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("醫師時段查詢")]
    public class DoctorController : Controller
    {
        private readonly IDateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(IDateService dateService, IConfiguration configuration, ILogger<DoctorController> logger)
        {
            _configuration = configuration;
            _dateService = dateService;
            _logger = logger;
        }
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(0);
            ViewData["StrDate"] = strDate;
            return View();
        }



        [HttpGet("doctor")]
        public async Task<IActionResult> Record([FromQuery] string strDate)
        {
            _logger.LogInformation($"input date: {strDate}");
            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");

            strDate = !string.IsNullOrEmpty(strDate) ? strDate.Replace("-", "") : _dateService.GetStartDate(0);
            var week = _dateService.GetWeekDay(strDate);
            week = week == "0" ? "7" : week; 
            _logger.LogInformation($"Original date: {strDate}");
            strDate = (int.Parse(strDate.Substring(0, 4)) - 1911).ToString() + strDate.Substring(4, 4);
            _logger.LogInformation($"Converted date: {strDate}");

            _logger.LogInformation($"Received date: {strDate}, Week: {week}");


            using (var connection = new SqlConnection(connectionStringTgsql))
            {
                var offSql = @"SELECT [醫師代號] FROM [醫師代班表] WHERE [限數] = '-2' AND [日期] = @strDate";
                _logger.LogInformation($"Executing SQL: {offSql}, Parameters: {{ strDate: {strDate} }}");
                var off = (await connection.QueryAsync<string>(offSql, new { strDate })).ToList();

                var recordSql = @"
                    SELECT b.[代碼內容] as [科別], c.[代碼內容] as [診別], d.[代碼內容] as [班別], a.[醫師代號], e.[姓名], a.[備註]
                    FROM [醫師排班表] as a
                    JOIN [代碼檔] as b ON a.[科別代碼] = b.[代碼]
                    JOIN [代碼檔] as c ON a.[診別代碼] = c.[代碼]
                    JOIN [代碼檔] as d ON a.[班別代碼] = d.[代碼]
                    JOIN [人事資料檔] as e ON a.[醫師代號] = e.[人事代號]
                    WHERE a.[年月份] = @yearMonth AND a.[星期代碼] = @week
                    AND a.[限數] NOT IN ('-2')
                    AND a.[醫師代號] NOT IN @offArray
                    AND b.[代碼名稱] = '科別代碼'
                    AND c.[代碼名稱] = '診別代碼'
                    AND d.[代碼名稱] = '班別代碼'
                    ORDER BY a.[班別代碼], [科別]";

                _logger.LogInformation($"Executing SQL: {recordSql}, Parameters: {{ yearMonth: {strDate.Substring(0, 5)}, week: {week}, offArray: {string.Join(", ", off)} }}");
                var yearMonth = strDate.Substring(0, 5);
                var records = await connection.QueryAsync(recordSql, new { yearMonth, week, offArray = off });

                var result = records.Select(r => new
                {
                    科別 = (string)r.科別,
                    診別 = (string)r.診別,
                    班別 = (string)r.班別,
                    代號 = (string)r.醫師代號,
                    姓名 = (string)r.姓名,
                    備註 = (string)r.備註
                }).ToList();

                if (!result.Any())
                {
                    result.Add(new { 科別 = "", 診別 = "", 班別 = "", 代號 = "", 姓名 = "", 備註 = "" });
                }

                var recordSecondSql = @"
                    SELECT b.[代碼內容] as [科別], c.[代碼內容] as [診別], e.[代碼內容] as [班別], a.[醫師代號], d.[姓名]
                    FROM [醫師代班表] as a
                    JOIN [代碼檔] as b ON a.[科別代碼] = b.[代碼]
                    JOIN [代碼檔] as c ON a.[診別代碼] = c.[代碼]
                    JOIN [人事資料檔] as d ON a.[醫師代號] = d.[人事代號]
                    LEFT JOIN [代碼檔] as e ON LEFT(a.[預約代號], 1) = e.[代碼]
                    WHERE a.[日期] = @strDate AND a.[限數] = '-2'
                    AND b.[代碼名稱] = '科別代碼'
                    AND c.[代碼名稱] = '診別代碼'
                    AND e.[代碼名稱] = '班別代碼'";

                var recordSecond = await connection.QueryAsync(recordSecondSql, new { strDate });

                var resultSecond = recordSecond.Select(r => new
                {
                    科別 = (string)r.科別,
                    診別 = (string)r.診別,
                    班別 = (string)r.班別,
                    代號 = (string)r.醫師代號,
                    姓名 = (string)r.姓名
                }).ToList();

                if (!resultSecond.Any())
                {
                    resultSecond.Add(new { 科別 = "", 診別 = "", 班別 = "", 代號 = "", 姓名 = "" });
                }

                return Json(new { Result = result, ResultSecond = resultSecond });
            }
        }
    }
}