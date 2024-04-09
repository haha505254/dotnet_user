using Dapper;
using dotnet_user.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;

namespace dotnet_user.Services
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly IConfiguration _configuration;
        public SalaryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<dynamic> UserInfo(int id)
        {
            using var tgsqlconnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            var user = await tgsqlconnection.QueryAsync("SELECT * FROM 人事資料檔 WHERE counter = @Id", new { Id = id });
            return user.FirstOrDefault() ?? new { };
        }

        public async Task<(int, string)> UserNo(int id)
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

        public async Task<bool> CheckPassword(string idNumber, string password)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var passwordQuery = "SELECT 姓名, 身份證號, 第二密碼 FROM 人事資料檔 WHERE (第二密碼 IS NOT NULL OR 第二密碼 != '') AND LEN(身份證號) = 10 AND 身份證號 = @IdNumber AND 第二密碼 = @Password";
            var passwordResult = await connection.QueryAsync(passwordQuery, new { IdNumber = idNumber, Password = password });

            return passwordResult.Any();
        }

        public async Task<IEnumerable<dynamic>> GetSalary(int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var salary = await connection.QueryAsync($"EXEC [dbo].[proc_查個人薪資發放年月]'{year}', '{personnelNumber}'");
            return salary;
        }

        public async Task<IEnumerable<dynamic>> GetBonus(int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var bonus = await connection.QueryAsync($"EXEC [dbo].[proc_查個人獎金發放年月]'{year}', '{personnelNumber}'");
            return bonus;
        }

        public async Task<string> GetDepartmentName(string departmentCode)
        {
            using var tgsqlConn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            var deptQuery = "SELECT 單位名稱 FROM 病歷單位代號檔 WHERE 單位代號 = @部門別";
            var dept = await tgsqlConn.QueryFirstOrDefaultAsync<dynamic>(deptQuery, new { 部門別 = departmentCode });
            return dept?.單位名稱 ?? string.Empty;
        }

        public async Task<dynamic> GetJobInformation(string idNumber)
        {
            using var countryConn = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var jobQuery = @"
                SELECT 職務名稱 
                FROM 人事資料檔
                WHERE 身份證號 = @身份證字號
                AND 離職日 = ''  
                AND (職務名稱 IS NOT NULL OR 職務名稱 != '')
                GROUP BY 職務名稱";
            var job = await countryConn.QueryFirstOrDefaultAsync<dynamic>(jobQuery, new { 身份證字號 = idNumber });
            return job ?? new { };
        }

        public async Task<IEnumerable<dynamic>> GetSalaryDetails(int counter, int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var salaryQuery = "EXEC [dbo].[proc_薪資發放結果] @Counter, @Year, @PersonnelNumber";
            var salary = await connection.QueryAsync<dynamic>(salaryQuery, new { Counter = counter, Year = year, PersonnelNumber = personnelNumber });
            return salary;
        }

        public async Task<(string, object)> GetBonusQuery(int jobCode, string idNumber)
        {
            string bonusQuery;
            object queryParameters;

            if (jobCode == 1 || jobCode == 7)
            {
                bonusQuery = @"
            SELECT * FROM 獎金發放結果檔
            WHERE 人員代號 = @人員代號 AND 獎金年月 = @年 AND 分公司檔_counter = 2";
                queryParameters = new { 人員代號 = idNumber, 年 = DateTime.Now.Year };
            }
            else
            {
                bonusQuery = @"
            SELECT * FROM 獎金發放結果檔
            WHERE 人員代號 = @人員代號 AND 獎金年月 = @年";
                queryParameters = new { 人員代號 = idNumber, 年 = DateTime.Now.Year };
            }

            await Task.Delay(1);

            return (bonusQuery, queryParameters);
        }

        public async Task<IEnumerable<dynamic>> GetBonuses(string query, object parameters)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var bonuses = await connection.QueryAsync<dynamic>(query, parameters);
            return bonuses;
        }

        public async Task<List<dynamic>> GetRegisterDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-掛'
                    )
                    ORDER BY a.就診日";
            var registers = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return registers.ToList();
        }

        public async Task<List<dynamic>> GetClinicDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                    SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                    FROM [country].[dbo].[醫師提成內容檔] as a
                    JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                    WHERE a.主檔_counter IN (
                        SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-門'
                    )
                    ORDER BY a.就診日";
            var clinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return clinics.ToList();
        }

        public async Task<List<dynamic>> GetAdmissionDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                FROM [country].[dbo].[醫師提成內容檔] as a
                JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                WHERE a.主檔_counter IN (
                SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-住'
                )
                ORDER BY a.就診日";
            var admissions = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return admissions.ToList();
        }
    public async Task<List<dynamic>> GetMedicineDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT a.就診日 as 日期, b.病歷號碼, b.姓名, a.床號, a.處置簡稱 as 科目, a.提成金額 as 提撥
                FROM [country].[dbo].[醫師提成內容檔] as a
                JOIN [hpserver].[tgsql].[dbo].[病患檔] as b ON a.病患檔_counter = b.counter
                WHERE a.主檔_counter IN (
                    SELECT counter FROM [country].[dbo].[醫師提成主檔] WHERE 人事代號 = @UserNo AND 提成區間_起 LIKE @Year + '%' AND 提成項目 = '當月自費-藥工費'
                )
                ORDER BY a.就診日";
            var medicines = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return medicines.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetPersonnelCounter(string userNo)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT counter
                FROM 人事資料檔
                WHERE 人員代號 = @UserNo AND 分公司檔_counter = 2
                GROUP BY counter";
            var counter = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo });
            return counter;
        }

        public async Task<IEnumerable<dynamic>> GetNotes(int counter, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT 備註, 異動金額
                FROM 人事薪資異動檔
                WHERE 人事檔_counter = @UserNo AND 異動年月 = @Year AND 項目名稱 = '當月自費' AND 備註 != ''
                ";
            var notes = await connection.QueryAsync<dynamic>(query, new { UserNo = counter, Year = year });
            return notes;
        }

        public async Task<IEnumerable<dynamic>> GetLastClinics(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT b.病歷號碼, b.姓名, b.執行日期, b.健保代碼, b.項目名稱 as 治療項目, b.單價, b.總量, b.提成比例, b.提成金額
            FROM 健保醫師提成主檔 as a
            JOIN 健保醫師提成內容檔 as b ON a.counter = b.主檔_counter
            WHERE a.醫師代號 = @UserNo AND a.提成年月 = @Year AND a.門住診別 = '門'";

            var lastClinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return lastClinics;
        }

        public async Task<List<dynamic>> GetLastClinicsAmount(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT 提成總計, 總額預扣, 補發或核減, 給付額, 總額追扣, 實發額
            FROM 健保醫師提成主檔
            WHERE 醫師代號 = @UserNo AND 提成年月 = @Year AND 門住診別 = '門'";
            var lastClinicsAmount = (await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year })).ToList();
            return lastClinicsAmount;
        }

        public async Task<IEnumerable<dynamic>> GetLastAdmission(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT b.病歷號碼, b.姓名, b.執行日期, b.健保代碼, b.項目名稱 as 治療項目, b.單價, b.總量, b.提成比例, b.提成金額
            FROM 健保醫師提成主檔 as a
            JOIN 健保醫師提成內容檔 as b ON a.counter = b.主檔_counter
            WHERE a.醫師代號 = @UserNo AND a.提成年月 = @Year AND a.門住診別 = '住'";

            var lastAdmission = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return lastAdmission;
        }

        public async Task<List<dynamic>> GetLastAdmissionAmount(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT 提成總計, 總額預扣, 補發或核減, 給付額, 總額追扣, 實發額
                FROM 健保醫師提成主檔
                WHERE 醫師代號 = @No AND 提成年月 = @Year AND 門住診別 = '住'";
            var lastAdmissionAmount = (await connection.QueryAsync<dynamic>(query, new { No = userNo, Year = year })).ToList();
            return lastAdmissionAmount;
        }


        public async Task UpdateEmail(int id, string email)
        {
            using var tgsqlconnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            await tgsqlconnection.ExecuteAsync("UPDATE 人事資料檔 SET Email帳號 = @Email WHERE counter = @Id", new { Email = email, Id = id });
        }

        public async Task<bool[]> SendEmail(dynamic[] to, string title, string content)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("宏恩醫療財團法人宏恩綜合醫院", "country@country.org.tw"));
            message.To.AddRange(to.Select(t => new MailboxAddress(t.name, t.email)));
            message.Subject = title;

            var builder = new BodyBuilder
            {
                HtmlBody = content
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync("mail.country.org.tw", 25, SecureSocketOptions.None);
            await client.AuthenticateAsync("country_mis", "306578ooo");

            var sendTasks = to.Select(t => client.SendAsync(message));
            var results = await Task.WhenAll(sendTasks);
            await client.DisconnectAsync(true);

            return results.Select(r => r == "Ok").ToArray();
        }

    }
}