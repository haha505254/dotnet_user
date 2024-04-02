using System.Collections.Generic;
using System.IO;
using System.Linq;
using dotnet_user.Services.Interface;
using OfficeOpenXml;

namespace dotnet_user.Services
{
    public class ExcelService : IExcelService
    {
        public MemoryStream ExportToExcel<T>(List<T> data, string sheetName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            var properties = data.FirstOrDefault()?.GetType().GetProperties();
            var row = 1;

            if (properties != null)
            {
                for (var i = 0; i < properties.Length; i++)
                {
                    worksheet.Cells[row, i + 1].Value = properties[i].Name;
                }

                foreach (var record in data)
                {
                    row++;
                    for (var i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = properties[i].GetValue(record, null)?.ToString();
                    }
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }
    }
}