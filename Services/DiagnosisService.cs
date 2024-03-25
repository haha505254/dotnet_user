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

        public string GetFirstDayOfLastMonth()
        {
            return _dateService.GetFirstDayOfLastMonth();
        }

        public string GetLastDayOfLastMonth()
        {
            return _dateService.GetLastDayOfLastMonth();
        }

        public async Task<dynamic> GetDiagnosisRecords(string str_date, string end_date)
        {
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(0) : str_date.Replace("-", "");
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            var diagnosisCountByMonth = await _diagnosisRepository.GetDiagnosisCountByMonth(str_date, end_date);
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