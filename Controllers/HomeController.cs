using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
public class HomeController : Controller
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(int? id)
    {
        string connectionStringTgsql = _configuration.GetConnectionString("TgsqlConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgsqlConnection' 連接字符串。");
        string connectionStringTgvis = _configuration.GetConnectionString("TgvisConnection") ?? throw new InvalidOperationException("未在配置中找到 'TgvisConnection' 連接字符串。");

        // 查詢人事資料檔以獲取部門別
        string sqlDept = "SELECT 部門別 FROM 人事資料檔 WHERE counter = @Id";
        string dept;
        using (var connection = new SqlConnection(connectionStringTgsql))
        {
            var deptResult = await connection.QueryFirstOrDefaultAsync<string>(sqlDept, new { Id = id ?? 0 });
            dept = deptResult ?? "guest";
        }

        // 根據部門別查詢用戶權限
        string sqlUserRight = "SELECT a.dept, b.name, b.icon FROM adminUserRight as a " +
                              "JOIN adminPage as b ON a.pageNo = b.no " +
                              "WHERE a.dept = @Dept";
        IEnumerable<dynamic> userRights;
        using (var connection = new SqlConnection(connectionStringTgvis))
        {
            userRights = await connection.QueryAsync<dynamic>(sqlUserRight, new { Dept = dept });
        }

        ViewData["UserRights"] = userRights; // 将 userRights 添加到 ViewData
        return View();
    }
}