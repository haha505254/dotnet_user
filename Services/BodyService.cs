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

        // 獲取當前日期
        public string GetCurrentDate()
        {
            return _dateService.GetCurrentDate();
        }

        // 獲取指定天數前的日期
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // 獲取生理量測記錄
        public async Task<dynamic> GetBodyRecords(string id, string str_date, string end_date)
        {
            // 如果起始日期為空,則設置為30天前的日期,移除連接符號
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(30).Replace("-", "") : str_date.Replace("-", "");
            // 如果結束日期為空,則設置為當前日期,否則移除連接符號
            end_date = string.IsNullOrWhiteSpace(end_date) ? _dateService.GetCurrentDate() : end_date.Replace("-", "");

            // 計算起始日期和結束日期之間的天數差異
            var daysDifference = _dateService.GetDaysDifference(str_date, end_date) + 1;
            // 獲取結束日期之前指定天數的日期陣列
            var dateArray = _dateService.GetDateArray(end_date, daysDifference).Cast<IDictionary<string, object>>().ToList();

            // 將起始日期和結束日期轉換為民國年格式
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);
            end_date = (int.Parse(end_date.Substring(0, 4)) - 1911).ToString() + end_date.Substring(4, 4);

            // 根據 id 獲取人事資料的身份證字號
            var idResult = await _bodyRepository.GetPersonnelId(id);
            if (idResult == null || string.IsNullOrEmpty(idResult)) return new { error = "No data found." }; // 如果找不到資料,返回錯誤訊息

            // 根據身份證字號獲取病患檔的 counter
            var counterResult = await _bodyRepository.GetPatientCounter(idResult);
            if (counterResult == 0) return new { error = "No counter found." }; // 如果找不到 counter,返回錯誤訊息

            // 根據 counter 和日期範圍獲取血糖血壓記錄
            var records = await _bodyRepository.GetBodyRecords(counterResult, str_date, end_date);

            // 遍歷血糖血壓記錄
            foreach (var record in records)
            {
                var recordDate = _dateService.ParseRocDate(record.日期); // 將民國日期轉換為 DateTime 物件

                // 遍歷日期陣列
                foreach (var date in dateArray)
                {
                    // 將日期轉換為西元年格式字串
                    string convertedDate = _dateService.ConvertToGregorian(date["日期"]?.ToString() ?? string.Empty);

                    // 如果轉換後的日期與記錄日期相符
                    if (convertedDate.StartsWith(recordDate.ToString("yyyyMMdd")))
                    {
                        // 將記錄中的數值賦值給對應的日期物件
                        date["data1"] = record.Data1 != null ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                        date["data2"] = record.Data2 != null ? Convert.ToDouble(record.Data2).ToString("0.0") : "0";
                        date["data3"] = record.Data3 != null ? Convert.ToDouble(record.Data3).ToString("0.0") : "0";
                        date["data4"] = record.Data2 != null && Convert.ToDouble(record.Data2) < 1 ? Convert.ToDouble(record.Data1).ToString("0.0") : "0";
                    }
                }
            }

            // 構建結果物件
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