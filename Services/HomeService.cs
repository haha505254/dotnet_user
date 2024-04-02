using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class HomeService : IHomeService
    {
        private readonly IHomeRepository _homeRepository;

        public HomeService(IHomeRepository homeRepository)
        {
            _homeRepository = homeRepository;
        }

        // 獲取用戶權限
        public async Task<IEnumerable<dynamic>> GetUserRights(int? id)
        {
            // 根據 id 獲取部門別
            var dept = await _homeRepository.GetDepartment(id);

            // 根據部門別獲取用戶權限
            var userRights = await _homeRepository.GetUserRights(dept);

            return userRights;
        }
    }
}