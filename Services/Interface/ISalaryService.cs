using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface ISalaryService
    {
        Task<List<object>> RecordSalaryAndBonus(int id, int year, string password);
        Task<object> GetSalaryDetail(int id, int year);
        Task<object> GetBonusDetail(int id, int year);
        Task<object> GetDoctorDetail(int id, int year);
    }
}