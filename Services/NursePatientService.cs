using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class NursePatientService : INursePatientService
    {
        private readonly INursePatientRepository _nursePatientRepository;

        public NursePatientService(INursePatientRepository nursePatientRepository)
        {
            _nursePatientRepository = nursePatientRepository;
        }

        // 獲取護病比資料
        public async Task<IEnumerable<dynamic>> GetNursePatientRatios()
        {
            return await _nursePatientRepository.GetNursePatientRatios();
        }

        // 更新護病比資料
        public async Task UpdateNursePatientRatio(string month, string enMonth, decimal ratio)
        {
            await _nursePatientRepository.UpdateNursePatientRatio(month, enMonth, ratio);
        }
    }
}