using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace dotnet_user.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly IConfiguration _configuration;

        public DoctorRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ������w�������v�𰲰O��
        public async Task<List<string>> GetDoctorOff(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var offSql = @"SELECT [��v�N��] FROM [��v�N�Z��] WHERE [����] = '-2' AND [���] = @strDate";
            var off = (await connection.QueryAsync<string>(offSql, new { strDate })).ToList();

            return off;
        }

        // �����v�ɬq�O��
        public async Task<IEnumerable<dynamic>> GetDoctorRecords(string yearMonth, string week, List<string> offArray)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var recordSql = @"
                SELECT b.[�N�X���e] as [��O], c.[�N�X���e] as [�E�O], d.[�N�X���e] as [�Z�O], a.[��v�N��], e.[�m�W], a.[�Ƶ�]
                FROM [��v�ƯZ��] as a
                JOIN [�N�X��] as b ON a.[��O�N�X] = b.[�N�X]
                JOIN [�N�X��] as c ON a.[�E�O�N�X] = c.[�N�X]
                JOIN [�N�X��] as d ON a.[�Z�O�N�X] = d.[�N�X]
                JOIN [�H�Ƹ����] as e ON a.[��v�N��] = e.[�H�ƥN��]
                WHERE a.[�~���] = @yearMonth AND a.[�P���N�X] = @week
                AND a.[����] NOT IN ('-2')
                AND a.[��v�N��] NOT IN @offArray
                AND b.[�N�X�W��] = '��O�N�X'
                AND c.[�N�X�W��] = '�E�O�N�X'
                AND d.[�N�X�W��] = '�Z�O�N�X'
                ORDER BY a.[�Z�O�N�X], [��O]";

            var records = await connection.QueryAsync(recordSql, new { yearMonth, week, offArray });

            return records;
        }

        // �����v�N�Z�O��
        public async Task<IEnumerable<dynamic>> GetDoctorSecondRecords(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var recordSecondSql = @"
                SELECT b.[�N�X���e] as [��O], c.[�N�X���e] as [�E�O], e.[�N�X���e] as [�Z�O], a.[��v�N��], d.[�m�W]
                FROM [��v�N�Z��] as a
                JOIN [�N�X��] as b ON a.[��O�N�X] = b.[�N�X]
                JOIN [�N�X��] as c ON a.[�E�O�N�X] = c.[�N�X]
                JOIN [�H�Ƹ����] as d ON a.[��v�N��] = d.[�H�ƥN��]
                LEFT JOIN [�N�X��] as e ON LEFT(a.[�w���N��], 1) = e.[�N�X]
                WHERE a.[���] = @strDate AND a.[����] = '-2'
                AND b.[�N�X�W��] = '��O�N�X'
                AND c.[�N�X�W��] = '�E�O�N�X'
                AND e.[�N�X�W��] = '�Z�O�N�X'";

            var recordSecond = await connection.QueryAsync(recordSecondSql, new { strDate });

            return recordSecond;
        }
    }
}