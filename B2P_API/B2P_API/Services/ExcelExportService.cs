using B2P_API.Interface;
using B2P_API.Response;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Reflection;

namespace B2P_API.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public async Task<ApiResponse<byte[]>> ExportToExcelAsync<T>(
            List<T> data,
            string sheetName = "Data",
            Dictionary<string, Func<T, object>>? columnMappings = null)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                if (data == null || data.Count == 0)
                {
                    return new ApiResponse<byte[]>
                    {
                        Success = false,
                        Message = "No data available to export.",
                        Status = 400,
                        Data = null
                    };
                }

                // Nếu không có columnMappings, sử dụng reflection để lấy tất cả properties
                if (columnMappings == null)
                {
                    columnMappings = GetDefaultColumnMappings<T>();
                }

                var headers = columnMappings.Keys.ToList();
                var columnCount = headers.Count;

                // Tạo header
                for (int i = 0; i < columnCount; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Style header
                using (var range = worksheet.Cells[1, 1, 1, columnCount])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Thêm dữ liệu
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int row = i + 2;

                    for (int j = 0; j < columnCount; j++)
                    {
                        var header = headers[j];
                        var valueFunc = columnMappings[header];
                        var value = valueFunc(item);

                        worksheet.Cells[row, j + 1].Value = value;
                    }
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add borders
                var dataRange = worksheet.Cells[1, 1, data.Count + 1, columnCount];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                return new ApiResponse<byte[]>
                {
                    Success = true,
                    Message = $"Excel file exported successfully with {data.Count} records.",
                    Status = 200,
                    Data = package.GetAsByteArray()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = $"Error exporting to Excel: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }

        private Dictionary<string, Func<T, object>> GetDefaultColumnMappings<T>()
        {
            var mappings = new Dictionary<string, Func<T, object>>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Bỏ qua navigation properties và collections
                if (prop.PropertyType.IsClass &&
                    prop.PropertyType != typeof(string) &&
                    prop.PropertyType != typeof(DateTime) &&
                    prop.PropertyType != typeof(DateTime?))
                    continue;

                mappings[prop.Name] = item => prop.GetValue(item) ?? "";
            }

            return mappings;
        }
    }
}
