using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Controllers
{
    [Route("切片報告管理")]
    public class BiopsyController : Controller
    {
        private readonly IBiopsyService _biopsyService;
        private readonly ILogger<BiopsyController> _logger;

        public BiopsyController(IBiopsyService biopsyService, ILogger<BiopsyController> logger)
        {
            _biopsyService = biopsyService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var strDate = _biopsyService.GetStartDate(30); // 取得開始日期(30天前)
            var endDate = _biopsyService.GetCurrentDate(); //結束日期(當前日期)

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;

            return View();
        }

        [HttpGet("biopsy")]
        public async Task<IActionResult> Biopsy(string str_date = "", string end_date = "")
        {
            try
            {
                // 取得切片報告資料
                var combinedRecords = await _biopsyService.GetBiopsyData(str_date, end_date);
                var result = _biopsyService.FormatBiopsyData(combinedRecords);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving biopsy data");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }

        // 下載切片報告明細
        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "")
        {
            try
            {
                // 取得切片報告資料
                var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);
                // 將資料匯出為 Excel 檔案
                var stream = _biopsyService.ExportToExcel(dynamicData);

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsyData.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading biopsy detail");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }

        // 下載切片報告相關的 SQL 語句
        [HttpGet("downloadSql")]
        public async Task<IActionResult> DownloadSql(string str_date = "", string end_date = "")
        {
            try
            {
                // 取得切片報告資料
                var dynamicData = await _biopsyService.GetBiopsyData(str_date, end_date);
                // 產生更新資料的 SQL 語句
                var result = await _biopsyService.GenerateUpdateSql(dynamicData.ToList());
                // 將 SQL 語句匯出為 Excel 檔案 
                var stream = _biopsyService.ExportSqlToExcel(result);

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BiopsySql.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading biopsy SQL");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }
    }
}