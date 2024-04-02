using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IHomeRepository
    {
        Task<string> GetDepartment(int? id); // �ھ� id ��������O
        Task<IEnumerable<dynamic>> GetUserRights(string dept); // �ھڳ����O����Τ��v��
    }
}