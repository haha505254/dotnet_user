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
            using var tgsqlconnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            var user = await tgsqlconnection.QueryAsync("SELECT * FROM 人事資料檔 WHERE counter = @Id", new { Id = id });
            return user.FirstOrDefault() ?? new { };
        }

        private async Task<(int, string)> UserNo(int id)
        {
            var user = await UserInfo(id);
            int counter = (user.職務代碼 == 1 || user.職務代碼 == 7) && user.人事代號.Length == 3 && user.部門別 != "B71." ? 2 : 1;
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var no = await connection.QueryAsync("SELECT 人員代號 FROM 人事資料檔 WHERE 身份證號 = @IdNumber AND 分公司檔_counter = @Counter", new { IdNumber = user.身份證字號, Counter = counter });
            var firstNo = no.FirstOrDefault();
            if (firstNo == null) 
            {
                return (counter, string.Empty); 
            }

            return (counter, firstNo.人員代號);
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

        [HttpGet("salaryDetail")]
        public async Task<IActionResult> SalaryDetail(int id, int year)
        {

                var user = await UserInfo(id);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection");
                string connectionStringCountry = _configuration.GetConnectionString("CountryConnection");

                // 取得部門信息
                using var tgsqlConn = new SqlConnection(connectionStringTgsql);
                var deptQuery = "SELECT 單位名稱 FROM 病歷單位代號檔 WHERE 單位代號 = @部門別";
                var dept = await tgsqlConn.QueryFirstOrDefaultAsync<dynamic>(deptQuery, new { 部門別 = user.部門別 });

                // 取得職務信息
                using var countryConn = new SqlConnection(connectionStringCountry);
                var jobQuery = @"
                    SELECT 職務名稱 
                    FROM 人事資料檔 
                    WHERE 身份證號 = @身份證字號 
                        AND 離職日 = '' 
                        AND (職務名稱 IS NOT NULL OR 職務名稱 != '') 
                    GROUP BY 職務名稱";
                var job = await countryConn.QueryFirstOrDefaultAsync<dynamic>(jobQuery, new { 身份證字號 = user.身份證字號 });

                // 取得用戶編號
                var (counter, personnelNumber) = await UserNo(id);
                if (string.IsNullOrEmpty(personnelNumber))
                {
                    return NotFound("Personnel number not found.");
                }

                // 執行存儲過程獲取薪資資訊
                var salaryQuery = "EXEC [dbo].[proc_薪資發放結果] @Counter, @Year, @PersonnelNumber";
                var salary = (await countryConn.QueryAsync<dynamic>(salaryQuery, new { Counter = counter, Year = year, PersonnelNumber = personnelNumber })).ToList();

                // 計算加扣項
                decimal debt = 0, credit = 0, food = 0;
                foreach (var item in salary)
                {
                    switch (item.加扣項)
                    {
                        case "加項":
                            debt += Convert.ToDecimal(item.薪資項目金額);
                            break;
                        case "扣項":
                            credit += Convert.ToDecimal(item.薪資項目金額);
                            break;
                    }

                    var bonusTypes = new[] { "伙食費", "不休假獎金", "其他(免)", "國定假日加班費", "例假日加班費", "休假日加班費", "加班費" };
                    var itemName = item.薪資項目名稱.ToString();
                    bool isBonusType = false;

                    foreach (var type in bonusTypes)
                    {
                        if (itemName == type)
                        {
                            isBonusType = true;
                            break;
                        }
                    }

                    if (isBonusType)
                    {
                        food += Convert.ToDecimal(item.薪資項目金額);
                    }
                }

                var userInfoOutput = new
                {
                    部門 = dept?.單位名稱,
                    代號 = user.人事代號,
                    姓名 = user.姓名,
                    職務 = job?.職務名稱,
                    時間 = DateTime.Now.ToString("yyyy-MM"),
                    加項 = debt.ToString("N0"),
                    扣項 = credit.ToString("N0"),
                    應稅 = (debt - food).ToString("N0"),
                    實發 = (debt - credit).ToString("N0"),
                    發薪 = salary.FirstOrDefault()?.發薪日期,
                    帳號 = salary.FirstOrDefault()?.轉帳帳號,
                    信箱 = user.Email帳號
                };

                // 初始化加項和扣項详情列表
                var deptDetailsList = new List<dynamic>();
                var creditDetailsList = new List<dynamic>();

                foreach (var item in salary)
                {
                    if (item.加扣項 == "加項")
                    {
                        deptDetailsList.Add(new
                        {
                            加項項目 = item.薪資項目名稱 ?? "",
                            加項 = Convert.ToDecimal(item.薪資項目金額).ToString("N0"),
                            加項備註 = item.備註 ?? ""
                        });
                    }
                    if (item.加扣項 == "扣項")
                    {
                        creditDetailsList.Add(new
                        {
                            扣項項目 = item.薪資項目名稱 ?? "",
                            扣項 = Convert.ToDecimal(item.薪資項目金額).ToString("N0"),
                            扣項備註 = item.備註 ?? ""
                        });
                    }
                }

                // 合并加項和扣項详情到薪资详情列表
                var salaryDetailsList = new List<dynamic>();
                for (int i = 0; i < 9; i++)
                {
                    var deptItem = i < deptDetailsList.Count ? deptDetailsList[i] : new { 加項項目 = "", 加項 = "", 加項備註 = "" };
                    var creditItem = i < creditDetailsList.Count ? creditDetailsList[i] : new { 扣項項目 = "", 扣項 = "", 扣項備註 = "" };

                    salaryDetailsList.Add(new
                    {
                        加項項目 = deptItem.加項項目,
                        加項 = deptItem.加項,
                        加項備註 = deptItem.加項備註,
                        扣項項目 = creditItem.扣項項目,
                        扣項 = creditItem.扣項,
                        扣項備註 = creditItem.扣項備註
                    });
                }

                // 整理最终输出
                var finalOutput = new List<object>
                {
                    new List<object> { userInfoOutput },
                    salaryDetailsList
                };

                return Ok(finalOutput);

        }
    }
}
