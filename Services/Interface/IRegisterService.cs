using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IRegisterService
    {
        string GetStartDate(int daysAgo); // ������w�Ѽƫe�����
        Task<dynamic> GetRegisterRecords(string id, string str_date); // ����w���W��O��
    }
}