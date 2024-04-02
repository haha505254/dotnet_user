using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface INursePatientRepository
    {
        Task<IEnumerable<dynamic>> GetNursePatientRatios(); // ����@�f����
        Task UpdateNursePatientRatio(string month, string enMonth, decimal ratio); // ��s�@�f����
    }
}