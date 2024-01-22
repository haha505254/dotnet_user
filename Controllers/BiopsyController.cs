using System.IO;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services; // 確保引用了 DateService 所在的命名空間
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace dotnet_user.Controllers

{
    [Route("切片報告管理")]
    public class BiopsyController : Controller
    {
        private readonly DateService _dateService;
        private readonly BiopsyService _biopsyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BiopsyController> _logger;
        public BiopsyController(DateService dateService, IConfiguration configuration, ILogger<BiopsyController> logger, BiopsyService biopsyService)
        {
            _configuration = configuration;
            _dateService = dateService;
            _biopsyService = biopsyService;
            _logger = logger;

        }
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(30);
            var endDate = _dateService.GetCurrentDate();

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("biopsy")]
        public async Task<IActionResult> Biopsy(string str_date = "", string end_date = "")
        {
            var combinedRecords = await _biopsyService.GetBiopsyData(str_date, end_date);

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

            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            return Content(json, "application/json");
        }

        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);

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

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Biopsy Data");

                var properties = records.FirstOrDefault()?.GetType().GetProperties();
                int row = 1;

                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = properties[i].Name;
                    }

                    foreach (var record in records)
                    {
                        row++;
                        for (int i = 0; i < properties.Length; i++)
                        {
                            worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                        }
                    }
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsyData.xlsx");
            }
        }

        [HttpGet("downloadSql")]
        public async Task<IActionResult> DownloadSql(string str_date = "", string end_date = "")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);

            string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");

            List<dynamic> result = new List<dynamic>();


            foreach (var record in dynamicData)
            {
                using (SqlConnection conn = new SqlConnection(connectionStringTgsql))
                {
                    conn.Open();

                    if (record.來源 == 0)
                    {
                        string queryA = "SELECT 備註, 結果檔_counter FROM 門診處置內容檔 WHERE counter = @counter";
                        SqlCommand cmdA = new SqlCommand(queryA, conn);
                        cmdA.Parameters.AddWithValue("@counter", record.處置檔);

                        var sql_a = new List<dynamic>();
                        using (SqlDataReader reader = cmdA.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sql_a.Add(new { 備註 = reader["備註"].ToString(), 結果檔_counter = reader["結果檔_counter"].ToString() });
                            }
                        }

                        if (sql_a.Count == 1)
                        {
                            result.Add(new { sql = $"update 門診處置內容檔 set 結果檔_counter = {sql_a[0].備註} where counter = {record.處置檔}" });
                            result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {record.處置檔} where counter = {sql_a[0].備註}" });

                            string queryB = "SELECT counter, 結果檔_counter FROM 門診處置內容檔 WHERE 門診檔_counter = @門診檔_counter AND 結果檔_counter = @結果檔_counter";
                            SqlCommand cmdB = new SqlCommand(queryB, conn);
                            cmdB.Parameters.AddWithValue("@門診檔_counter", record.counter);
                            cmdB.Parameters.AddWithValue("@結果檔_counter", sql_a[0].備註);

                            var sql_b = new List<dynamic>();
                            using (SqlDataReader readerB = cmdB.ExecuteReader())
                            {
                                while (readerB.Read())
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
                        string queryA = "SELECT 備註, 結果檔_counter FROM 住診處置內容檔 WHERE counter = @counter";
                        SqlCommand cmdA = new SqlCommand(queryA, conn);
                        cmdA.Parameters.AddWithValue("@counter", record.處置檔);

                        var sql_a = new List<dynamic>();
                        using (SqlDataReader reader = cmdA.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sql_a.Add(new { 備註 = reader["備註"].ToString(), 結果檔_counter = reader["結果檔_counter"].ToString() });
                            }
                        }

                        if (sql_a.Count == 1)
                        {
                            string queryB = "SELECT counter, 結果檔_counter FROM 住診處置內容檔 WHERE 住診檔_counter = @住診檔_counter AND 處置代號 = @處置代號";
                            SqlCommand cmdB = new SqlCommand(queryB, conn);
                            cmdB.Parameters.AddWithValue("@住診檔_counter", record.counter);
                            cmdB.Parameters.AddWithValue("@處置代號", sql_a[0].備註.Replace("-", ""));

                            var sql_b = new List<dynamic>();
                            using (SqlDataReader readerB = cmdB.ExecuteReader())
                            {
                                while (readerB.Read())
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
            }
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Biopsy Data");

                var properties = result.FirstOrDefault()?.GetType().GetProperties();
                int row = 0;

                if (properties != null)
                {
                    foreach (var record in result)
                    {
                        row++;
                        for (int i = 0; i < properties.Length; i++)
                        {
                            worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                        }
                    }
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsySql.xlsx");
            }
        }
    }

}
