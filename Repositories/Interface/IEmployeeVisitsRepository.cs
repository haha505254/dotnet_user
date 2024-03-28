using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IEmployeeVisitsRepository
    {
        Task<IEnumerable<dynamic>> GetEmployeeVisitsRecords(string str_date, string end_date); // ������u�ݶE�O��
        Task<IEnumerable<dynamic>> GetDoctorVisitsRecords(string str_date, string end_date); // �����v�ݶE�O��
    }
}