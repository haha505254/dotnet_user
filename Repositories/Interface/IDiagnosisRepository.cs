using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IDiagnosisRepository
    {
        Task<IEnumerable<dynamic>> GetDiagnosisCountByMonth(string strDate, string endDate); //取得每個月的診斷書數量
        Task<IEnumerable<dynamic>> GetDiagnosisCountByDoctorAndMonth(string strDate, string endDate); // 取得每個醫師在每個月的診斷書數量
    }
}