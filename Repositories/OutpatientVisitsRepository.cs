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

        // �ھڤ���d��M�ݶE�H�ƪ��e������E�ݶE�H�Ƹ��
        public async Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, int countValue)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"
                SELECT 
                    a.�������, 
                    b.�N�X���e AS �ݶE�Z�O, 
                    c.�m�W AS ��v�m�W, 
                    COUNT(a.counter) AS �ݶE�H��
                FROM ������ AS a
                JOIN �N�X�� AS b ON a.�Z�O�N�X = b.�N�X
                JOIN �H�Ƹ���� AS c ON a.��v�N�� = c.�H�ƥN��
                WHERE 
                    a.������� BETWEEN @str_date AND @end_date
                    AND a.���E�N�X = '5'
                    AND b.�N�X�W�� = '�Z�O�N�X'
                GROUP BY a.�������, b.�N�X���e, c.�m�W
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