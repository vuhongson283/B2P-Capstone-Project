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

                // Định dạng các cột DateTime
                ApplyDateTimeFormatting<T>(worksheet, headers, data.Count);

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

        private void ApplyDateTimeFormatting<T>(ExcelWorksheet worksheet, List<string> headers, int dataRowCount)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int j = 0; j < headers.Count; j++)
            {
                var header = headers[j];
                var property = properties.FirstOrDefault(p => p.Name == header);

                if (property != null)
                {
                    var propertyType = property.PropertyType;
                    var columnRange = worksheet.Cells[2, j + 1, dataRowCount + 1, j + 1];

                    // DateTime fields
                    if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                    {
                        columnRange.Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                    }
                    // TimeSpan fields
                    else if (propertyType == typeof(TimeSpan) || propertyType == typeof(TimeSpan?))
                    {
                        columnRange.Style.Numberformat.Format = "hh:mm:ss";
                    }
                    // **THÊM XỬ LÝ CHO TIMEONLY**
                    else if (propertyType == typeof(TimeOnly) || propertyType == typeof(TimeOnly?))
                    {
                        columnRange.Style.Numberformat.Format = "hh:mm:ss";
                    }
                    // String time fields (startTime, endTime, etc.)
                    else if (propertyType == typeof(string) && IsTimeField(header))
                    {
                        columnRange.Style.Numberformat.Format = "hh:mm:ss";
                    }
                }
            }
        }

        private bool IsTimeField(string fieldName)
        {
            var timeFieldNames = new[] {
                "startTime", "endTime", "time", "hour",
                "StartTime", "EndTime", "Time", "Hour"
            };

            return timeFieldNames.Any(pattern =>
                fieldName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Hour", StringComparison.OrdinalIgnoreCase));
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

                // Xử lý DateTime
                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    mappings[prop.Name] = item =>
                    {
                        var value = prop.GetValue(item);
                        return value ?? DateTime.MinValue;
                    };
                }
                // Xử lý TimeSpan
                else if (prop.PropertyType == typeof(TimeSpan) || prop.PropertyType == typeof(TimeSpan?))
                {
                    mappings[prop.Name] = item =>
                    {
                        var value = prop.GetValue(item);
                        return value ?? TimeSpan.Zero;
                    };
                }
                // Xử lý TimeOnly - PHẦN ĐÃ SỬA
                else if (prop.PropertyType == typeof(TimeOnly) || prop.PropertyType == typeof(TimeOnly?))
                {
                    mappings[prop.Name] = item =>
                    {
                        var value = prop.GetValue(item);
                        if (value is TimeOnly timeOnly)
                        {
                            return timeOnly.ToTimeSpan();
                        }
                        else if (value != null && value is TimeOnly?)
                        {
                            var nullableTimeOnly = (TimeOnly?)value;
                            if (nullableTimeOnly.HasValue)
                            {
                                return nullableTimeOnly.Value.ToTimeSpan();
                            }
                        }
                        return TimeSpan.Zero;
                    };
                }
                // Xử lý string time fields
                else if (prop.PropertyType == typeof(string) && IsTimeField(prop.Name))
                {
                    mappings[prop.Name] = item =>
                    {
                        var value = prop.GetValue(item)?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (TimeSpan.TryParse(value, out TimeSpan timeValue))
                            {
                                return timeValue;
                            }
                            return value;
                        }
                        return "";
                    };
                }
                else
                {
                    mappings[prop.Name] = item => prop.GetValue(item) ?? "";
                }
            }

            return mappings;
        }
    }
}

