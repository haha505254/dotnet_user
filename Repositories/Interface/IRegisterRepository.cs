using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IRegisterRepository
    {
        Task<IEnumerable<dynamic>> GetRegisterRecords(string strDate); // �ھڤ������w���W��O��
    }
}