using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IDiagnosisRepository
    {
        Task<IEnumerable<dynamic>> GetDiagnosisCountByMonth(string strDate, string endDate); //���o�C�Ӥ몺�E�_�Ѽƶq
        Task<IEnumerable<dynamic>> GetDiagnosisCountByDoctorAndMonth(string strDate, string endDate); // ���o�C����v�b�C�Ӥ몺�E�_�Ѽƶq
    }
}