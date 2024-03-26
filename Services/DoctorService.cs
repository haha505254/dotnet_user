using System;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDateService _dateService;

        public DoctorService(IDoctorRepository doctorRepository, IDateService dateService)
        {
            _doctorRepository = doctorRepository;
            _dateService = dateService;
        }

        // 獲取指定天數前的起始日期
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // 獲取醫師時段記錄
        public async Task<dynamic> GetDoctorRecords(string strDate)
        {
            // 如果 strDate 為空或空白,則使用當前日期作為起始日期
            strDate = !string.IsNullOrEmpty(strDate) ? strDate.Replace("-", "") : _dateService.GetStartDate(0);
            // 獲取星期幾
            var week = _dateService.GetWeekDay(strDate);
            // 如果星期為 0,則轉換為 7
            week = week == "0" ? "7" : week;
            // 將日期轉換為民國年格式
            strDate = (int.Parse(strDate.Substring(0, 4)) - 1911).ToString() + strDate.Substring(4, 4);

            // 獲取指定日期的醫師休假記錄
            var off = await _doctorRepository.GetDoctorOff(strDate);
            // 獲取年月份
            var yearMonth = strDate.Substring(0, 5);
            // 獲取醫師時段記錄
            var records = await _doctorRepository.GetDoctorRecords(yearMonth, week, off);

            // 將醫師時段記錄轉換為指定格式
            var result = records.Select(r => new
            {
                科別 = (string)r.科別,
                診別 = (string)r.診別,
                班別 = (string)r.班別,
                代號 = (string)r.醫師代號,
                姓名 = (string)r.姓名,
                備註 = (string)r.備註
            }).ToList();

            // 如果結果為空,則添加一條空記錄
            if (!result.Any())
            {
                result.Add(new { 科別 = "", 診別 = "", 班別 = "", 代號 = "", 姓名 = "", 備註 = "" });
            }

            // 獲取醫師代班記錄
            var recordSecond = await _doctorRepository.GetDoctorSecondRecords(strDate);

            // 將醫師代班記錄轉換為指定格式
            var resultSecond = recordSecond.Select(r => new
            {
                科別 = (string)r.科別,
                診別 = (string)r.診別,
                班別 = (string)r.班別,
                代號 = (string)r.醫師代號,
                姓名 = (string)r.姓名
            }).ToList();

            // 如果結果為空,則添加一條空記錄
            if (!resultSecond.Any())
            {
                resultSecond.Add(new { 科別 = "", 診別 = "", 班別 = "", 代號 = "", 姓名 = "" });
            }

            // 返回醫師時段記錄和醫師代班記錄
            return new { Result = result, ResultSecond = resultSecond };
        }
    }
}