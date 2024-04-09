using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IRegisterRepository
    {
        Task<IEnumerable<dynamic>> GetRegisterRecords(string strDate); // 根據日期獲取預掛名單記錄
    }
}