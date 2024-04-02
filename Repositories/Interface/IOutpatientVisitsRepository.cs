using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IOutpatientVisitsRepository
    {
        Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, int countValue); // 根據日期範圍和看診人數門檻獲取門診看診人數資料
    }
}