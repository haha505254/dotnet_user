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

        // �ھ� id ��������O
        public async Task<string> GetDepartment(int? id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھ� id ��������O
            var deptResult = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT �����O FROM �H�Ƹ���� WHERE counter = @Id",
                new { Id = id ?? 0 });

            return deptResult ?? "guest";
        }

        // �ھڳ����O����Τ��v��
        public async Task<IEnumerable<dynamic>> GetUserRights(string dept)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgvisConnection"));

            // ���� SQL �d��,�ھڳ����O����Τ��v��
            var userRights = await connection.QueryAsync<dynamic>(
                "SELECT a.dept, b.name, b.icon FROM adminUserRight as a " +
                "JOIN adminPage as b ON a.pageNo = b.no " +
                "WHERE a.dept = @Dept",
                new { Dept = dept });

            return userRights;
        }
    }
}