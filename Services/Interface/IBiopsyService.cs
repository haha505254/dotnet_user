using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IBiopsyService
    {
        // 取得當前日期
        string GetCurrentDate();
        // 取得指定天數前的日期
        string GetStartDate(int daysAgo);
        // 取得切片報告資料
        Task<IEnumerable<dynamic>> GetBiopsyData(string strDate, string endDate);
        // 格式化切片報告資料
        string FormatBiopsyData(IEnumerable<dynamic> combinedRecords);
        // 將資料匯出為 Excel
        MemoryStream ExportToExcel(IEnumerable<dynamic> dynamicData);
        // 產生更新資料的 SQL 語句
        Task<List<dynamic>> GenerateUpdateSql(IEnumerable<dynamic> dynamicData);
        // 將 SQL 語句匯出為 Excel
        MemoryStream ExportSqlToExcel(List<dynamic> result);
    }
}