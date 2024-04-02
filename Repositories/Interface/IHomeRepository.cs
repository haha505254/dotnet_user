using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IHomeRepository
    {
        Task<string> GetDepartment(int? id); // 根據 id 獲取部門別
        Task<IEnumerable<dynamic>> GetUserRights(string dept); // 根據部門別獲取用戶權限
    }
}