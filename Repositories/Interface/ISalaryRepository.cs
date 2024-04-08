using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface ISalaryRepository
    {
        Task<dynamic> UserInfo(int id);
        Task<(int, string)> UserNo(int id);
        Task<bool> CheckPassword(string idNumber, string password);
        Task<IEnumerable<dynamic>> GetSalary(int year, string personnelNumber);
        Task<IEnumerable<dynamic>> GetBonus(int year, string personnelNumber);
        Task<string> GetDepartmentName(string departmentCode);
        Task<dynamic> GetJobInformation(string idNumber);
        Task<IEnumerable<dynamic>> GetSalaryDetails(int counter, int year, string personnelNumber);
        Task<(string, object)> GetBonusQuery(int jobCode, string idNumber);
        Task<IEnumerable<dynamic>> GetBonuses(string query, object parameters);
        Task<List<dynamic>> GetRegisterDetail(string userNo, string year);
        Task<List<dynamic>> GetClinicDetail(string userNo, string year);
        Task<List<dynamic>> GetAdmissionDetail(string userNo, string year);
        Task<List<dynamic>> GetMedicineDetail(string userNo, string year);
        Task<IEnumerable<dynamic>> GetPersonnelCounter(string userNo);
        Task<IEnumerable<dynamic>> GetNotes(int counter, string year);
        Task<IEnumerable<dynamic>> GetLastClinics(string userNo, string year);
        Task<List<dynamic>> GetLastClinicsAmount(string userNo, string year);
        Task<IEnumerable<dynamic>> GetLastAdmission(string userNo, string year);
        Task<List<dynamic>> GetLastAdmissionAmount(string userNo, string year);
    }
}