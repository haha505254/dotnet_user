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
            var user = await tgsqlconnection.QueryAsync("SELECT * FROM �H�Ƹ���� WHERE counter = @Id", new { Id = id });
            return user.FirstOrDefault() ?? new { };
        }

        public async Task<(int, string)> UserNo(int id)
        {
            var user = await UserInfo(id);
            int counter = (user.¾�ȥN�X == 1 || user.¾�ȥN�X == 7) && user.�H�ƥN��.Length == 3 && user.�����O != "B71." ? 2 : 1;
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var no = await connection.QueryAsync("SELECT �H���N�� FROM �H�Ƹ���� WHERE �����Ҹ� = @IdNumber AND �����q��_counter = @Counter GROUP BY �H���N��, ��¾�� ORDER BY ��¾�� DESC", new { IdNumber = user.�����Ҧr��, Counter = counter });
            var firstNo = no.FirstOrDefault();
            if (firstNo == null)
            {
                return (counter, string.Empty);
            }

            return (counter, firstNo.�H���N��);
        }

        public async Task<bool> CheckPassword(string idNumber, string password)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var passwordQuery = "SELECT �m�W, �����Ҹ�, �ĤG�K�X FROM �H�Ƹ���� WHERE (�ĤG�K�X IS NOT NULL OR �ĤG�K�X != '') AND LEN(�����Ҹ�) = 10 AND �����Ҹ� = @IdNumber AND �ĤG�K�X = @Password";
            var passwordResult = await connection.QueryAsync(passwordQuery, new { IdNumber = idNumber, Password = password });

            return passwordResult.Any();
        }

        public async Task<IEnumerable<dynamic>> GetSalary(int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var salary = await connection.QueryAsync($"EXEC [dbo].[proc_�d�ӤH�~��o��~��]'{year}', '{personnelNumber}'");
            return salary;
        }

        public async Task<IEnumerable<dynamic>> GetBonus(int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var bonus = await connection.QueryAsync($"EXEC [dbo].[proc_�d�ӤH�����o��~��]'{year}', '{personnelNumber}'");
            return bonus;
        }

        public async Task<string> GetDepartmentName(string departmentCode)
        {
            using var tgsqlConn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            var deptQuery = "SELECT ���W�� FROM �f�����N���� WHERE ���N�� = @�����O";
            var dept = await tgsqlConn.QueryFirstOrDefaultAsync<dynamic>(deptQuery, new { �����O = departmentCode });
            return dept?.���W�� ?? string.Empty;
        }

        public async Task<dynamic> GetJobInformation(string idNumber)
        {
            using var countryConn = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var jobQuery = @"
                SELECT ¾�ȦW�� 
                FROM �H�Ƹ����
                WHERE �����Ҹ� = @�����Ҧr��
                AND ��¾�� = ''  
                AND (¾�ȦW�� IS NOT NULL OR ¾�ȦW�� != '')
                GROUP BY ¾�ȦW��";
            var job = await countryConn.QueryFirstOrDefaultAsync<dynamic>(jobQuery, new { �����Ҧr�� = idNumber });
            return job ?? new { };
        }

        public async Task<IEnumerable<dynamic>> GetSalaryDetails(int counter, int year, string personnelNumber)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var salaryQuery = "EXEC [dbo].[proc_�~��o�񵲪G] @Counter, @Year, @PersonnelNumber";
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
            SELECT * FROM �����o�񵲪G��
            WHERE �H���N�� = @�H���N�� AND �����~�� = @�~ AND �����q��_counter = 2";
                queryParameters = new { �H���N�� = idNumber, �~ = DateTime.Now.Year };
            }
            else
            {
                bonusQuery = @"
            SELECT * FROM �����o�񵲪G��
            WHERE �H���N�� = @�H���N�� AND �����~�� = @�~";
                queryParameters = new { �H���N�� = idNumber, �~ = DateTime.Now.Year };
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
                    SELECT a.�N�E�� as ���, b.�f�����X, b.�m�W, a.�ɸ�, a.�B�m²�� as ���, a.�������B as ����
                    FROM [country].[dbo].[��v�������e��] as a
                    JOIN [hpserver].[tgsql].[dbo].[�f�w��] as b ON a.�f�w��_counter = b.counter
                    WHERE a.�D��_counter IN (
                        SELECT counter FROM [country].[dbo].[��v�����D��] WHERE �H�ƥN�� = @UserNo AND �����϶�_�_ LIKE @Year + '%' AND �������� = '���۶O-��'
                    )
                    ORDER BY a.�N�E��";
            var registers = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return registers.ToList();
        }

        public async Task<List<dynamic>> GetClinicDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                    SELECT a.�N�E�� as ���, b.�f�����X, b.�m�W, a.�ɸ�, a.�B�m²�� as ���, a.�������B as ����
                    FROM [country].[dbo].[��v�������e��] as a
                    JOIN [hpserver].[tgsql].[dbo].[�f�w��] as b ON a.�f�w��_counter = b.counter
                    WHERE a.�D��_counter IN (
                        SELECT counter FROM [country].[dbo].[��v�����D��] WHERE �H�ƥN�� = @UserNo AND �����϶�_�_ LIKE @Year + '%' AND �������� = '���۶O-��'
                    )
                    ORDER BY a.�N�E��";
            var clinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return clinics.ToList();
        }

        public async Task<List<dynamic>> GetAdmissionDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT a.�N�E�� as ���, b.�f�����X, b.�m�W, a.�ɸ�, a.�B�m²�� as ���, a.�������B as ����
                FROM [country].[dbo].[��v�������e��] as a
                JOIN [hpserver].[tgsql].[dbo].[�f�w��] as b ON a.�f�w��_counter = b.counter
                WHERE a.�D��_counter IN (
                SELECT counter FROM [country].[dbo].[��v�����D��] WHERE �H�ƥN�� = @UserNo AND �����϶�_�_ LIKE @Year + '%' AND �������� = '���۶O-��'
                )
                ORDER BY a.�N�E��";
            var admissions = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return admissions.ToList();
        }
    public async Task<List<dynamic>> GetMedicineDetail(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT a.�N�E�� as ���, b.�f�����X, b.�m�W, a.�ɸ�, a.�B�m²�� as ���, a.�������B as ����
                FROM [country].[dbo].[��v�������e��] as a
                JOIN [hpserver].[tgsql].[dbo].[�f�w��] as b ON a.�f�w��_counter = b.counter
                WHERE a.�D��_counter IN (
                    SELECT counter FROM [country].[dbo].[��v�����D��] WHERE �H�ƥN�� = @UserNo AND �����϶�_�_ LIKE @Year + '%' AND �������� = '���۶O-�Ĥu�O'
                )
                ORDER BY a.�N�E��";
            var medicines = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return medicines.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetPersonnelCounter(string userNo)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT counter
                FROM �H�Ƹ����
                WHERE �H���N�� = @UserNo AND �����q��_counter = 2
                GROUP BY counter";
            var counter = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo });
            return counter;
        }

        public async Task<IEnumerable<dynamic>> GetNotes(int counter, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT �Ƶ�, ���ʪ��B
                FROM �H���~�겧����
                WHERE �H����_counter = @UserNo AND ���ʦ~�� = @Year AND ���ئW�� = '���۶O' AND �Ƶ� != ''
                ";
            var notes = await connection.QueryAsync<dynamic>(query, new { UserNo = counter, Year = year });
            return notes;
        }

        public async Task<IEnumerable<dynamic>> GetLastClinics(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT b.�f�����X, b.�m�W, b.������, b.���O�N�X, b.���ئW�� as �v������, b.���, b.�`�q, b.�������, b.�������B
            FROM ���O��v�����D�� as a
            JOIN ���O��v�������e�� as b ON a.counter = b.�D��_counter
            WHERE a.��v�N�� = @UserNo AND a.�����~�� = @Year AND a.����E�O = '��'";

            var lastClinics = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return lastClinics;
        }

        public async Task<List<dynamic>> GetLastClinicsAmount(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT �����`�p, �`�B�w��, �ɵo�ήִ�, ���I�B, �`�B�l��, ��o�B
            FROM ���O��v�����D��
            WHERE ��v�N�� = @UserNo AND �����~�� = @Year AND ����E�O = '��'";
            var lastClinicsAmount = (await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year })).ToList();
            return lastClinicsAmount;
        }

        public async Task<IEnumerable<dynamic>> GetLastAdmission(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
            SELECT b.�f�����X, b.�m�W, b.������, b.���O�N�X, b.���ئW�� as �v������, b.���, b.�`�q, b.�������, b.�������B
            FROM ���O��v�����D�� as a
            JOIN ���O��v�������e�� as b ON a.counter = b.�D��_counter
            WHERE a.��v�N�� = @UserNo AND a.�����~�� = @Year AND a.����E�O = '��'";

            var lastAdmission = await connection.QueryAsync<dynamic>(query, new { UserNo = userNo, Year = year });
            return lastAdmission;
        }

        public async Task<List<dynamic>> GetLastAdmissionAmount(string userNo, string year)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            var query = @"
                SELECT �����`�p, �`�B�w��, �ɵo�ήִ�, ���I�B, �`�B�l��, ��o�B
                FROM ���O��v�����D��
                WHERE ��v�N�� = @No AND �����~�� = @Year AND ����E�O = '��'";
            var lastAdmissionAmount = (await connection.QueryAsync<dynamic>(query, new { No = userNo, Year = year })).ToList();
            return lastAdmissionAmount;
        }


        public async Task UpdateEmail(int id, string email)
        {
            using var tgsqlconnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));
            await tgsqlconnection.ExecuteAsync("UPDATE �H�Ƹ���� SET Email�b�� = @Email WHERE counter = @Id", new { Email = email, Id = id });
        }

        public async Task<bool[]> SendEmail(dynamic[] to, string title, string content)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("���������]�Ϊk�H������X��|", "country@country.org.tw"));
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