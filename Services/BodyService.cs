using System;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class BodyService : IBodyService
    {
        private readonly IBodyRepository _bodyRepository;
        private readonly IDateService _dateService;

        public BodyService(IBodyRepository bodyRepository, IDateService dateService)
        {
            _bodyRepository = bodyRepository;
            _dateService = dateService;
        }

        // �����e���
        public string GetCurrentDate()
        {
            return _dateService.GetCurrentDate();
        }

        // ������w�Ѽƫe�����
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // ����Ͳz�q���O��
        public async Task<dynamic> GetBodyRecords(string id, string str_date, string end_date)
        {
            // �p�G�_�l�������,�h�]�m��30�ѫe�����,�����s���Ÿ�
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(30).Replace("-", "") : str_date.Replace("-", "");
            // �p�G�����������,�h�]�m����e���,�_�h�����s���Ÿ�
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            // �p��_�l����M��������������ѼƮt��
            var daysDifference = _dateService.GetDaysDifference(str_date, end_date) + 1;
            // �������������e���w�Ѽƪ�����}�C
            var dateArray = _dateService.GetDateArray(end_date, daysDifference).Cast<IDictionary<string, object>>().ToList();

            // �N�_�l����M��������ഫ������~�榡
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            // �ھ� id ����H�Ƹ�ƪ������Ҧr��
            var idResult = await _bodyRepository.GetPersonnelId(id);
            if (idResult == null || string.IsNullOrEmpty(idResult)) return new { error = "No data found." }; // �p�G�䤣����,��^���~�T��

            // �ھڨ����Ҧr������f�w�ɪ� counter
            var counterResult = await _bodyRepository.GetPatientCounter(idResult);
            if (counterResult == 0) return new { error = "No counter found." }; // �p�G�䤣�� counter,��^���~�T��

            // �ھ� counter �M����d�������}�����O��
            var records = await _bodyRepository.GetBodyRecords(counterResult, str_date, end_date);

            // �M����}�����O��
            foreach (var record in records)
            {
                var recordDate = _dateService.ParseRocDate(record.���); // �N�������ഫ�� DateTime ����

                // �M������}�C
                foreach (var date in dateArray)
                {
                    // �N����ഫ���褸�~�榡�r��
                    string convertedDate = _dateService.ConvertToGregorian(date["���"]?.ToString() ?? string.Empty);

                    // �p�G�ഫ�᪺����P�O������۲�
                    if (convertedDate.StartsWith(recordDate.ToString("yyyyMMdd")))
                    {
                        // �N�O�������ƭȽ�ȵ��������������
                        date["data1"] = record.Data1 != null ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                        date["data2"] = record.Data2 != null ? Convert.ToDouble(record.Data2).ToString("0.0") : "0";
                        date["data3"] = record.Data3 != null ? Convert.ToDouble(record.Data3).ToString("0.0") : "0";
                        date["data4"] = record.Data2 != null && Convert.ToDouble(record.Data2) < 1 ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                    }
                }
            }

            // �c�ص��G����
            var result = new
            {
                date = dateArray.Select(d =>
                {
                    var dateString = d["���"] as string;
                    return dateString != null ? dateString.Substring(dateString.Length - 4) : string.Empty;
                }).ToList(),
                data1 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data1"]), 1).ToString("0.0")).ToList(),
                data2 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data2"]), 1).ToString("0.0")).ToList(),
                data3 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data3"]), 1).ToString("0.0")).ToList(),
                data4 = dateArray.Select(d => Math.Round(Convert.ToDouble(d["data4"]), 1).ToString("0.0")).ToList(),
                str_date = str_date,
                end_date = end_date
            };

            return result;
        }
    }
}