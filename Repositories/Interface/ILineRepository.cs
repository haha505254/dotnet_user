using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface ILineRepository
    {
        Task<dynamic> GetUserAsync(string id); // 根據id獲取使用者資料
        Task<IEnumerable<dynamic>> GetUserSettingAsync(string counter); // 根據counter獲取使用者設定
        Task<IEnumerable<dynamic>> GetNotifyItemsAsync(); // 獲取通知項目
        Task UpdateUserSettingAsync(string counter, string email, IDictionary<string, string> notifySettings); // 更新使用者設定
    }
}