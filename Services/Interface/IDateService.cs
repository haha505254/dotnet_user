using System;
using System.Collections.Generic;

namespace dotnet_user.Services.Interface
{
    public interface IDateService
    {
        string GetCurrentDate(); // �����e���
        string GetStartDate(int daysAgo); // ������w�Ѽƫe�����
        int GetDaysDifference(string strDate, string endDate); // �p���Ӥ���������ѼƮt��
        IEnumerable<dynamic> GetDateArray(string endDate, int days); // ������w����������e���w�Ѽƪ�����}�C
        DateTime ParseRocDate(string rocDateString); // �N�������r���ഫ�� DateTime ����
        string ConvertToGregorian(string rocDate); // �N�������r���ഫ���褸�~�榡�r��
        string GetFirstDayOfLastMonth(); // ����W�Ӥ몺�Ĥ@��
        string GetLastDayOfLastMonth(); // ����W�Ӥ몺�̫�@��
        string GetWeekDay(string strDate); // �ھڤ���r������P���X
    }
}