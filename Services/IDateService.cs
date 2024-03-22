using System;
using System.Collections.Generic;

namespace dotnet_user.Services
{
    public interface IDateService
    {
        string GetCurrentDate();
        string GetStartDate(int daysAgo);
        int GetDaysDifference(string strDate, string endDate);
        IEnumerable<dynamic> GetDateArray(string endDate, int days);
        DateTime ParseRocDate(string rocDateString);
        string ConvertToGregorian(string rocDate);
        string GetFirstDayOfLastMonth();
        string GetLastDayOfLastMonth();
        string GetWeekDay(string strDate);
    }
}