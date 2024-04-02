using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface ILineService
    {
        Task<dynamic> GetUserData(string id); // ����ϥΪ̸��
        Task UpdateUserSetting(string counter, string email, IDictionary<string, string> notifySettings); // ��s�ϥΪ̳]�w
    }
}