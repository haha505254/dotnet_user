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
            var result = await _biopsyService.GetBiopsyData(str_date, end_date);
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
    }

}
