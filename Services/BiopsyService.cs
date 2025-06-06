﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace dotnet_user.Services
{
    public class BiopsyService : IBiopsyService
    {
        private readonly IBiopsyRepository _biopsyRepository;
        private readonly IDateService _dateService;
        private readonly ILogger<BiopsyService> _logger;
        private readonly IExcelService _excelService;

        public BiopsyService(IBiopsyRepository biopsyRepository, IDateService dateService, ILogger<BiopsyService> logger, IExcelService excelService)
        {
            _biopsyRepository = biopsyRepository;
            _dateService = dateService;
            _logger = logger;
            _excelService = excelService;
        }

        // 取得當前日期
        public string GetCurrentDate()
        {
            return _dateService.GetCurrentDate();
        }

        // 取得指定天數前的日期
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // 取得切片報告資料
        public async Task<IEnumerable<dynamic>> GetBiopsyData(string strDate, string endDate)
        {
            strDate = strDate.Replace("-", "");
            endDate = endDate.Replace("-", "");

            if (!string.IsNullOrEmpty(strDate) && DateTime.TryParseExact(strDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedStartDate))
            {
                strDate = parsedStartDate.AddDays(-1).ToString("yyyyMMdd");
            }
            else
            {
                strDate = _dateService.GetStartDate(30);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParseExact(endDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedEndDate))
            {
                endDate = parsedEndDate.ToString("yyyyMMdd");
            }
            else
            {
                endDate = _dateService.GetCurrentDate();
            }

            strDate = (int.Parse(strDate.Substring(0, 4)) - 1911).ToString() + strDate.Substring(4, 4);
            endDate = (int.Parse(endDate.Substring(0, 4)) - 1911).ToString() + endDate.Substring(4, 4);

            var outpatientRecords = await _biopsyRepository.GetOutpatientRecords(strDate, endDate);
            var inpatientRecords = await _biopsyRepository.GetInpatientRecords(strDate, endDate);
            var combinedRecords = outpatientRecords.Concat(inpatientRecords).ToList();

            return combinedRecords;
        }

        // 格式化切片報告資料
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

        // 將資料匯出為 Excel
        public MemoryStream ExportToExcel(IEnumerable<dynamic> dynamicData)
        {
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

            return _excelService.ExportToExcel(records, "Biopsy Data");
        }

        // 產生更新資料的 SQL 語句
        public async Task<List<dynamic>> GenerateUpdateSql(IEnumerable<dynamic> dynamicData)
        {
            var result = new List<dynamic>();

            foreach (var record in dynamicData)
            {
                if (record.來源 == 0)
                {
                    var sql_a = await _biopsyRepository.GetBiopsyRemarks(record.處置檔);

                    if (sql_a.Count == 1)
                    {
                        result.Add(new { sql = $"update 門診處置內容檔 set 結果檔_counter = {sql_a[0].備註} where counter = {record.處置檔}" });
                        result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {record.處置檔} where counter = {sql_a[0].備註}" });

                        var sql_b = await _biopsyRepository.GetRelatedRecords(record.counter, sql_a[0].備註);

                        if (sql_b.Count == 1)
                        {
                            result.Add(new { sql = $"update 門診處置內容檔 set 結果檔_counter = {sql_a[0].結果檔_counter} where counter = {sql_b[0].counter}" });
                            result.Add(new { sql = $"update 報告台結果檔 set 來源檔_counter = {sql_b[0].counter} where counter = {sql_a[0].結果檔_counter}" });
                        }
                    }
                }
                else
                {
                    var sql_a = await _biopsyRepository.GetBiopsyRemarks(record.處置檔);

                    if (sql_a.Count == 1)
                    {
                        var sql_b = await _biopsyRepository.GetRelatedRecords(record.counter, sql_a[0].備註.Replace("-", ""));

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

        // 將 SQL 語句匯出為 Excel
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