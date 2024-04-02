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

        // 根據id獲取使用者資料
        public async Task<dynamic> GetUserAsync(string id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            // 執行 SQL 查詢,根據 id 獲取使用者資料
            var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM 人事資料檔 WHERE counter = @Id AND 刪除否 = '0' AND 離職日 = '' AND 到職日 != ''",
                new { Id = id });
            if (user == null)
            {
                // 拋出異常
                throw new Exception("User not found.");
            }
            return user;
        }

        // 根據counter獲取使用者設定
        public async Task<IEnumerable<dynamic>> GetUserSettingAsync(string counter)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));

            // 執行 SQL 查詢,根據 counter 獲取使用者設定
            var userSetting = await connection.QueryAsync<dynamic>(
                "SELECT * FROM Notify WHERE Member_Counter = @Counter",
                new { Counter = counter });

            return userSetting;
        }

        // 獲取通知項目
        public async Task<IEnumerable<dynamic>> GetNotifyItemsAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));

            // 執行 SQL 查詢,獲取通知項目
            var items = await connection.QueryAsync<dynamic>("SELECT * FROM NotifyItem");

            return items;
        }

        // 更新使用者設定
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

            var updateEmailQuery = "UPDATE 人事資料檔 SET Email帳號 = @Email WHERE Counter = @Counter";
            await tgsqlConnection.ExecuteAsync(updateEmailQuery,
                new { Email = email, Counter = counter });
        }
    }
}