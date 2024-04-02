using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface ILineService
    {
        Task<dynamic> GetUserData(string id); // 獲取使用者資料
        Task UpdateUserSetting(string counter, string email, IDictionary<string, string> notifySettings); // 更新使用者設定
    }
}