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

        // 獲取指定日期的醫師休假記錄
        public async Task<List<string>> GetDoctorOff(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var offSql = @"SELECT [醫師代號] FROM [醫師代班表] WHERE [限數] = '-2' AND [日期] = @strDate";
            var off = (await connection.QueryAsync<string>(offSql, new { strDate })).ToList();

            return off;
        }

        // 獲取醫師時段記錄
        public async Task<IEnumerable<dynamic>> GetDoctorRecords(string yearMonth, string week, List<string> offArray)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var recordSql = @"
                SELECT b.[代碼內容] as [科別], c.[代碼內容] as [診別], d.[代碼內容] as [班別], a.[醫師代號], e.[姓名], a.[備註]
                FROM [醫師排班表] as a
                JOIN [代碼檔] as b ON a.[科別代碼] = b.[代碼]
                JOIN [代碼檔] as c ON a.[診別代碼] = c.[代碼]
                JOIN [代碼檔] as d ON a.[班別代碼] = d.[代碼]
                JOIN [人事資料檔] as e ON a.[醫師代號] = e.[人事代號]
                WHERE a.[年月份] = @yearMonth AND a.[星期代碼] = @week
                AND a.[限數] NOT IN ('-2')
                AND a.[醫師代號] NOT IN @offArray
                AND b.[代碼名稱] = '科別代碼'
                AND c.[代碼名稱] = '診別代碼'
                AND d.[代碼名稱] = '班別代碼'
                ORDER BY a.[班別代碼], [科別]";

            var records = await connection.QueryAsync(recordSql, new { yearMonth, week, offArray });

            return records;
        }

        // 獲取醫師代班記錄
        public async Task<IEnumerable<dynamic>> GetDoctorSecondRecords(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var recordSecondSql = @"
                SELECT b.[代碼內容] as [科別], c.[代碼內容] as [診別], e.[代碼內容] as [班別], a.[醫師代號], d.[姓名]
                FROM [醫師代班表] as a
                JOIN [代碼檔] as b ON a.[科別代碼] = b.[代碼]
                JOIN [代碼檔] as c ON a.[診別代碼] = c.[代碼]
                JOIN [人事資料檔] as d ON a.[醫師代號] = d.[人事代號]
                LEFT JOIN [代碼檔] as e ON LEFT(a.[預約代號], 1) = e.[代碼]
                WHERE a.[日期] = @strDate AND a.[限數] = '-2'
                AND b.[代碼名稱] = '科別代碼'
                AND c.[代碼名稱] = '診別代碼'
                AND e.[代碼名稱] = '班別代碼'";

            var recordSecond = await connection.QueryAsync(recordSecondSql, new { strDate });

            return recordSecond;
        }
    }
}