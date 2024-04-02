using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface INursePatientRepository
    {
        Task<IEnumerable<dynamic>> GetNursePatientRatios(); // 獲取護病比資料
        Task UpdateNursePatientRatio(string month, string enMonth, decimal ratio); // 更新護病比資料
    }
}