using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IEmployeeVisitsRepository
    {
        Task<IEnumerable<dynamic>> GetEmployeeVisitsRecords(string str_date, string end_date); // 獲取員工看診記錄
        Task<IEnumerable<dynamic>> GetDoctorVisitsRecords(string str_date, string end_date); // 獲取醫師看診記錄
    }
}