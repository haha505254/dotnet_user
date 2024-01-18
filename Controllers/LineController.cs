using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace dotnet_user.Controllers
{
    [Route("通知管理系統")]
    public class LineController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LineController> _logger;

        public LineController(IConfiguration configuration, ILogger<LineController> logger)
        {
            _configuration = configuration;
            _logger = logger;

        }

        public async Task<IActionResult> Index(string id)
        {

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "ID 不能為 null 或空白。");
            }

            var user = await GetUserAsync(id);
            if (user == null)
            {
                return View(); 
            }

            string counter = user.Counter?.ToString() ?? throw new InvalidOperationException("Counter 無效。");
            string email = user.Email帳號?.ToString() ?? throw new InvalidOperationException("Email帳號 無效。");

            var userSetting = await GetUserSettingAsync(counter);
            var items = await GetNotifyItemsAsync();

            ViewData["User"] = user;
            ViewData["Counter"] = counter;
            ViewData["Email"] = email;
            ViewData["UserSetting"] = userSetting;
            ViewData["Items"] = items;

            return View();


            async Task<dynamic?> GetUserAsync(string id)
            {
                var connectionString = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");
                using var connection = new SqlConnection(connectionString);
                var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM 人事資料檔 WHERE counter = @Id AND 刪除否 = '0' AND 離職日 = '' AND 到職日 != ''",
                    new { Id = id });
                return user;
            }

            async Task<IEnumerable<dynamic>> GetUserSettingAsync(string counter)
            {
                var connectionString = _configuration.GetConnectionString("CountryConnection") ?? throw new InvalidOperationException("未在配置中找到 'CountryConnection' 連接字符串。");
                using var connection = new SqlConnection(connectionString);
                return await connection.QueryAsync<dynamic>(
                    "SELECT * FROM Notify WHERE Member_Counter = @Counter",
                    new { Counter = counter });
            }

            async Task<IEnumerable<dynamic>> GetNotifyItemsAsync()
            {
                var connectionString = _configuration.GetConnectionString("CountryConnection") ?? throw new InvalidOperationException("未在配置中找到 'CountryConnection' 連接字符串。");
                using var connection = new SqlConnection(connectionString);
                return await connection.QueryAsync<dynamic>("SELECT * FROM NotifyItem");
            }

        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserSetting(string counter, string email, IDictionary<string, string> notifySettings)
        {
            _logger.LogInformation("Received notifySettings: {NotifySettings}", notifySettings);
            using var countryConnection = new SqlConnection(_configuration.GetConnectionString("CountryConnection"));
            using var tgsqlConnection = new SqlConnection(_configuration.GetConnectionString("TgsqlConnection"));

            var item = await countryConnection.QueryAsync<dynamic>("SELECT * FROM NotifyItem");
            var count = item.Count();

            for (int i = 1; i <= count; i++)
            {
                var lineValue = notifySettings.ContainsKey($"{i}") ? notifySettings[$"{i}"] : "0";
                var mailValue = notifySettings.ContainsKey($"mail{i}") ? notifySettings[$"mail{i}"] : "0";

                var updateQuery = "UPDATE Notify SET Line = @Line, Mail = @Mail WHERE Member_Counter = @MemberCounter AND NotifyItem_Counter = @NotifyItemCounter";
                // Log the query and parameters
                _logger.LogInformation("Executing SQL: {SQL}, Line: {Line}, Mail: {Mail}, MemberCounter: {MemberCounter}, NotifyItemCounter: {NotifyItemCounter}",
                                       updateQuery, lineValue, mailValue, counter, i);

                await countryConnection.ExecuteAsync(updateQuery,
                    new { Line = lineValue, Mail = mailValue, MemberCounter = counter, NotifyItemCounter = i });
            }

            var updateEmailQuery = "UPDATE 人事資料檔 SET Email帳號 = @Email WHERE Counter = @Counter";
            // Log the email update query
            _logger.LogInformation("Executing SQL: {SQL}, Email: {Email}, Counter: {Counter}", updateEmailQuery, email, counter);

            await tgsqlConnection.ExecuteAsync(updateEmailQuery,
                new { Email = email, Counter = counter });

            return Content("User settings updated successfully.");
        }


    }
}
