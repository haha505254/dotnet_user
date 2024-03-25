using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IBodyRepository
    {
        Task<string> GetPersonnelId(string id); // �ھ� id ����H�Ƹ�ƪ������Ҧr��
        Task<int> GetPatientCounter(string id); // �ھڨ����Ҧr������f�w�ɪ� counter
        Task<IEnumerable<dynamic>> GetBodyRecords(int counter, string strDate, string endDate); // �ھ� counter �M����d�������}�����O��
    }
}