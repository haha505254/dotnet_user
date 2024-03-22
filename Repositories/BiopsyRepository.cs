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
                SELECT a.來源代碼 as 來源, b.門診檔_counter as counter, b.counter as 處置檔, a.開單日期, d.病歷號碼, d.姓名, e.代碼內容 as 科別, f.姓名 as 醫師
                FROM 報告台結果檔 as a
                JOIN 門診處置內容檔 as b ON a.來源檔_counter = b.counter
                JOIN 門診檔 as c ON b.門診檔_counter = c.counter
                JOIN 病患檔 as d ON c.病患檔_counter = d.counter
                JOIN 代碼檔 as e ON c.科別代碼 = e.代碼
                JOIN 人事資料檔 as f ON c.醫師代號 = f.人事代號
                WHERE a.開單日期 BETWEEN @str_date AND @end_date
                AND a.流程旗標 = '送'
                AND b.處置代號 IN ('25001C', '25002C', '25003C', '25004C', '25024C', '25025C')
                AND b.備註 != ''
                AND e.代碼名稱 = '科別代碼'
                ORDER BY a.開單日期";

            var outpatientRecords = await connection.QueryAsync(outpatientQuery, new { str_date = strDate, end_date = endDate });
            return outpatientRecords;
        }

        public async Task<IEnumerable<dynamic>> GetInpatientRecords(string strDate, string endDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var inpatientQuery = @"
                SELECT a.來源代碼 as 來源, b.住診檔_counter as counter, b.counter as 處置檔, a.開單日期, d.病歷號碼, d.姓名, e.代碼內容 as 科別, f.姓名 as 醫師
                FROM 報告台結果檔 as a
                JOIN 住診處置內容檔 as b ON a.來源檔_counter = b.counter
                JOIN 住診檔 as c ON b.住診檔_counter = c.counter
                JOIN 病患檔 as d ON c.病患檔_counter = d.counter
                JOIN 代碼檔 as e ON c.入院科別代碼 = e.代碼
                JOIN 人事資料檔 as f ON c.主治醫師代號 = f.人事代號
                WHERE a.開單日期 BETWEEN @str_date AND @end_date
                AND a.流程旗標 = '送'
                AND b.處置代號 IN ('25001C', '25002C', '25003C', '25004C', '25024C', '25025C')
                AND b.備註 != ''
                AND e.代碼名稱 = '科別代碼'
                ORDER BY a.開單日期";

            var inpatientRecords = await connection.QueryAsync(inpatientQuery, new { str_date = strDate, end_date = endDate });
            return inpatientRecords;
        }

        public async Task<IEnumerable<dynamic>> GetBiopsyRemarks(int counter)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var queryA = "SELECT 備註, 結果檔_counter FROM 門診處置內容檔 WHERE counter = @counter";
            var cmdA = new SqlCommand(queryA, conn);
            cmdA.Parameters.AddWithValue("@counter", counter);

            await conn.OpenAsync();
            var remarks = await cmdA.ExecuteReaderAsync();
            var result = new List<dynamic>();

            while (await remarks.ReadAsync())
            {
                result.Add(new { 備註 = remarks["備註"].ToString(), 結果檔_counter = remarks["結果檔_counter"].ToString() });
            }

            return result;
        }

        public async Task<IEnumerable<dynamic>> GetRelatedRecords(int counter, string remark)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var queryB = "SELECT counter, 結果檔_counter FROM 門診處置內容檔 WHERE 門診檔_counter = @counter AND 結果檔_counter = @remark";
            var cmdB = new SqlCommand(queryB, conn);
            cmdB.Parameters.AddWithValue("@counter", counter);
            cmdB.Parameters.AddWithValue("@remark", remark);

            await conn.OpenAsync();
            var relatedRecords = await cmdB.ExecuteReaderAsync();
            var result = new List<dynamic>();

            while (await relatedRecords.ReadAsync())
            {
                result.Add(new { counter = relatedRecords["counter"].ToString(), 結果檔_counter = relatedRecords["結果檔_counter"].ToString() });
            }

            return result;
        }
    }
}