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
    public class EmployeeVisitsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeVisitsService> _logger;
        private readonly DateService _dateService;

        public EmployeeVisitsService(IConfiguration configuration, ILogger<EmployeeVisitsService> logger, DateService dateService)
        {
            _configuration = configuration;
            _logger = logger;
            _dateService = dateService;
        }
        public async Task<IEnumerable<dynamic>> GetEmployeeVisitsData(string str_date, string end_date)
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
                var employeeVisitsQuery = @"SELECT '員工看診' AS 類別, a.醫師代號, d.姓名 AS 醫師姓名, c.病歷號碼, a.患者姓名, b.就診日, b.結束日
                                        FROM 掛號檔 AS a
                                        JOIN 門診檔 AS b ON a.門診檔_counter = b.counter
                                        JOIN 病患檔 AS c ON c.counter = a.病患檔_counter
                                        JOIN 人事資料檔 AS d ON a.醫師代號 = d.人事代號
                                        WHERE a.掛號日期 BETWEEN @str_date AND @end_date
                                        AND a.病患檔_counter IN (SELECT counter FROM 病患檔 WHERE 身份證字號 IN (SELECT 身份證字號 FROM 人事資料檔 WHERE 職務代碼 NOT IN (1, 7) AND 離職日 = '' AND 身份證字號 <> '' AND 刪除否 = 0 AND LEN(人事代號) > 3))
                                        AND a.完診代碼 = '5' AND b.保險別代碼 = '1' AND b.就醫類別 IN ('01', '02', '04')
                                        ORDER BY a.醫師代號";

                var employeeVisitsRecords = await connection.QueryAsync(employeeVisitsQuery, new { str_date, end_date });

                var doctorVisitsQuery = @"SELECT '醫師看診' AS 類別, a.醫師代號, d.姓名 AS 醫師姓名, c.病歷號碼, a.患者姓名, b.就診日, b.結束日
                                      FROM 掛號檔 AS a
                                      JOIN 門診檔 AS b ON a.門診檔_counter = b.counter
                                      JOIN 病患檔 AS c ON c.counter = a.病患檔_counter
                                      JOIN 人事資料檔 AS d ON a.醫師代號 = d.人事代號
                                      WHERE a.掛號日期 BETWEEN @str_date AND @end_date
                                      AND a.病患檔_counter IN (SELECT counter FROM 病患檔 WHERE 身份證字號 IN (SELECT 身份證字號 FROM 人事資料檔 WHERE 職務代碼 IN (1, 7) AND 離職日 = '' AND 身份證字號 <> '' AND 刪除否 = 0 AND LEN(人事代號) = 3))
                                      AND a.完診代碼 = '5' AND a.保險別代碼 = '1' AND b.就醫類別 IN ('01', '02', '04')
                                      ORDER BY a.醫師代號";

                var doctorVisitsRecords = await connection.QueryAsync(doctorVisitsQuery, new { str_date, end_date });

                combinedRecords = employeeVisitsRecords.Concat(doctorVisitsRecords).ToList();
            }

            return combinedRecords;
        
        }
    }
}
