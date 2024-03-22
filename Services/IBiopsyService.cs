using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services
{
    public interface IBiopsyService
    {
        string GetCurrentDate();
        string GetStartDate(int daysAgo);
        Task<IEnumerable<dynamic>> GetBiopsyData(string strDate, string endDate);
        string FormatBiopsyData(IEnumerable<dynamic> combinedRecords);
        MemoryStream ExportToExcel(IEnumerable<dynamic> dynamicData);
        Task<List<dynamic>> GenerateUpdateSql(IEnumerable<dynamic> dynamicData);
        MemoryStream ExportSqlToExcel(List<dynamic> result);
    }
}