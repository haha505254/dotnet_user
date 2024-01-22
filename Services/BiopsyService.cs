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
    public class BiopsyService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BiopsyService> _logger;
        private readonly DateService _dateService;

        public BiopsyService(IConfiguration configuration, ILogger<BiopsyService> logger, DateService dateService)
        {
            _configuration = configuration;
            _logger = logger;
            _dateService = dateService;
        }

        public async Task<IEnumerable<dynamic>> GetBiopsyData(string str_date, string end_date)
        {
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
            List<dynamic> combinedRecords;

            using (var connection = new SqlConnection(connectionStringTgsql))
            {
                await connection.OpenAsync();
                // 門診數據查詢
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

                var outpatientRecords = await connection.QueryAsync(outpatientQuery, new { str_date, end_date });

                // 住診數據查詢
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
                var inpatientRecords = await connection.QueryAsync(inpatientQuery, new { str_date, end_date });

                combinedRecords = outpatientRecords.Concat(inpatientRecords).ToList();
            }




            return combinedRecords;
        }
    }
}
