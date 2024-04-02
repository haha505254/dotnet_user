using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface ILineRepository
    {
        Task<dynamic> GetUserAsync(string id); // �ھ�id����ϥΪ̸��
        Task<IEnumerable<dynamic>> GetUserSettingAsync(string counter); // �ھ�counter����ϥΪ̳]�w
        Task<IEnumerable<dynamic>> GetNotifyItemsAsync(); // ����q������
        Task UpdateUserSettingAsync(string counter, string email, IDictionary<string, string> notifySettings); // ��s�ϥΪ̳]�w
    }
}