using Microsoft.AspNetCore.Mvc;
using dotnet_user.Services.Interface;
using System.Threading.Tasks;

namespace dotnet_user.Controllers
{
    [Route("護病比更新")]
    public class NursePatientController : Controller
    {
        private readonly INursePatientService _nursePatientService; // 注入 INursePatientService 介面

        public NursePatientController(INursePatientService nursePatientService)
        {
            _nursePatientService = nursePatientService;
        }

        // 首頁 Action,返回護病比更新的檢視
        public async Task<IActionResult> Index()
        {
            // 呼叫 _nursePatientService.GetNursePatientRatios 方法取得護病比資料
            var nursePatientRatios = await _nursePatientService.GetNursePatientRatios();

            // 將護病比資料存儲在 ViewData 中,以傳遞給檢視
            ViewData["NursePatientRatios"] = nursePatientRatios;

            return View();
        }

        // 更新護病比的 Action,接收 month、enMonth 和 ratio 參數
        [HttpPost("nursePatient")]
        public async Task<IActionResult> Update(string month, string enMonth, decimal ratio)
        {
            // 呼叫 _nursePatientService.UpdateNursePatientRatio 方法更新護病比資料
            await _nursePatientService.UpdateNursePatientRatio(month, enMonth, ratio);

            // 設置成功訊息
            TempData["Message"] = "<i class=\"fas fa-check pr-2\"></i>更新成功。";
            TempData["AlertClass"] = "alert-primary";

            // 重定向到首頁
            return RedirectToAction("Index");
        }
    }
}