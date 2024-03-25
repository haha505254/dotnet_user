using System;
using System.Collections.Generic;

namespace dotnet_user.Services.Interface
{
    public interface IDateService
    {
        string GetCurrentDate(); // 獲取當前日期
        string GetStartDate(int daysAgo); // 獲取指定天數前的日期
        int GetDaysDifference(string strDate, string endDate); // 計算兩個日期之間的天數差異
        IEnumerable<dynamic> GetDateArray(string endDate, int days); // 獲取指定結束日期之前指定天數的日期陣列
        DateTime ParseRocDate(string rocDateString); // 將民國日期字串轉換為 DateTime 物件
        string ConvertToGregorian(string rocDate); // 將民國日期字串轉換為西元年格式字串
        string GetFirstDayOfLastMonth(); // 獲取上個月的第一天
        string GetLastDayOfLastMonth(); // 獲取上個月的最後一天
        string GetWeekDay(string strDate); // 根據日期字串獲取星期幾
    }
}