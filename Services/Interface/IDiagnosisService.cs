using System.Threading.Tasks;

namespace dotnet_user.Services
{
    public interface IDiagnosisService
    {
        string GetFirstDayOfLastMonth();
        string GetLastDayOfLastMonth();
        Task<dynamic> GetDiagnosisRecords(string str_date, string end_date);
    }
}