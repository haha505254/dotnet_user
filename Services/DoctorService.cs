using System;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Repositories;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDateService _dateService;

        public DoctorService(IDoctorRepository doctorRepository, IDateService dateService)
        {
            _doctorRepository = doctorRepository;
            _dateService = dateService;
        }

        // ������w�Ѽƫe���_�l���
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // �����v�ɬq�O��
        public async Task<dynamic> GetDoctorRecords(string strDate)
        {
            // �p�G strDate ���ũΪť�,�h�ϥη�e����@���_�l���
            strDate = !string.IsNullOrEmpty(strDate) ? strDate.Replace("-", "") : _dateService.GetStartDate(0);
            // ����P���X
            var week = _dateService.GetWeekDay(strDate);
            // �p�G�P���� 0,�h�ഫ�� 7
            week = week == "0" ? "7" : week;
            // �N����ഫ������~�榡
            strDate = (int.Parse(strDate.Substring(0, 4)) - 1911).ToString() + strDate.Substring(4, 4);

            // ������w�������v�𰲰O��
            var off = await _doctorRepository.GetDoctorOff(strDate);
            // ����~���
            var yearMonth = strDate.Substring(0, 5);
            // �����v�ɬq�O��
            var records = await _doctorRepository.GetDoctorRecords(yearMonth, week, off);

            // �N��v�ɬq�O���ഫ�����w�榡
            var result = records.Select(r => new
            {
                ��O = (string)r.��O,
                �E�O = (string)r.�E�O,
                �Z�O = (string)r.�Z�O,
                �N�� = (string)r.��v�N��,
                �m�W = (string)r.�m�W,
                �Ƶ� = (string)r.�Ƶ�
            }).ToList();

            // �p�G���G����,�h�K�[�@���ŰO��
            if (!result.Any())
            {
                result.Add(new { ��O = "", �E�O = "", �Z�O = "", �N�� = "", �m�W = "", �Ƶ� = "" });
            }

            // �����v�N�Z�O��
            var recordSecond = await _doctorRepository.GetDoctorSecondRecords(strDate);

            // �N��v�N�Z�O���ഫ�����w�榡
            var resultSecond = recordSecond.Select(r => new
            {
                ��O = (string)r.��O,
                �E�O = (string)r.�E�O,
                �Z�O = (string)r.�Z�O,
                �N�� = (string)r.��v�N��,
                �m�W = (string)r.�m�W
            }).ToList();

            // �p�G���G����,�h�K�[�@���ŰO��
            if (!resultSecond.Any())
            {
                resultSecond.Add(new { ��O = "", �E�O = "", �Z�O = "", �N�� = "", �m�W = "" });
            }

            // ��^��v�ɬq�O���M��v�N�Z�O��
            return new { Result = result, ResultSecond = resultSecond };
        }
    }
}