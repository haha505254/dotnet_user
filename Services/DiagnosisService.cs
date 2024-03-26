using System;
using System.Threading.Tasks;
using dotnet_user.Repositories;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class DiagnosisService : IDiagnosisService
    {
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IDateService _dateService;

        public DiagnosisService(IDiagnosisRepository diagnosisRepository, IDateService dateService)
        {
            _diagnosisRepository = diagnosisRepository;
            _dateService = dateService;
        }

        // 取得上個月的第一天
        public string GetFirstDayOfLastMonth()
        {
            return _dateService.GetFirstDayOfLastMonth();
        }

        // 取得上個月的最後一天
        public string GetLastDayOfLastMonth()
        {
            return _dateService.GetLastDayOfLastMonth();
        }

        // 取得診斷書記錄
        public async Task<dynamic> GetDiagnosisRecords(string str_date, string end_date)
        {
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(0) : str_date.Replace("-", "");
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            // 將西元年轉換為民國年
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            // 取得每個月的診斷書數量
            var diagnosisCountByMonth = await _diagnosisRepository.GetDiagnosisCountByMonth(str_date, end_date);

            // 取得每個醫師在每個月的診斷書數量
            var diagnosisCountByDoctorAndMonth = await _diagnosisRepository.GetDiagnosisCountByDoctorAndMonth(str_date, end_date);

            var result = new
            {
                FirstRecord = diagnosisCountByMonth,
                SecondRecord = diagnosisCountByDoctorAndMonth
            };

            return result;
        }
    }
}