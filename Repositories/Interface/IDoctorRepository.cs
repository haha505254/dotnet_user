using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IDoctorRepository
    {
        Task<List<string>> GetDoctorOff(string strDate); // ������w�������v�𰲰O��
        Task<IEnumerable<dynamic>> GetDoctorRecords(string yearMonth, string week, List<string> offArray); // �����v�ɬq�O��
        Task<IEnumerable<dynamic>> GetDoctorSecondRecords(string strDate); // �����v�N�Z�O��
    }
}