using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class LineService : ILineService
    {
        private readonly ILineRepository _lineRepository;

        public LineService(ILineRepository lineRepository)
        {
            _lineRepository = lineRepository;
        }

        // 獲取使用者資料
        public async Task<dynamic> GetUserData(string id)
        {
            // 呼叫 _lineRepository.GetUserAsync 方法獲取使用者資料
            var user = await _lineRepository.GetUserAsync(id);

            string counter = user.Counter?.ToString() ?? throw new System.InvalidOperationException("Counter 無效。");
            string email = user.Email帳號?.ToString() ?? throw new System.InvalidOperationException("Email帳號 無效。");

            // 呼叫 _lineRepository.GetUserSettingAsync 方法獲取使用者設定
            var userSetting = await _lineRepository.GetUserSettingAsync(counter);
            // 呼叫 _lineRepository.GetNotifyItemsAsync 方法獲取通知項目
            var items = await _lineRepository.GetNotifyItemsAsync();

            return new
            {
                User = user,
                Counter = counter,
                Email = email,
                UserSetting = userSetting,
                Items = items
            };
        }

        // 更新使用者設定
        public async Task UpdateUserSetting(string counter, string email, IDictionary<string, string> notifySettings)
        {
            // 呼叫 _lineRepository.UpdateUserSettingAsync 方法更新使用者設定
            await _lineRepository.UpdateUserSettingAsync(counter, email, notifySettings);
        }
    }
}