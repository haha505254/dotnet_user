using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;
using OfficeOpenXml;

namespace dotnet_user.Services
{
    public class EmployeeVisitsService : IEmployeeVisitsService
    {
        private readonly IEmployeeVisitsRepository _employeeVisitsRepository;
        private readonly IDateService _dateService;
        private readonly ILogger<EmployeeVisitsService> _logger;

        public EmployeeVisitsService(IEmployeeVisitsRepository employeeVisitsRepository, IDateService dateService, ILogger<EmployeeVisitsService> logger)
        {
            _employeeVisitsRepository = employeeVisitsRepository;
            _dateService = dateService;
            _logger = logger;
        }


        // 獲取院內看診明細資料
        public async Task<List<dynamic>> GetEmployeeVisitsData(string str_date, string end_date)
        {
            try
            {
                str_date = FormatDate(str_date, _dateService.GetStartDate(30));
                end_date = FormatDate(end_date, _dateService.GetCurrentDate());

                var employeeRecords = await _employeeVisitsRepository.GetEmployeeVisitsRecords(str_date, end_date);
                var doctorRecords = await _employeeVisitsRepository.GetDoctorVisitsRecords(str_date, end_date);

                var combinedRecords = employeeRecords.Concat(doctorRecords);

                var result = combinedRecords.Select(record => new
                {
                    類別 = record.類別 as dynamic,
                    醫師代號 = record.醫師代號 as dynamic,
                    醫師姓名 = record.醫師姓名 as dynamic,
                    病歷號碼 = record.病歷號碼 as dynamic,
                    患者姓名 = record.患者姓名 as dynamic,
                    就診日 = record.就診日 as dynamic,
                    結束日 = record.結束日 as dynamic
                }).ToList<dynamic>();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving employee visits data");
                throw;
            }
        }
        public async Task<MemoryStream> DownloadEmployeeVisitsData(string str_date, string end_date)
        {
            try
            {
                str_date = FormatDate(str_date, _dateService.GetStartDate(30), true);
                end_date = FormatDate(end_date, _dateService.GetCurrentDate());

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var dynamicData = await _employeeVisitsRepository.GetEmployeeVisitsRecords(str_date, end_date);
                var records = dynamicData.Select(item => new
                {
                    item.類別,
                    item.醫師代號,
                    item.醫師姓名,
                    item.病歷號碼,
                    item.患者姓名,
                    item.就診日,
                    item.結束日
                }).ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("EmployeeVisits Data");

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

                    return stream;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading employee visits data");
                throw;
            }
        }

        private string FormatDate(string dateString, string defaultDate, bool subtractOneDay = false)
        {
            dateString = dateString.Replace("-", "");

            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                if (subtractOneDay)
                {
                    parsedDate = parsedDate.AddDays(-1);
                }
                dateString = parsedDate.ToString("yyyyMMdd");
            }
            else
            {
                dateString = defaultDate;
            }

            return (int.Parse(dateString.Substring(0, 4)) - 1911).ToString() + dateString.Substring(4, 4);
        }
    }
}