using System.Threading.Tasks;
using dotnet_user.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_user.Middleware
{
    public class UserRightsMiddleware
    {
        // 宣告一個 RequestDelegate 類型的私有唯讀欄位 _next，用於儲存下一個中間件的引用
        private readonly RequestDelegate _next;

        // UserRightsMiddleware 的建構函式，接受一個 RequestDelegate 類型的參數 next
        public UserRightsMiddleware(RequestDelegate next)
        {
            // 將傳入的 next 參數賦值給 _next 欄位，以便在 InvokeAsync 方法中調用下一個中間件
            _next = next;
        }

        // 中間件的主要邏輯，每個請求都會經過這個方法
        public async Task InvokeAsync(HttpContext context)
        {
            // 嘗試從查詢字串中解析出 "id" 參數的值，並將其轉換為整數類型，若解析失敗則默認為 0
            int.TryParse(context.Request.Query["id"], out int id);

            // 從服務容器中獲取 IHomeService 的實例
            var homeService = context.RequestServices.GetRequiredService<IHomeService>();

            try
            {
                // 調用 IHomeService 的 GetUserRights 方法，傳入 id 參數，獲取使用者權限
                var userRights = await homeService.GetUserRights(id);

                // 將獲取到的使用者權限存儲在 HttpContext 的 Items 集合中，键為 "UserRights"
                context.Items["UserRights"] = userRights;
            }
            catch (Exception ex)
            {
                // 如果在獲取使用者權限的過程中發生異常，則從服務容器中獲取 ILogger<UserRightsMiddleware> 的實例
                var logger = context.RequestServices.GetRequiredService<ILogger<UserRightsMiddleware>>();

                // 使用 logger 記錄錯誤訊息和異常詳細資訊
                logger.LogError(ex, "An error occurred while retrieving user rights.");
            }

            // 調用 _next 委託，將請求傳遞給下一個中間件進行處理
            await _next(context);
        }
    }
}