using System.Threading.Tasks;

namespace dotnet_user.Services
{
    public interface IBodyService
    {
        string GetCurrentDate();
        string GetStartDate(int daysAgo);
        Task<dynamic> GetBodyRecords(string id, string str_date, string end_date);
    }
}