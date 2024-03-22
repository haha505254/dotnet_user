using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace dotnet_user.Repositories
{
    public class BodyRepository : IBodyRepository
    {
        private readonly IConfiguration _configuration;

        public BodyRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetPersonnelId(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var idResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT 身份證字號 FROM 人事資料檔 WHERE counter = @Id",
                new { Id = id });

            return idResult ?? string.Empty;
        }

        public async Task<int> GetPatientCounter(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var counterResult = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT counter FROM 病患檔 WHERE 身份證字號 = @Id AND 病歷號碼 LIKE '1%' AND (刪除否 IS NULL OR 刪除否 = 0)",
                new { Id = id });

            return counterResult;
        }

        public async Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var records = await connection.QueryAsync<dynamic>(
                "SELECT 日期, Data1, Data2, Data3 FROM 血糖血壓記錄檔 WHERE 病患檔_Counter = @Counter AND 日期 BETWEEN @StrDate AND @EndDate ORDER BY 日期 ASC, 時間 ASC",
                new { Counter = counter, StrDate = strDate, EndDate = endDate });

            return records;
        }
    }
}