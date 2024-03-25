using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IBodyRepository
    {
        Task<string> GetPersonnelId(string id); // 根據 id 獲取人事資料的身份證字號
        Task<int> GetPatientCounter(string id); // 根據身份證字號獲取病患檔的 counter
        Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate); // 根據 counter 和日期範圍獲取血糖血壓記錄
    }
}