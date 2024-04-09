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

        // 獲取指定天數前的日期
        public string GetStartDate(int daysAgo)
        {
            return _dateService.GetStartDate(daysAgo);
        }

        // 獲取預掛名單記錄
        public async Task<dynamic> GetRegisterRecords(string id, string str_date = "")
        {
            // 如果起始日期為空,則設置為前一天的日期,移除連接符號
            str_date = string.IsNullOrWhiteSpace(str_date) ? _dateService.GetStartDate(-1).Replace("-", "") : str_date.Replace("-", "");

            // 將起始日期轉換為民國年格式
            str_date = (int.Parse(str_date.Substring(0, 4)) - 1911).ToString() + str_date.Substring(4, 4);

            // 根據日期獲取預掛名單記錄
            var records = await _registerRepository.GetRegisterRecords(str_date);

            // 構建結果物件
            var result = records.Select(record => new
            {
                病歷號碼 = record.病歷號碼,
                身份證字號 = record.身份證字號,
                患者姓名 = record.患者姓名,
                掛號序號 = record.掛號序號,
                科別 = record.科別,
                診別 = record.診別,
                班別 = record.班別,
                姓名 = record.姓名
            }).ToList();

            return result;
        }
    }
}