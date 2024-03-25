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

            string query = @"SELECT LEFT(�E�_���, 5) AS ���, COUNT(*) AS �ƶq
                             FROM �E�_�ҩ���
                             WHERE �E�_��� BETWEEN @strDate AND @endDate
                             GROUP BY LEFT(�E�_���, 5)
                             ORDER BY ���";

            var records = await connection.QueryAsync<dynamic>(query, new { strDate, endDate });
            return records;
        }

        public async Task<IEnumerable<dynamic>> GetDiagnosisCountByDoctorAndMonth(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            string query = @"SELECT LEFT(a.�E�_���, 5) AS ���, b.�m�W, COUNT(*) AS �ƶq
                             FROM �E�_�ҩ��� AS a
                             JOIN �H�Ƹ���� AS b ON a.��v�N�� = b.�H�ƥN��
                             WHERE a.�E�_��� BETWEEN @strDate AND @endDate
                             GROUP BY LEFT(a.�E�_���, 5), b.�m�W
                             ORDER BY LEFT(a.�E�_���, 5), b.�m�W";

            var records = await connection.QueryAsync<dynamic>(query, new { strDate, endDate });
            return records;
        }
    }
}