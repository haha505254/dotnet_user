using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IEmployeeVisitsService
    {
        Task<List<dynamic>> GetEmployeeVisitsData(string str_date, string end_date); // ����|���ݶE���Ӹ��
        Task<MemoryStream> DownloadEmployeeVisitsData(string str_date, string end_date); // �U���|���ݶE���Ӹ��
    }
}