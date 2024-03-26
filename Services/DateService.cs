using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class DateService : IDateService
    {
        // 獲取當前日期
        public string GetCurrentDate()
        {
            return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // 獲取指定天數前的日期
        public string GetStartDate(int daysAgo)
        {
            var startDate = DateTime.Now.AddDays(-daysAgo);
            return startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // 計算兩個日期之間的天數差異
        public int GetDaysDifference(string strDate, string endDate)
        {
            var startDate = DateTime.ParseExact(strDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDateTime = DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            return (endDateTime - startDate).Days;
        }

        /// <summary>
        /// 獲取指定結束日期之前指定天數的日期陣列
        /// </summary>
        /// <param name="endDate">結束日期,格式為 "yyyyMMdd"</param>
        /// <param name="days">天數</param>
        /// <returns>包含日期資訊的動態物件陣列</returns>
        /// <example>
        /// 輸入:
        ///   endDate = "20230530"
        ///   days = 7
        /// 輸出:
        ///   [
        ///     {
        ///       "日期": "1120524",
        ///       "data1": "0",
        ///       "data2": "0",
        ///       "data3": "0",
        ///       "data4": "0"
        ///     },
        ///     {
        ///       "日期": "1120525",
        ///       "data1": "0",
        ///       "data2": "0",
        ///       "data3": "0",
        ///       "data4": "0"
        ///     },
        ///     ...
        ///     {
        ///       "日期": "1120530",
        ///       "data1": "0",
        ///       "data2": "0",
        ///       "data3": "0",
        ///       "data4": "0"
        ///     }
        ///   ]
        /// </example>
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

        // 將民國日期字串轉換為西元年格式字串
        public string ConvertToGregorian(string rocDate)
        {
            if (DateTime.TryParseExact(rocDate, "yyyyMMdd", null, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.AddYears(1911).ToString("yyyyMMdd");
            }
            return rocDate;
        }

        // 獲取上個月的第一天
        public string GetFirstDayOfLastMonth()
        {
            var firstDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            return firstDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // 獲取上個月的最後一天
        public string GetLastDayOfLastMonth()
        {
            var lastDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            return lastDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // 根據日期字串獲取星期幾
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