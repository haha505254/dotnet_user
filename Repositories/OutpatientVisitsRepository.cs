using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using dotnet_user.Repositories.Interface;
using Microsoft.Extensions.Configuration;

namespace dotnet_user.Repositories
{
    public class OutpatientVisitsRepository : IOutpatientVisitsRepository
    {
        private readonly IConfiguration _configuration;

        public OutpatientVisitsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 根據日期範圍和看診人數門檻獲取門診看診人數資料
        public async Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, int countValue)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"
                SELECT 
                    a.掛號日期, 
                    b.代碼內容 AS 看診班別, 
                    c.姓名 AS 醫師姓名, 
                    COUNT(a.counter) AS 看診人數
                FROM 掛號檔 AS a
                JOIN 代碼檔 AS b ON a.班別代碼 = b.代碼
                JOIN 人事資料檔 AS c ON a.醫師代號 = c.人事代號
                WHERE 
                    a.掛號日期 BETWEEN @str_date AND @end_date
                    AND a.完診代碼 = '5'
                    AND b.代碼名稱 = '班別代碼'
                GROUP BY a.掛號日期, b.代碼內容, c.姓名
                HAVING COUNT(a.counter) >= @countValue";

            var parameters = new
            {
                str_date,
                end_date,
                countValue
            };

            var result = await connection.QueryAsync<dynamic>(query, parameters);
            return result;
        }
    }
}