using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IBodyService
    {
        string GetCurrentDate(); // 獲取當前日期
        string GetStartDate(int daysAgo); // 獲取指定天數前的日期
        Task<dynamic> GetBodyRecords(string id, string str_date, string end_date); // 獲取生理量測記錄
    }
}