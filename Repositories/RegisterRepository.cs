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

        // 根據日期獲取預掛名單記錄
        public async Task<IEnumerable<dynamic>> GetRegisterRecords(string strDate)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據日期獲取預掛名單記錄
            var query = @"
                SELECT b.病歷號碼, b.身份證字號, a.患者姓名, a.掛號序號, c.代碼內容 as 科別, d.代碼內容 as 診別, e.代碼內容 as 班別, f.姓名
                FROM 掛號檔 as a
                JOIN 病患檔 as b ON a.病患檔_counter = b.counter
                JOIN 代碼檔 as c ON a.科別代碼 = c.代碼
                JOIN 代碼檔 as d ON a.診別代碼 = d.代碼
                JOIN 代碼檔 as e ON a.班別代碼 = e.代碼
                JOIN 人事資料檔 as f ON a.醫師代號 = f.人事代號
                WHERE a.掛號日期 = @str_date AND a.分派類別 = 'A' AND a.科別代碼 NOT IN (27, 28) AND
                c.代碼名稱 = '科別代碼' AND d.代碼名稱 = '診別代碼' AND e.代碼名稱 = '班別代碼'";

            var records = await connection.QueryAsync<dynamic>(query, new { str_date = strDate });
            return records;
        }
    }
}