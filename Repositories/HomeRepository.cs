using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly IConfiguration _configuration;

        public HomeRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 根據 id 獲取部門別
        public async Task<string> GetDepartment(int? id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據 id 獲取部門別
            var deptResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT 部門別 FROM 人事資料檔 WHERE counter = @Id",
                new { Id = id ?? 0 });

            return deptResult ?? "guest";
        }

        // 根據部門別獲取用戶權限
        public async Task<IEnumerable<dynamic>> GetUserRights(string dept)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgvisConnection"));

            // 執行 SQL 查詢,根據部門別獲取用戶權限
            var userRights = await connection.QueryAsync<dynamic>(
                "SELECT a.dept, b.name, b.icon FROM adminUserRight as a " +
                "JOIN adminPage as b ON a.pageNo = b.no " +
                "WHERE a.dept = @Dept",
                new { Dept = dept });

            return userRights;
        }
    }
}