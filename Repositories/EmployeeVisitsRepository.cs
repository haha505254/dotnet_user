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

        // ������u�ݶE�O��
        public async Task<IEnumerable<dynamic>> GetEmployeeVisitsRecords(string str_date, string end_date)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"SELECT '���u�ݶE' AS ���O, a.��v�N��, d.�m�W AS ��v�m�W, c.�f�����X, a.�w�̩m�W, b.�N�E��, b.������
                            FROM ������ AS a
                            JOIN ���E�� AS b ON a.���E��_counter = b.counter
                            JOIN �f�w�� AS c ON c.counter = a.�f�w��_counter
                            JOIN �H�Ƹ���� AS d ON a.��v�N�� = d.�H�ƥN��
                            WHERE a.������� BETWEEN @str_date AND @end_date
                            AND a.�f�w��_counter IN (SELECT counter FROM �f�w�� WHERE �����Ҧr�� IN (SELECT �����Ҧr�� FROM �H�Ƹ���� WHERE ¾�ȥN�X NOT IN (1, 7) AND ��¾�� = '' AND �����Ҧr�� <> '' AND �R���_ = 0 AND LEN(�H�ƥN��) > 3))
                            AND a.���E�N�X = '5' AND b.�O�I�O�N�X = '1' AND b.�N�����O IN ('01', '02', '04')
                            ORDER BY a.��v�N��";

            return await connection.QueryAsync(query, new { str_date, end_date });
        }

        // �����v�ݶE�O��
        public async Task<IEnumerable<dynamic>> GetDoctorVisitsRecords(string str_date, string end_date)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var query = @"SELECT '��v�ݶE' AS ���O, a.��v�N��, d.�m�W AS ��v�m�W, c.�f�����X, a.�w�̩m�W, b.�N�E��, b.������
                            FROM ������ AS a
                            JOIN ���E�� AS b ON a.���E��_counter = b.counter
                            JOIN �f�w�� AS c ON c.counter = a.�f�w��_counter
                            JOIN �H�Ƹ���� AS d ON a.��v�N�� = d.�H�ƥN��
                            WHERE a.������� BETWEEN @str_date AND @end_date
                            AND a.�f�w��_counter IN (SELECT counter FROM �f�w�� WHERE �����Ҧr�� IN (SELECT �����Ҧr�� FROM �H�Ƹ���� WHERE ¾�ȥN�X IN (1, 7) AND ��¾�� = '' AND �����Ҧr�� <> '' AND �R���_ = 0 AND LEN(�H�ƥN��) = 3))
                            AND a.���E�N�X = '5' AND a.�O�I�O�N�X = '1' AND b.�N�����O IN ('01', '02', '04')
                            ORDER BY a.��v�N��";

            return await connection.QueryAsync(query, new { str_date, end_date });
        }
    }
}