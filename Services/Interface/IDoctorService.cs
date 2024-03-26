using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IDoctorService
    {
        string GetStartDate(int daysAgo);// 獲取指定天數前的起始日期
        Task<dynamic> GetDoctorRecords(string strDate);// 獲取醫師時段記錄
    }
}