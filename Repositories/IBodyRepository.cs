using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IBodyRepository
    {
        Task<string> GetPersonnelId(string id);
        Task<int> GetPatientCounter(string id);
        Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate);
    }
}