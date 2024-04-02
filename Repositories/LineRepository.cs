using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dotnet_user.Repositories.Interface;

namespace dotnet_user.Repositories
{
    public class LineRepository : ILineRepository
    {
        private readonly IConfiguration _configuration;
        public LineRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // �ھ�id����ϥΪ̸��
        public async Task<dynamic> GetUserAsync(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // ���� SQL �d��,�ھ� id ����ϥΪ̸��
            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM �H�Ƹ���� WHERE counter = @Id AND �R���_ = '0' AND ��¾�� = '' AND ��¾�� != ''",
                new { Id = id });
            if (user == null)
            {
                // �ߥX���`
                throw new Exception("User not found.");
            }
            return user;
        }

        // �ھ�counter����ϥΪ̳]�w
        public async Task<IEnumerable<dynamic>> GetUserSettingAsync(string counter)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));

            // ���� SQL �d��,�ھ� counter ����ϥΪ̳]�w
            var userSetting = await connection.QueryAsync<dynamic>(
                "SELECT * FROM Notify WHERE Member_Counter = @Counter",
                new { Counter = counter });

            return userSetting;
        }

        // ����q������
        public async Task<IEnumerable<dynamic>> GetNotifyItemsAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));

            // ���� SQL �d��,����q������
            var items = await connection.QueryAsync<dynamic>("SELECT * FROM NotifyItem");

            return items;
        }

        // ��s�ϥΪ̳]�w
        public async Task UpdateUserSettingAsync(string counter, string email, IDictionary<string, string> notifySettings)
        {
            using var countryConnection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            using var tgsqlConnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var item = await countryConnection.QueryAsync<dynamic>("SELECT * FROM NotifyItem");
            var count = item.Count();

            for (int i = 1; i <= count; i++)
            {
                var lineValue = notifySettings.ContainsKey($"{i}") ? notifySettings[$"{i}"] : "0";
                var mailValue = notifySettings.ContainsKey($"mail{i}") ? notifySettings[$"mail{i}"] : "0";

                var updateQuery = "UPDATE Notify SET Line = @Line, Mail = @Mail WHERE Member_Counter = @MemberCounter AND NotifyItem_Counter = @NotifyItemCounter";
                await countryConnection.ExecuteAsync(updateQuery,
                    new { Line = lineValue, Mail = mailValue, MemberCounter = counter, NotifyItemCounter = i });
            }

            var updateEmailQuery = "UPDATE �H�Ƹ���� SET Email�b�� = @Email WHERE Counter = @Counter";
            await tgsqlConnection.ExecuteAsync(updateEmailQuery,
                new { Email = email, Counter = counter });
        }
    }
}