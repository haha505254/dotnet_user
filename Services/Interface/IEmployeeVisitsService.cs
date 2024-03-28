using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IEmployeeVisitsService
    {
        Task<List<dynamic>> GetEmployeeVisitsData(string str_date, string end_date); // 獲取院內看診明細資料
        Task<MemoryStream> DownloadEmployeeVisitsData(string str_date, string end_date); // 下載院內看診明細資料
    }
}