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
    [Route("薪資條查詢")]
    public class SalaryController : Controller
    {
        private readonly DateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalaryController> _logger;
        public SalaryController(DateService dateService, IConfiguration configuration, ILogger<SalaryController> logger)
        {
            _configuration = configuration;
            _dateService = dateService;
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
            var userInfo = await UserInfo(id);
            var userNo = await UserNo(id);
            var results = await RecordSalaryAndBonus(id, year, userNo.Item2, password);

            return Ok(results);
        }

        private async Task<dynamic> UserInfo(int id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            var user = await connection.QueryAsync("SELECT * FROM 人事資料檔 WHERE counter = @Id", new { Id = id });
            return user.FirstOrDefault();
        }

        private async Task<(int, string)> UserNo(int id)
        {
            var user = await UserInfo(id);
            int counter = (user.職務代碼 == 1 || user.職務代碼 == 7) && user.人事代號.Length == 3 && user.部門別 != "B71." ? 2 : 1;
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var no = await connection.QueryAsync("SELECT 人員代號 FROM 人事資料檔 WHERE 身份證號 = @IdNumber AND 分公司檔_counter = @Counter", new { IdNumber = user.身份證字號, Counter = counter });
            return (counter, no.FirstOrDefault().人員代號);
        }

        private async Task<List<object>> RecordSalaryAndBonus(int id, int year, string no, string password)
        {
            var user = await UserInfo(id);
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var passwordQuery = "SELECT 姓名, 身份證號, 第二密碼 FROM 人事資料檔 WHERE (第二密碼 IS NOT NULL OR 第二密碼 != '') AND LEN(身份證號) = 10 AND 身份證號 = @IdNumber AND 第二密碼 = @Password";
            var passwordResult = await connection.QueryAsync(passwordQuery, new { IdNumber = user.身份證字號, Password = password });

            var results = new List<object>();

            if (passwordResult.Any())
            {
                var salary = await connection.QueryAsync($"EXEC [dbo].[proc_查個人薪資發放年月]'{year}', '{no}'");
                var bonus = await connection.QueryAsync($"EXEC [dbo].[proc_查個人獎金發放年月]'{year}', '{no}'");

                for (int i = 1; i <= 12; i++)
                {
                    var month = i.ToString("D2");
                    var salaryMatch = salary.FirstOrDefault(s => month == s.薪資年月.Substring(3, 2))?.薪資年月;
                    var bonusMatch = bonus.FirstOrDefault(b => month == b.獎金年月.Substring(3, 2))?.獎金年月;

                    results.Add(new
                    {
                        第二密碼 = 0,
                        月份 = month,
                        薪資 = salaryMatch ?? string.Empty,
                        獎金 = bonusMatch ?? string.Empty
                    });
                }
            }
            else
            {
                for (int i = 1; i <= 12; i++)
                {
                    results.Add(new
                    {
                        第二密碼 = 1,
                        月份 = i.ToString("D2"),
                        薪資 = string.Empty,
                        獎金 = string.Empty
                    });
                }
            }

            return results;
        }
    }
}
