using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IOutpatientVisitsService
    {
        Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, string count); // ������E�ݶE�H�Ƹ��
    }
}