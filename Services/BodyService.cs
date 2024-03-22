using System;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories;

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

        public string GetCurrentDate()
        {
            return _dateService.GetCurrentDate();
        }

        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        public async Task<dynamic> GetBodyRecords(string id, string str_date, string end_date)
        {
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(30).Replace("-", "") : str_date.Replace("-", "");
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            var daysDifference = _dateService.GetDaysDifference(str_date, end_date) + 1;
            var dateArray = _dateService.GetDateArray(end_date, daysDifference).Cast<IDictionary<string, object>>().ToList();

            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            var idResult = await _bodyRepository.GetPersonnelId(id);
            if (idResult == null) return new { error = "No data found." };

            var counterResult = await _bodyRepository.GetPatientCounter(idResult);
            if (counterResult == 0) return new { error = "No counter found." };

            var records = await _bodyRepository.GetBodyRecords(counterResult, str_date, end_date);

            foreach (var record in records)
            {
                var recordDate = _dateService.ParseRocDate(record.日期);

                foreach (var date in dateArray)
                {
                    string convertedDate = _dateService.ConvertToGregorian(date["日期"]?.ToString() ?? string.Empty);

                    if (convertedDate.StartsWith(recordDate.ToString("yyyyMMdd")))
                    {
                        date["data1"] = record.Data1 != null ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                        date["data2"] = record.Data2 != null ? Convert.ToDouble(record.Data2).ToString("0.0") : "0";
                        date["data3"] = record.Data3 != null ? Convert.ToDouble(record.Data3).ToString("0.0") : "0";
                        date["data4"] = record.Data2 != null && Convert.ToDouble(record.Data2) < 1 ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                    }
                }
            }

            var result = new
            {
                date = dateArray.Select(d =>
                {
                    var dateString = d["日期"] as string;
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