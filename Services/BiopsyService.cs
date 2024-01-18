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
using System.Dynamic;
namespace dotnet_user.Services
{
    public class BiopsyService
    {
        private readonly ILogger<BiopsyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly DateService _dateService;

        public BiopsyService(ILogger<BiopsyService> logger, IConfiguration configuration, DateService dateService)
        {
            _logger = logger;
            _configuration = configuration;
            _dateService = dateService;
        }

        public async Task<List<dynamic>> GetBiopsyDataAsync(string str_date, string end_date)
        {
            // 這裡加入日期處理邏輯

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

            var result = new List<dynamic>();
            foreach (var record in combinedRecords)
            {
                dynamic expando = new ExpandoObject();
                expando.來源 = record.來源 == 0 ? "門診" : "住診";
                expando.counter = record.counter.ToString();
                expando.處置檔 = record.處置檔.ToString();
                expando.開單日期 = record.開單日期.ToString();
                expando.病歷號碼 = record.病歷號碼.ToString();
                expando.姓名 = record.姓名.ToString();
                expando.科別 = record.科別.ToString();
                expando.醫師 = record.醫師.ToString();

                result.Add(expando);
            }

            return result;
        }
    }
}
