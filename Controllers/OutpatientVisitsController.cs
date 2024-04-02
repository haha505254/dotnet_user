using dotnet_user.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace dotnet_user.Controllers
{
    [Route("門診看診人數")]
    public class OutpatientVisitsController : Controller
    {
        private readonly IDateService _dateService;
        private readonly IOutpatientVisitsService _outpatientVisitsService;
        private readonly ILogger<OutpatientVisitsController> _logger;

        public OutpatientVisitsController(IDateService dateService, ILogger<OutpatientVisitsController> logger, IOutpatientVisitsService outpatientVisitsService)
        {
            _dateService = dateService;
            _outpatientVisitsService = outpatientVisitsService;
            _logger = logger;
        }

        // 首頁 Action,返回門診看診人數的檢視
        public IActionResult Index()
        {
            var strDate = _dateService.GetStartDate(30); // 取得起始日期(30天前)
            var endDate = _dateService.GetCurrentDate(); // 取得當前日期
            var count = 60; // 設置預設的看診人數門檻

            ViewData["StrDate"] = strDate;
            ViewData["EndDate"] = endDate;
            ViewData["Count"] = count;

            return View();
        }

        // 獲取門診看診人數資料的 Action
        [HttpGet("outpatientVisits")]
        public async Task<IActionResult> OutpatientVisits(string str_date = "", string end_date = "", string count = "")
        {
            try
            {
                // 呼叫 _outpatientVisitsService.GetOutpatientVisitsData 方法獲取門診看診人數資料
                var records = await _outpatientVisitsService.GetOutpatientVisitsData(str_date, end_date, count);

                var result = records.Select(record => new
                {
                    掛號日期 = record.掛號日期.ToString(),
                    看診班別 = record.看診班別.ToString(),
                    醫師姓名 = record.醫師姓名.ToString(),
                    看診人數 = record.看診人數.ToString(),
                }).ToList();

                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving outpatient visits data");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }

        // 下載門診看診人數詳細資料的 Action
        [HttpGet("downloadDetail")]
        public async Task<IActionResult> DownloadDetail(string str_date = "", string end_date = "", string count = "")
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // 呼叫 _outpatientVisitsService.GetOutpatientVisitsData 方法獲取門診看診人數資料
                var dynamicData = await _outpatientVisitsService.GetOutpatientVisitsData(str_date, end_date, count);

                var records = dynamicData.Select(item => new
                {
                    item.掛號日期,
                    item.看診班別,
                    item.醫師姓名,
                    item.看診人數,
                }).OrderBy(item => item.掛號日期).ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("OutpatientVisits Data");

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

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OutpatientVisitsData.xlsx");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading outpatient visits detail");
                return Json(new { error = "An error occurred while processing your request." });
            }
        }
    }
}