using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;

namespace dotnet_user.Services
{
    public class DateService : IDateService
    {
        public string GetCurrentDate()
        {
            return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public string GetStartDate(int daysAgo)
        {
            var startDate = DateTime.Now.AddDays(-daysAgo);
            return startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public int GetDaysDifference(string strDate, string endDate)
        {
            var startDate = DateTime.ParseExact(strDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDateTime = DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            return (endDateTime - startDate).Days;
        }

        public IEnumerable<dynamic> GetDateArray(string endDate, int days)
        {
            var endDateTime = DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            var dates = new List<dynamic>();

            for (int i = 0; i < days; i++)
            {
                dynamic date = new ExpandoObject();
                var currentDate = endDateTime.AddDays(-days + i + 1);

                date.日期 = (currentDate.Year - 1911).ToString("D4") + currentDate.ToString("MMdd");
                date.data1 = "0";
                date.data2 = "0";
                date.data3 = "0";
                date.data4 = "0";

                dates.Add(date);
            }

            return dates;
        }

        public DateTime ParseRocDate(string rocDateString)
        {
            int rocYear = 0, month = 0, day = 0;
            DateTime recordDate = DateTime.MinValue;

            if (rocDateString.Length == 7 && int.TryParse(rocDateString.Substring(0, 3), out rocYear)
                && int.TryParse(rocDateString.Substring(3, 2), out month)
                && int.TryParse(rocDateString.Substring(5, 2), out day))
            {
                int westernYear = rocYear + 1911;
                recordDate = new DateTime(westernYear, month, day);
            }

            return recordDate;
        }

        public string ConvertToGregorian(string rocDate)
        {
            if (DateTime.TryParseExact(rocDate, "yyyyMMdd", null, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.AddYears(1911).ToString("yyyyMMdd");
            }
            return rocDate;
        }

        public string GetFirstDayOfLastMonth()
        {
            var firstDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            return firstDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public string GetLastDayOfLastMonth()
        {
            var lastDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            return lastDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public string GetWeekDay(string strDate)
        {
            var year = int.Parse(strDate.Substring(0, 4));
            var month = int.Parse(strDate.Substring(4, 2));
            var day = int.Parse(strDate.Substring(6, 2));
            var date = new DateTime(year, month, day);
            return ((int)date.DayOfWeek).ToString();
        }
    }
}