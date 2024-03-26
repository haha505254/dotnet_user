using System.Threading.Tasks;

namespace dotnet_user.Services
{
    public interface IDiagnosisService
    {
        string GetFirstDayOfLastMonth(); //取得上個月第一天
        string GetLastDayOfLastMonth(); //取得上個月最後一天
        Task<dynamic> GetDiagnosisRecords(string str_date, string end_date); //取得診斷書記錄
    }
}