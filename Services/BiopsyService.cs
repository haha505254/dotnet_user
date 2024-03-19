using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper; // Ensure Dapper is included
using Newtonsoft.Json;
using OfficeOpenXml;

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
        public string FormatBiopsyData(IEnumerable<dynamic> combinedRecords)
        {
            var result = combinedRecords.Select(record => new
            {
                來源 = record.來源 == 0 ? "門診" : "住診",
                counter = record.counter.ToString(),
                處置檔 = record.處置檔.ToString(),
                開單日期 = record.開單日期.ToString(),
                病歷號碼 = record.病歷號碼.ToString(),
                姓名 = record.姓名.ToString(),
                科別 = record.科別.ToString(),
                醫師 = record.醫師.ToString()
            }).ToList();

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        public MemoryStream ExportToExcel(IEnumerable<dynamic> dynamicData)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var records = dynamicData.Select(item => new
            {
                item.來源,
                item.counter,
                item.處置檔,
                item.開單日期,
                item.病歷號碼,
                item.姓名,
                item.科別,
                item.醫師
            }).ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Biopsy Data");

            var properties = records.FirstOrDefault()?.GetType().GetProperties();
            var row = 1;

            if (properties != null)
            {
                for (var i = 0; i < properties.Length; i++)
                {
                    worksheet.Cells[row, i + 1].Value = properties[i].Name;
                }

                foreach (var record in records)
                {
                    row++;
                    for (var i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                    }
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }
        public async Task<List<dynamic>> GenerateUpdateSql(IEnumerable<dynamic> dynamicData)
        {
            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");

            var result = new List<dynamic>();

            foreach (var record in dynamicData)
            {
                using var conn = new SqlConnection(connectionStringTgsql);
                await conn.OpenAsync();

                if (record.來源 == 0)
                {
                    var queryA = "SELECT 備註, 結果檔_counter FROM 門診處置內容檔 WHERE counter = @counter";
                    using var cmdA = new SqlCommand(queryA, conn);
                    cmdA.Parameters.AddWithValue("@counter", record.處置檔);

                    var sql_a = new List<dynamic>();
                    using (var reader = await cmdA.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sql_a.Add(new { 備註 = reader["備註"].ToString(), 結果檔_counter = reader["結果檔_counter"].ToString() });
                        }
                    }

                    if (sql_a.Count == 1)
                    {
                        result.Add(new { sql = $"update 門診處置內容檔 set 結果檔_counter = {sql_a[0].備註} where counter = {record.處置檔}" });
                        result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {record.處置檔} where counter = {sql_a[0].備註}" });

                        var queryB = "SELECT counter, 結果檔_counter FROM 門診處置內容檔 WHERE 門診檔_counter = @門診檔_counter AND 結果檔_counter = @結果檔_counter";
                        using var cmdB = new SqlCommand(queryB, conn);
                        cmdB.Parameters.AddWithValue("@門診檔_counter", record.counter);
                        cmdB.Parameters.AddWithValue("@結果檔_counter", sql_a[0].備註);

                        var sql_b = new List<dynamic>();
                        using (var readerB = await cmdB.ExecuteReaderAsync())
                        {
                            while (await readerB.ReadAsync())
                            {
                                sql_b.Add(new { counter = readerB["counter"].ToString(), 結果檔_counter = readerB["結果檔_counter"].ToString() });
                            }
                        }

                        if (sql_b.Count == 1)
                        {
                            result.Add(new { sql = $"update 門診處置內容檔 set 結果檔_counter = {sql_a[0].結果檔_counter} where counter = {sql_b[0].counter}" });
                            result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {sql_b[0].counter} where counter = {sql_a[0].結果檔_counter}" });
                        }
                    }
                }
                else
                {
                    var queryA = "SELECT 備註, 結果檔_counter FROM 住診處置內容檔 WHERE counter = @counter";
                    using var cmdA = new SqlCommand(queryA, conn);
                    cmdA.Parameters.AddWithValue("@counter", record.處置檔);

                    var sql_a = new List<dynamic>();
                    using (var reader = await cmdA.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sql_a.Add(new { 備註 = reader["備註"].ToString(), 結果檔_counter = reader["結果檔_counter"].ToString() });
                        }
                    }

                    if (sql_a.Count == 1)
                    {
                        var queryB = "SELECT counter, 結果檔_counter FROM 住診處置內容檔 WHERE 住診檔_counter = @住診檔_counter AND 處置代號 = @處置代號";
                        using var cmdB = new SqlCommand(queryB, conn);
                        cmdB.Parameters.AddWithValue("@住診檔_counter", record.counter);
                        cmdB.Parameters.AddWithValue("@處置代號", sql_a[0].備註.Replace("-", ""));

                        var sql_b = new List<dynamic>();
                        using (var readerB = await cmdB.ExecuteReaderAsync())
                        {
                            while (await readerB.ReadAsync())
                            {
                                sql_b.Add(new { counter = readerB["counter"].ToString(), 結果檔_counter = readerB["結果檔_counter"].ToString() });
                            }
                        }

                        if (sql_b.Count == 1)
                        {
                            result.Add(new { sql = $"update 住診處置內容檔 set 結果檔_counter = {sql_b[0].結果檔_counter} where counter = {record.處置檔}" });
                            result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {record.處置檔} where counter = {sql_b[0].結果檔_counter}" });
                            result.Add(new { sql = $"update 住診處置內容檔 set 結果檔_counter = {sql_a[0].結果檔_counter} where counter = {sql_b[0].counter}" });
                            result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {sql_b[0].counter} where counter = {sql_a[0].結果檔_counter}" });
                        }
                    }
                }
            }

            return result;
        }

        public MemoryStream ExportSqlToExcel(List<dynamic> result)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Biopsy SQL");

            var properties = result.FirstOrDefault()?.GetType().GetProperties();
            var row = 0;

            if (properties != null)
            {
                foreach (var record in result)
                {
                    row++;
                    for (var i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                    }
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }
    }
}
