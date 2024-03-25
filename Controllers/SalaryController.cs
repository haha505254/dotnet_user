using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;
using Azure.Core;
using System.Text;
using Microsoft.Win32;
using Microsoft.AspNetCore.Http;
using static Azure.Core.HttpHeader;
using System.Diagnostics.Metrics;
using Newtonsoft.Json;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("薪資條查詢")]
    public class SalaryController : Controller
    {
        private readonly IDateService _dateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalaryController> _logger;
        public SalaryController(IDateService dateService, IConfiguration configuration, ILogger<SalaryController> logger)
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
            var no = await connection.QueryAsync("SELECT 人員代號 FROM 人事資料檔 WHERE 身份證號 = @IdNumber AND 分公司檔_counter = @Counter GROUP BY 人員代號, 到職日 ORDER BY 到職日 DESC", new { IdNumber = user.身份證字號, Counter = counter });
            var firstNo = no.FirstOrDefault();
            if (firstNo == null)
            {
                return (counter, string.Empty);
            }

            return (counter, firstNo.人員代號);
        }

        [HttpGet("salary")]
        public async Task<IActionResult> Record(int id, string password, int year)
        {
            var results = await RecordSalaryAndBonus(id, year, password);

            return Ok(results);
        }


        private async Task<List<object>> RecordSalaryAndBonus(int id, int year, string password)
        {
            var user = await UserInfo(id);
            var userNo = await UserNo(id);
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var passwordQuery = "SELECT 姓名, 身份證號, 第二密碼 FROM 人事資料檔 WHERE (第二密碼 IS NOT NULL OR 第二密碼 != '') AND LEN(身份證號) = 10 AND 身份證號 = @IdNumber AND 第二密碼 = @Password";
            var passwordResult = await connection.QueryAsync(passwordQuery, new { IdNumber = user.身份證字號, Password = password });

            var results = new List<object>();

            if (passwordResult.Any())
            {
                var salary = await connection.QueryAsync($"EXEC [dbo].[proc_查個人薪資發放年月]'{year}', '{userNo.Item2}'");
                var bonus = await connection.QueryAsync($"EXEC [dbo].[proc_查個人獎金發放年月]'{year}', '{userNo.Item2}'");

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


        [HttpGet("bonusDetail")]
        public async Task<IActionResult> BonusDetail(int id, int year)
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
            var deptName = (await tgsqlConn.QueryFirstOrDefaultAsync<dynamic>(deptQuery, new { 部門別 = user.部門別 }))?.單位名稱 ?? "未知部門";

            // 取得職務信息
            using var countryConn = new SqlConnection(connectionStringCountry);
            var jobQuery = @"
                SELECT TOP 1 職務名稱, 人員代號, 轉帳帳號, 到職日
                FROM 人事資料檔
                WHERE 身份證號 = @身份證字號 AND 離職日 = '' AND (職務名稱 IS NOT NULL AND 職務名稱 != '')
                GROUP BY 職務名稱, 人員代號, 轉帳帳號, 到職日
                ORDER BY 到職日 DESC";
            var job = await countryConn.QueryFirstOrDefaultAsync<dynamic>(jobQuery, new { 身份證字號 = user.身份證字號 });

            if (job == null)
            {
                return NotFound("Job information not found.");
            }

            // 根据职务代码调整查询以适应分公司檔_counter的条件
            string bonusQuery;
            object queryParameters;
            if (user.職務代碼 == 1 || user.職務代碼 == 7)
            {
                bonusQuery = @"
            SELECT * FROM 獎金發放結果檔
            WHERE 人員代號 = @人員代號 AND 獎金年月 = @年 AND 分公司檔_counter = 2";
                queryParameters = new { 人員代號 = job.人員代號, 年 = year };
            }
            else
            {
                bonusQuery = @"
            SELECT * FROM 獎金發放結果檔
            WHERE 人員代號 = @人員代號 AND 獎金年月 = @年";
                queryParameters = new { 人員代號 = job.人員代號, 年 = year };
            }

            var bonuses = await countryConn.QueryAsync<dynamic>(bonusQuery, queryParameters);


            var debt = bonuses.Sum(b => (decimal)b.實發金額);
            var tax = bonuses.Sum(b => (decimal)b.稅額);

            var userInfo = new
            {
                部門 = deptName,
                代號 = user.人事代號,
                姓名 = user.姓名,
                職務 = job.職務名稱,
                時間 = DateTime.Now.ToString("yyyy-MM"),
                加項 = string.Format("{0:n0}", debt),
                應稅 = string.Format("{0:n0}", debt),
                稅額 = string.Format("{0:n0}", tax),
                實發 = string.Format("{0:n0}", debt - tax),
                發薪 = bonuses.Max(b => b.獎金年月),
                帳號 = job.轉帳帳號,
                信箱 = user.Email帳號
            };

            // 收集实际的奖金详情
            var actualDetails = bonuses.Select(b => (dynamic)new
            {
                加項項目 = b.獎金項目名稱,
                加項 = string.Format("{0:n0}", b.實發金額)
            }).ToList();
            // 确保 detailsList 至少有9个元素
            while (actualDetails.Count < 9)
            {
                actualDetails.Add(new { 加項項目 = "", 加項 = "" });
            }
            // 构建最终返回的结果
            var result = new object[] { new[] { userInfo }, actualDetails.ToArray() };
            return Ok(result);
        }


        [HttpGet("doctorDetail")]
        public async Task<IActionResult> DoctorDetail(int id, int year)
        {
            var userNoResult = await UserNo(id);
            var userNo = userNoResult.Item2;
            if (string.IsNullOrEmpty(userNo))
            {
                return NotFound("UserNo not found.");
            }

            var lastYear = year / 100 + 1911;
            var lastMonth = year % 100;
            // 直接在这里处理年份和月份，确保它们以字符串的形式被传入
            var lastMonthDate = new DateTime(lastYear, lastMonth, 1).AddMonths(-1).ToString("yyyyMM");

            decimal registerAmount = 0;
            decimal clinicAmount = 0;
            decimal admissionAmount = 0;
            decimal medicineAmount = 0;
            decimal noteAmount = 0;
            decimal ownTotal = 0;

            var register = new List<dynamic>();
            var clinic = new List<dynamic>();
            var admission = new List<dynamic>();
            var medicine = new List<dynamic>();
            var note = new List<dynamic>();

            string connectionCountryString = _configuration.GetConnectionString("CountryConnection");

            IEnumerable<dynamic> registers;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-掛'
                    )
                    ORDER BY a.就診日";
                registers = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year.ToString() });
            }

            foreach (var value in registers)
            {
                register.Add(new
                {
                    日期 = value.日期,
                    病歷號碼 = HideNumber(value.病歷號碼),
                    姓名 = HideString(value.姓名),
                    床號 = value.床號,
                    科目 = value.科目,
                    提撥 = value.提撥
                });

                registerAmount += Convert.ToDecimal(value.提撥);
            }

            IEnumerable<dynamic> clinics;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-門'
                    )
                    ORDER BY a.就診日";
                clinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year.ToString() });
            }


            foreach (var value in clinics)
            {
                clinic.Add(new
                {
                    日期 = value.日期,
                    病歷號碼 = HideNumber(value.病歷號碼),
                    姓名 = HideString(value.姓名),
                    床號 = value.床號,
                    科目 = value.科目,
                    提撥 = value.提撥
                });

                clinicAmount += Convert.ToDecimal(value.提撥);
            }


            IEnumerable<dynamic> admissions;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-住'
                    )
                    ORDER BY a.就診日";
                admissions = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year.ToString() });
            }


            foreach (var value in admissions)
            {
                admission.Add(new
                {
                    日期 = value.日期,
                    病歷號碼 = HideNumber(value.病歷號碼),
                    姓名 = HideString(value.姓名),
                    床號 = value.床號,
                    科目 = value.科目,
                    提撥 = value.提撥
                });

                admissionAmount += Convert.ToDecimal( value.提撥);
            }


            IEnumerable<dynamic> medicines;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-藥工費'
                    )
                    ORDER BY a.就診日";
                medicines = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year.ToString() });
            }

            foreach (var value in medicines)
            {
                medicine.Add(new
                {
                    日期 = value.日期,
                    病歷號碼 = HideNumber(value.病歷號碼),
                    姓名 = HideString(value.姓名),
                    床號 = value.床號,
                    科目 = value.科目,
                    提撥 = value.提撥
                });

                medicineAmount += Convert.ToDecimal(value.提撥);
            }


            IEnumerable<dynamic> counter;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT counter
                    FROM 人事資料檔
                    WHERE 人員代號 = @UserNo AND 分公司檔_counter = 2
                    GROUP BY counter";
                counter = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo });
            }

            IEnumerable<dynamic> notes;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT 備註, 異動金額
                    FROM 人事薪資異動檔
                    WHERE 人事檔_counter = @UserNo AND 異動年月 = @Year AND 項目名稱 = '當月自費' AND 備註 != ''
                    ";
                var firstCounter = counter.FirstOrDefault();
                var userCounter = firstCounter != null ? firstCounter.counter : 0;
                notes = await connection.QueryAsync<dynamic>(query, new { UserNo = userCounter, Year = year.ToString() });
            }




            foreach (var value in notes)
            {
                // 假设 value 是一个具有備註和異動金額属性的动态类型
                note.Add(new
                {
                    備註 = value.備註,
                    金額 = String.Format(CultureInfo.InvariantCulture, "{0:N0}", value.異動金額) // 使用InvariantCulture确保格式化的一致性
                });

                noteAmount += value.異動金額;
            }

            ownTotal = registerAmount + clinicAmount + admissionAmount + medicineAmount + noteAmount;

            IEnumerable<dynamic> lastClinics;

            string yearStr = lastMonthDate.ToString();

            var lastYearInt = Convert.ToInt32(yearStr.Substring(0, 4)) - 1911;
            var lastMonthStr = yearStr.Substring(4, 2);

            var formattedLastMonth = $"{lastYearInt}{lastMonthStr}";

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                SELECT b.病歷號碼, b.姓名, b.執行日期, b.健保代碼, b.項目名稱 as 治療項目, b.單價, b.總量, b.提成比例, b.提成金額
                FROM 健保醫師提成主檔 as a
                JOIN 健保醫師提成內容檔 as b ON a.counter = b.主檔_counter
                WHERE a.醫師代號 = @UserNo AND a.提成年月 = @FormattedLastMonth AND a.門住診別 = '門'";

                lastClinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, FormattedLastMonth = formattedLastMonth });
            }

            IEnumerable<dynamic> lastClinicsAmount;

            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 提成總計, 總額預扣, 補發或核減, 給付額, 總額追扣, 實發額
                FROM 健保醫師提成主檔
                WHERE 醫師代號 = @UserNo AND 提成年月 = @FormattedLastMonth AND 門住診別 = '門'";
                lastClinicsAmount = (await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, FormattedLastMonth = formattedLastMonth })).ToList();
            }




            // 格式化查询结果

            var formattedLastClinicsAmount = lastClinicsAmount.Select(x => new
            {
                提成總計 = String.Format("{0:N0}", x.提成總計),
                總額預扣 = String.Format("{0:N0}", x.總額預扣),
                補發或核減 = String.Format("{0:N0}", x.補發或核減),
                給付額 = String.Format("{0:N0}", x.給付額),
                總額追扣 = String.Format("{0:N0}", x.總額追扣),
                實發額 = String.Format("{0:N0}", x.實發額)
            }).FirstOrDefault();



            IEnumerable<dynamic> lastAdmission;


            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                SELECT b.病歷號碼, b.姓名, b.執行日期, b.健保代碼, b.項目名稱 as 治療項目, b.單價, b.總量, b.提成比例, b.提成金額
                FROM 健保醫師提成主檔 as a
                JOIN 健保醫師提成內容檔 as b ON a.counter = b.主檔_counter
                WHERE a.醫師代號 = @UserNo AND a.提成年月 = @FormattedLastMonth AND a.門住診別 = '住'";

                lastAdmission = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, FormattedLastMonth = formattedLastMonth });
            }


            List<dynamic> lastAdmissionAmount;
            using (var connection = new SqlConnection(connectionCountryString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT 提成總計, 總額預扣, 補發或核減, 給付額, 總額追扣, 實發額
                    FROM 健保醫師提成主檔
                    WHERE 醫師代號 = @No AND 提成年月 = @FormattedLastMonth AND 門住診別 = '住'";
                lastAdmissionAmount = (await connection.QueryAsync<dynamic>(query, new { No = userNo, FormattedLastMonth = formattedLastMonth })).ToList();
            }

            // 資料格式化
            var formattedLastAdmissionAmount = lastAdmissionAmount.Select(a => new
            {
                提成總計 = string.Format("{0:N0}", a.提成總計),
                總額預扣 = string.Format("{0:N0}", a.總額預扣),
                補發或核減 = string.Format("{0:N0}", a.補發或核減),
                給付額 = string.Format("{0:N0}", a.給付額),
                總額追扣 = string.Format("{0:N0}", a.總額追扣),
                實發額 = string.Format("{0:N0}", a.實發額)
            }).FirstOrDefault();

            // 整合最終輸出的資料
            var finalResult = new List<object>
            {
                register,
                string.Format("{0:N0}", registerAmount),
                clinic,
                string.Format("{0:N0}", clinicAmount),
                admission,
                string.Format("{0:N0}", admissionAmount),
                medicine,
                string.Format("{0:N0}", medicineAmount),
                note,
                string.Format("{0:N0}", noteAmount),
                string.Format("{0:N0}", ownTotal),
                lastClinics,
                formattedLastClinicsAmount != null ? formattedLastClinicsAmount : new {},
                lastAdmission,
                formattedLastAdmissionAmount != null ? formattedLastAdmissionAmount : new {}
            };

            return Ok(finalResult);
        }
        private string HideNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return number;
            var firstStr = number.Substring(0, 2);
            var lastStr = number.Length > 1 ? number.Substring(number.Length - 1) : "";
            var masked = firstStr + new string('*', Math.Max(0, number.Length - 3)) + lastStr;
            return masked;
        }
        private string HideString(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var firstChar = name.Substring(0, 1);
            var lastChar = name.Length > 1 ? name.Substring(name.Length - 1) : "";
            var masked = firstChar + new string('*', Math.Max(0, name.Length - 2)) + lastChar;
            return masked;
        }
    }
}

