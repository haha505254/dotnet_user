using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class NursePatientRepository : INursePatientRepository
    {
        private readonly IConfiguration _configuration;

        public NursePatientRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ����@�f����
        public async Task<IEnumerable<dynamic>> GetNursePatientRatios()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgvisConnection"));

            // ���� SQL �d��,����@�f����
            var nursePatientRatios = await connection.QueryAsync<dynamic>("SELECT * FROM nursePatientRatios");

            return nursePatientRatios;
        }

        // ��s�@�f����
        public async Task UpdateNursePatientRatio(string month, string enMonth, decimal ratio)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgvisConnection"));

            // ���� SQL ��s�y�y,��s�@�f����
            await connection.ExecuteAsync(@"
                UPDATE nursePatientRatios 
                SET month = @Month, en_month = @EnMonth, ratio = @Ratio",
                new { Month = month, EnMonth = enMonth, Ratio = ratio });
        }
    }
}