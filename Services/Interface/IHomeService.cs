using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IHomeService
    {
        Task<IEnumerable<dynamic>> GetUserRights(int? id); // 獲取用戶權限
    }
}