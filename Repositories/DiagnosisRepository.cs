using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace dotnet_user.Repositories
{
    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly IConfiguration _configuration;

        public DiagnosisRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<dynamic>> GetDiagnosisCountByMonth(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            string query = @"SELECT LEFT(診斷日期, 5) AS 月份, COUNT(*) AS 數量
                             FROM 診斷證明檔
                             WHERE 診斷日期 BETWEEN @strDate AND @endDate
                             GROUP BY LEFT(診斷日期, 5)
                             ORDER BY 月份";

            var records = await connection.QueryAsync<dynamic>(query, new { strDate, endDate });
            return records;
        }

        public async Task<IEnumerable<dynamic>> GetDiagnosisCountByDoctorAndMonth(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            string query = @"SELECT LEFT(a.診斷日期, 5) AS 月份, b.姓名, COUNT(*) AS 數量
                             FROM 診斷證明檔 AS a
                             JOIN 人事資料檔 AS b ON a.醫師代號 = b.人事代號
                             WHERE a.診斷日期 BETWEEN @strDate AND @endDate
                             GROUP BY LEFT(a.診斷日期, 5), b.姓名
                             ORDER BY LEFT(a.診斷日期, 5), b.姓名";

            var records = await connection.QueryAsync<dynamic>(query, new { strDate, endDate });
            return records;
        }
    }
}