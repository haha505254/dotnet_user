using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class BodyRepository : IBodyRepository
    {
        private readonly IConfiguration _configuration; 
        public BodyRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 根據 id 獲取人事資料的身份證字號
        public async Task<string> GetPersonnelId(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據 id 獲取身份證字號
            var idResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT 身份證字號 FROM 人事資料檔 WHERE counter = @Id",
                new { Id = id });

            return idResult ?? string.Empty;
        }

        // 根據身份證字號獲取病患檔的 counter
        public async Task<int> GetPatientCounter(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據身份證字號獲取 counter
            var counterResult = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT counter FROM 病患檔 WHERE 身份證字號 = @Id AND 病歷號碼 LIKE '1%' AND (刪除否 IS NULL OR 刪除否 = 0)",
                new { Id = id });

            return counterResult;
        }

        // 根據 counter 和日期範圍獲取血糖血壓記錄
        public async Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據 counter 和日期範圍獲取血糖血壓記錄
            var records = await connection.QueryAsync<dynamic>(
                "SELECT 日期, Data1, Data2, Data3 FROM 血糖血壓記錄檔 WHERE 病患檔_Counter = @Counter AND 日期 BETWEEN @StrDate AND @EndDate ORDER BY 日期 ASC, 時間 ASC",
                new { Counter = counter, StrDate = strDate, EndDate = endDate });

            return records;
        }
    }
}