using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace dotnet_user.Controllers
{

    [Route("護病比更新")]
    public class NursePatientController : Controller
    {
        private readonly IConfiguration _configuration;
        public NursePatientController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<IActionResult> Index()
        {
            // Retrieve the connection string for the Tgvis database
            string connectionString = _configuration.GetConnectionString("TgvisConnection") ?? throw new InvalidOperationException("Connection string 'TgvisConnection' not found in configuration.");

            // SQL query to fetch nurse-patient ratios (assuming this is related to user permissions)
            string nursePatientRatiosQuery = "SELECT * FROM nursePatientRatios";

            // Define a variable to store the query results
            IEnumerable<dynamic> nursePatientRatios;

            // Execute the query using the database connection
            using (var connection = new SqlConnection(connectionString))
            {
                nursePatientRatios = await connection.QueryAsync<dynamic>(nursePatientRatiosQuery);
            }

            // Store the results in ViewData to pass to the view
            ViewData["NursePatientRatios"] = nursePatientRatios;
            return View();
        }
        [HttpPost("nursePatient")]
        public async Task<IActionResult> Update(string month, string enMonth, decimal ratio)
        {
            string connectionString = _configuration.GetConnectionString("TgvisConnection") ?? throw new InvalidOperationException("Connection string 'TgvisConnection' not found in configuration.");

            using (var connection = new SqlConnection(connectionString))
            {
                string updateQuery = @"UPDATE nursePatientRatios SET 
                                   month = @Month, 
                                   en_month = @EnMonth, 
                                   ratio = @Ratio";

                await connection.ExecuteAsync(updateQuery, new { Month = month, EnMonth = enMonth, Ratio = ratio });
            }

            // 設置成功訊息
            TempData["Message"] = "<i class=\"fas fa-check pr-2\"></i>更新成功。";
            TempData["AlertClass"] = "alert-primary";

            // 從資料庫中重新獲取記錄並重定向到視圖
            return RedirectToAction("Index");
        }
    }
}
