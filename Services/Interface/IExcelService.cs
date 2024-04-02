using System.Collections.Generic;
using System.IO;

namespace dotnet_user.Services.Interface
{
    public interface IExcelService
    {
        MemoryStream ExportToExcel<T>(List<T> data, string sheetName);
    }
}