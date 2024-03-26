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

        // ���o�W�Ӥ몺�Ĥ@��
        public string GetFirstDayOfLastMonth()
        {
            return _dateService.GetFirstDayOfLastMonth();
        }

        // ���o�W�Ӥ몺�̫�@��
        public string GetLastDayOfLastMonth()
        {
            return _dateService.GetLastDayOfLastMonth();
        }

        // ���o�E�_�ѰO��
        public async Task<dynamic> GetDiagnosisRecords(string str_date, string end_date)
        {
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(0) : str_date.Replace("-", "");
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            // �N�褸�~�ഫ������~
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            // ���o�C�Ӥ몺�E�_�Ѽƶq
            var diagnosisCountByMonth = await _diagnosisRepository.GetDiagnosisCountByMonth(str_date, end_date);

            // ���o�C����v�b�C�Ӥ몺�E�_�Ѽƶq
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