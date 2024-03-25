using System.Threading.Tasks;

namespace dotnet_user.Services.Interface
{
    public interface IBodyService
    {
        string GetCurrentDate(); // �����e���
        string GetStartDate(int daysAgo); // ������w�Ѽƫe�����
        Task<dynamic> GetBodyRecords(string id, string str_date, string end_date); // ����Ͳz�q���O��
    }
}