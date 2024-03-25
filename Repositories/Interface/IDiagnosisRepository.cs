using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IDiagnosisRepository
    {
        Task<IEnumerable<dynamic>> GetDiagnosisCountByMonth(string strDate, string endDate);
        Task<IEnumerable<dynamic>> GetDiagnosisCountByDoctorAndMonth(string strDate, string endDate);
    }
}