using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;
using Microsoft.Extensions.Logging;

namespace dotnet_user.Services
{
    public class OutpatientVisitsService : IOutpatientVisitsService
    {
        private readonly IOutpatientVisitsRepository _outpatientVisitsRepository;
        private readonly IDateService _dateService;
        private readonly ILogger<OutpatientVisitsService> _logger;

        public OutpatientVisitsService(IOutpatientVisitsRepository outpatientVisitsRepository, IDateService dateService, ILogger<OutpatientVisitsService> logger)
        {
            _outpatientVisitsRepository = outpatientVisitsRepository;
            _dateService = dateService;
            _logger = logger;
        }

        // 獲取門診看診人數資料
        public async Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, string count)
        {
            int countValue = !string.IsNullOrEmpty(count) && int.TryParse(count, out int temp) ? temp : 60;

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

            // 呼叫 _outpatientVisitsRepository.GetOutpatientVisitsData 方法獲取門診看診人數資料
            var result = await _outpatientVisitsRepository.GetOutpatientVisitsData(str_date, end_date, countValue);
            return result;
        }
    }
}