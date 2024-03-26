using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IBiopsyRepository
    {
        Task<IEnumerable<dynamic>> GetOutpatientRecords(string strDate, string endDate); // ���o���E����
        Task<IEnumerable<dynamic>> GetInpatientRecords(string strDate, string endDate); // ���o��|����
        Task<IEnumerable<dynamic>> GetBiopsyRemarks(int counter); // ���o�����Ƶ�
        Task<IEnumerable<dynamic>> GetRelatedRecords(int counter, string remark); // ���o���s����
    }
}