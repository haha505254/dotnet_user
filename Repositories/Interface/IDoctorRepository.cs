using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IDoctorRepository
    {
        Task<List<string>> GetDoctorOff(string strDate); // 獲取指定日期的醫師休假記錄
        Task<IEnumerable<dynamic>> GetDoctorRecords(string yearMonth, string week, List<string> offArray); // 獲取醫師時段記錄
        Task<IEnumerable<dynamic>> GetDoctorSecondRecords(string strDate); // 獲取醫師代班記錄
    }
}