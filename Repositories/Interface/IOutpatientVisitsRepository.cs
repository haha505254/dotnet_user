using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IOutpatientVisitsRepository
    {
        Task<IEnumerable<dynamic>> GetOutpatientVisitsData(string str_date, string end_date, int countValue); // �ھڤ���d��M�ݶE�H�ƪ��e������E�ݶE�H�Ƹ��
    }
}