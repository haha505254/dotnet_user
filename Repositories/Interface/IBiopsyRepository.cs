using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_user.Repositories.Interface
{
    public interface IBiopsyRepository
    {
        Task<IEnumerable<dynamic>> GetOutpatientRecords(string strDate, string endDate); // 取得門診紀錄
        Task<IEnumerable<dynamic>> GetInpatientRecords(string strDate, string endDate); // 取得住院紀錄
        Task<IEnumerable<dynamic>> GetBiopsyRemarks(int counter); // 取得切片備註
        Task<IEnumerable<dynamic>> GetRelatedRecords(int counter, string remark); // 取得關連紀錄
    }
}