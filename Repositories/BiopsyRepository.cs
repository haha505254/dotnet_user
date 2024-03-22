using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace dotnet_user.Repositories
{
    public class BiopsyRepository : IBiopsyRepository
    {
        private readonly IConfiguration _configuration;

        public BiopsyRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<dynamic>> GetOutpatientRecords(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var outpatientQuery = @"
                SELECT a.�ӷ��N�X as �ӷ�, b.���E��_counter as counter, b.counter as �B�m��, a.�}����, d.�f�����X, d.�m�W, e.�N�X���e as ��O, f.�m�W as ��v
                FROM ���i�x���G�� as a
                JOIN ���E�B�m���e�� as b ON a.�ӷ���_counter = b.counter
                JOIN ���E�� as c ON b.���E��_counter = c.counter
                JOIN �f�w�� as d ON c.�f�w��_counter = d.counter
                JOIN �N�X�� as e ON c.��O�N�X = e.�N�X
                JOIN �H�Ƹ���� as f ON c.��v�N�� = f.�H�ƥN��
                WHERE a.�}���� BETWEEN @str_date AND @end_date
                AND a.�y�{�X�� = '�e'
                AND b.�B�m�N�� IN ('25001C', '25002C', '25003C', '25004C', '25024C', '25025C')
                AND b.�Ƶ� != ''
                AND e.�N�X�W�� = '��O�N�X'
                ORDER BY a.�}����";

            var outpatientRecords = await connection.QueryAsync(outpatientQuery, new { str_date = strDate, end_date = endDate });
            return outpatientRecords;
        }

        public async Task<IEnumerable<dynamic>> GetInpatientRecords(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var inpatientQuery = @"
                SELECT a.�ӷ��N�X as �ӷ�, b.��E��_counter as counter, b.counter as �B�m��, a.�}����, d.�f�����X, d.�m�W, e.�N�X���e as ��O, f.�m�W as ��v
                FROM ���i�x���G�� as a
                JOIN ��E�B�m���e�� as b ON a.�ӷ���_counter = b.counter
                JOIN ��E�� as c ON b.��E��_counter = c.counter
                JOIN �f�w�� as d ON c.�f�w��_counter = d.counter
                JOIN �N�X�� as e ON c.�J�|��O�N�X = e.�N�X
                JOIN �H�Ƹ���� as f ON c.�D�v��v�N�� = f.�H�ƥN��
                WHERE a.�}���� BETWEEN @str_date AND @end_date
                AND a.�y�{�X�� = '�e'
                AND b.�B�m�N�� IN ('25001C', '25002C', '25003C', '25004C', '25024C', '25025C')
                AND b.�Ƶ� != ''
                AND e.�N�X�W�� = '��O�N�X'
                ORDER BY a.�}����";

            var inpatientRecords = await connection.QueryAsync(inpatientQuery, new { str_date = strDate, end_date = endDate });
            return inpatientRecords;
        }

        public async Task<IEnumerable<dynamic>> GetBiopsyRemarks(int counter)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var queryA = "SELECT �Ƶ�, ���G��_counter FROM ���E�B�m���e�� WHERE counter = @counter";
            var cmdA = new SqlCommand(queryA, conn);
            cmdA.Parameters.AddWithValue("@counter", counter);

            await conn.OpenAsync();
            var remarks = await cmdA.ExecuteReaderAsync();
            var result = new List<dynamic>();

            while (await remarks.ReadAsync())
            {
                result.Add(new { �Ƶ� = remarks["�Ƶ�"].ToString(), ���G��_counter = remarks["���G��_counter"].ToString() });
            }

            return result;
        }

        public async Task<IEnumerable<dynamic>> GetRelatedRecords(int counter, string remark)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var queryB = "SELECT counter, ���G��_counter FROM ���E�B�m���e�� WHERE ���E��_counter = @counter AND ���G��_counter = @remark";
            var cmdB = new SqlCommand(queryB, conn);
            cmdB.Parameters.AddWithValue("@counter", counter);
            cmdB.Parameters.AddWithValue("@remark", remark);

            await conn.OpenAsync();
            var relatedRecords = await cmdB.ExecuteReaderAsync();
            var result = new List<dynamic>();

            while (await relatedRecords.ReadAsync())
            {
                result.Add(new { counter = relatedRecords["counter"].ToString(), ���G��_counter = relatedRecords["���G��_counter"].ToString() });
            }

            return result;
        }
    }
}