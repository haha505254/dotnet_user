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

        // �ھ� id ����H�Ƹ�ƪ������Ҧr��
        public async Task<string> GetPersonnelId(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھ� id ��������Ҧr��
            var idResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT �����Ҧr�� FROM �H�Ƹ���� WHERE counter = @Id",
                new { Id = id });

            return idResult ?? string.Empty;
        }

        // �ھڨ����Ҧr������f�w�ɪ� counter
        public async Task<int> GetPatientCounter(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھڨ����Ҧr����� counter
            var counterResult = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT counter FROM �f�w�� WHERE �����Ҧr�� = @Id AND �f�����X LIKE '1%' AND (�R���_ IS NULL OR �R���_ = 0)",
                new { Id = id });

            return counterResult;
        }

        // �ھ� counter �M����d�������}�����O��
        public async Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھ� counter �M����d�������}�����O��
            var records = await connection.QueryAsync<dynamic>(
                "SELECT ���, Data1, Data2, Data3 FROM ��}�����O���� WHERE �f�w��_Counter = @Counter AND ��� BETWEEN @StrDate AND @EndDate ORDER BY ��� ASC, �ɶ� ASC",
                new { Counter = counter, StrDate = strDate, EndDate = endDate });

            return records;
        }
    }
}