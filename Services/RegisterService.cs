using System;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class RegisterService : IRegisterService
    {
        private readonly IRegisterRepository _registerRepository;
        private readonly IDateService _dateService;

        public RegisterService(IRegisterRepository registerRepository, IDateService dateService)
        {
            _registerRepository = registerRepository;
            _dateService = dateService;
        }

        // ������w�Ѽƫe�����
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // ����w���W��O��
        public async Task<dynamic> GetRegisterRecords(string id, string str_date = "")
        {
            // �p�G�_�l�������,�h�]�m���e�@�Ѫ����,�����s���Ÿ�
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(-1).Replace("-", "") : str_date.Replace("-", "");

            // �N�_�l����ഫ������~�榡
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);

            // �ھڤ������w���W��O��
            var records = await _registerRepository.GetRegisterRecords(str_date);

            // �c�ص��G����
            var result = records.Select(record => new
            {
                �f�����X = record.�f�����X,
                �����Ҧr�� = record.�����Ҧr��,
                �w�̩m�W = record.�w�̩m�W,
                �����Ǹ� = record.�����Ǹ�,
                ��O = record.��O,
                �E�O = record.�E�O,
                �Z�O = record.�Z�O,
                �m�W = record.�m�W
            }).ToList();

            return result;
        }
    }
}