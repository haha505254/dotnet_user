using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class RegisterRepository : IRegisterRepository
    {
        private readonly IConfiguration _configuration;

        public RegisterRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // �ھڤ������w���W��O��
        public async Task<IEnumerable<dynamic>> GetRegisterRecords(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھڤ������w���W��O��
            var query = @"
                SELECT b.�f�����X, b.�����Ҧr��, a.�w�̩m�W, a.�����Ǹ�, c.�N�X���e as ��O, d.�N�X���e as �E�O, e.�N�X���e as �Z�O, f.�m�W
                FROM ������ as a
                JOIN �f�w�� as b ON a.�f�w��_counter = b.counter
                JOIN �N�X�� as c ON a.��O�N�X = c.�N�X
                JOIN �N�X�� as d ON a.�E�O�N�X = d.�N�X
                JOIN �N�X�� as e ON a.�Z�O�N�X = e.�N�X
                JOIN �H�Ƹ���� as f ON a.��v�N�� = f.�H�ƥN��
                WHERE a.������� = @str_date AND a.�������O = 'A' AND a.��O�N�X NOT IN (27, 28) AND
                c.�N�X�W�� = '��O�N�X' AND d.�N�X�W�� = '�E�O�N�X' AND e.�N�X�W�� = '�Z�O�N�X'";

            var records = await connection.QueryAsync<dynamic>(query, new { str_date = strDate });
            return records;
        }
    }
}