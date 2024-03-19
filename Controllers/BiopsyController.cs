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
            ViewData["StrDate"] = _dateService.GetStartDate(30);
            ViewData["EndDate"] = _dateService.GetCurrentDate();

            return View();
        }

        [HttpGet("biopsy")]
        public async Task<IActionResult> Biopsy(string str_date = "", string end_date = "")
        {
            var combinedRecords = await _biopsyService.GetBiopsyData(str_date, end_date);
            var result = _biopsyService.FormatBiopsyData(combinedRecords);
            return Content(result, "application/json");
        }

        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "")
        {
            var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);
            var stream = _biopsyService.ExportToExcel(dynamicData);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsyData.xlsx");
        }

        [HttpGet("downloadSql")]
        public async Task<IActionResult> DownloadSql(string str_date = "", string end_date = "")
        {
            var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);
            var result = await _biopsyService.GenerateUpdateSql(dynamicData.ToList());
            var stream = _biopsyService.ExportSqlToExcel(result);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsySql.xlsx");
        }
    }

}
