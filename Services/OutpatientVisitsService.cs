using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper; // Ensure Dapper is included

namespace dotnet_user.Services
{
    public class OutpatientVisitsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutpatientVisitsService> _logger;
        private readonly DateService _dateService;

        public OutpatientVisitsService(IConfiguration configuration, ILogger<OutpatientVisitsService> logger, DateService dateService)
        {
            _configuration = configuration;
            _logger = logger;
            _dateService = dateService;
        }

        public async Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, string count)
        {
            int countValue;
            // 檢查 'count' 參數是否存在且不為空，如果是，則解析它，否則設為預設值 60
            if (!string.IsNullOrEmpty(count) && int.TryParse(count, out countValue))
            {
                // count 參數有效，已解析為數字並賦值給 countValue
            }
            else
            {
                // count 參數不存在或無效，使用預設值 60
                countValue = 60;
            }
            // Your existing logic to format and process dates goes here
            str_date = str_date.Replace("-", "");
            end_date = end_date.Replace("-", "");
            // 處理 str_date
            if (!string.IsNullOrEmpty(str_date) && DateTime.TryParseExact(str_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate))
            {
                str_date = parsedStartDate.AddDays(-1).ToString("yyyyMMdd");
            }
            else
            {
                str_date = _dateService.GetStartDate(30);
            }

            // 處理 end_date
            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParseExact(end_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                end_date = parsedEndDate.ToString("yyyyMMdd");
            }
            else
            {
                end_date = _dateService.GetCurrentDate();
            }

            // 將西元年份轉換為民國年份
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            _logger.LogInformation("{str_date} {end_date}", str_date, end_date);

            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");



            using (var connection = new SqlConnection(connectionStringTgsql))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        a.掛號日期, 
                        b.代碼內容 AS 看診班別, 
                        c.姓名 AS 醫師姓名, 
                        COUNT(a.counter) AS 看診人數
                    FROM 掛號檔 AS a
                    JOIN 代碼檔 AS b ON a.班別代碼 = b.代碼
                    JOIN 人事資料檔 AS c ON a.醫師代號 = c.人事代號
                    WHERE 
                        a.掛號日期 BETWEEN @str_date AND @end_date
                        AND a.完診代碼 = '5'
                        AND b.代碼名稱 = '班別代碼'
                    GROUP BY a.掛號日期, b.代碼內容, c.姓名
                    HAVING COUNT(a.counter) >= @countValue";

                var parameters = new
                {
                    str_date,
                    end_date,
                    countValue
                };

                var result = await connection.QueryAsync<dynamic>(query, parameters);
                return result;
            }
        }
    }
}
