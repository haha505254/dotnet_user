using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IRegisterService
    {
        string GetStartDate(int daysAgo); // 獲取指定天數前的日期
        Task<dynamic> GetRegisterRecords(string id, string str_date); // 獲取預掛名單記錄
    }
}