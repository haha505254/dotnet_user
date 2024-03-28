using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class EmployeeVisitsRepository : IEmployeeVisitsRepository
    {
        private readonly IConfiguration _configuration;

        public EmployeeVisitsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 獲取員工看診記錄
        public async Task<IEnumerable<dynamic>> GetEmployeeVisitsRecords(string str_date, string end_date)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"SELECT '員工看診' AS 類別, a.醫師代號, d.姓名 AS 醫師姓名, c.病歷號碼, a.患者姓名, b.就診日, b.結束日
                            FROM 掛號檔 AS a
                            JOIN 門診檔 AS b ON a.門診檔_counter = b.counter
                            JOIN 病患檔 AS c ON c.counter = a.病患檔_counter
                            JOIN 人事資料檔 AS d ON a.醫師代號 = d.人事代號
                            WHERE a.掛號日期 BETWEEN @str_date AND @end_date
                            AND a.病患檔_counter IN (SELECT counter FROM 病患檔 WHERE 身份證字號 IN (SELECT 身份證字號 FROM 人事資料檔 WHERE 職務代碼 NOT IN (1, 7) AND 離職日 = '' AND 身份證字號 <> '' AND 刪除否 = 0 AND LEN(人事代號) > 3))
                            AND a.完診代碼 = '5' AND b.保險別代碼 = '1' AND b.就醫類別 IN ('01', '02', '04')
                            ORDER BY a.醫師代號";

            return await connection.QueryAsync(query, new { str_date, end_date });
        }

        // 獲取醫師看診記錄
        public async Task<IEnumerable<dynamic>> GetDoctorVisitsRecords(string str_date, string end_date)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"SELECT '醫師看診' AS 類別, a.醫師代號, d.姓名 AS 醫師姓名, c.病歷號碼, a.患者姓名, b.就診日, b.結束日
                            FROM 掛號檔 AS a
                            JOIN 門診檔 AS b ON a.門診檔_counter = b.counter
                            JOIN 病患檔 AS c ON c.counter = a.病患檔_counter
                            JOIN 人事資料檔 AS d ON a.醫師代號 = d.人事代號
                            WHERE a.掛號日期 BETWEEN @str_date AND @end_date
                            AND a.病患檔_counter IN (SELECT counter FROM 病患檔 WHERE 身份證字號 IN (SELECT 身份證字號 FROM 人事資料檔 WHERE 職務代碼 IN (1, 7) AND 離職日 = '' AND 身份證字號 <> '' AND 刪除否 = 0 AND LEN(人事代號) = 3))
                            AND a.完診代碼 = '5' AND a.保險別代碼 = '1' AND b.就醫類別 IN ('01', '02', '04')
                            ORDER BY a.醫師代號";

            return await connection.QueryAsync(query, new { str_date, end_date });
        }
    }
}