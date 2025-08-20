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
            PagedResponse<T> pagedData,
            string sheetName = "Data",
            Dictionary<string, Func<T, object>>? columnMappings = null)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                if (pagedData == null || pagedData.Items == null || !pagedData.Items.Any())
                {
                    return new ApiResponse<byte[]>
                    {
                        Success = false,
                        Message = "Không có dữ liệu để xuất.",
                        Status = 400,
                        Data = null
                    };
                }

                var data = pagedData.Items.ToList();
                int totalRecords = pagedData.TotalItems;

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
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
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

                // Định dạng số và tiền tệ
                ApplyNumberFormatting<T>(worksheet, headers, data.Count);

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add borders
                var dataRange = worksheet.Cells[1, 1, data.Count + 1, columnCount];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Thêm thông tin phân trang vào footer (tùy chọn)
                worksheet.Cells[data.Count + 3, 1].Value = $"Tổng số bản ghi: {totalRecords}";
                worksheet.Cells[data.Count + 4, 1].Value = $"Trang hiện tại: {pagedData.CurrentPage}/{pagedData.TotalPages}";
                worksheet.Cells[data.Count + 5, 1].Value = $"Số bản ghi mỗi trang: {pagedData.ItemsPerPage}";

                return new ApiResponse<byte[]>
                {
                    Success = true,
                    Message = $"Xuất file Excel thành công với {data.Count} bản ghi (tổng cộng {totalRecords} bản ghi).",
                    Status = 200,
                    Data = package.GetAsByteArray()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = $"Lỗi khi xuất file Excel: {ex.Message}",
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
                    // TimeOnly fields
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

                // Kiểm tra theo tên header tiếng Việt
                if (IsDateTimeFieldByHeader(header))
                {
                    var columnRange = worksheet.Cells[2, j + 1, dataRowCount + 1, j + 1];
                    columnRange.Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                }
            }
        }

        private void ApplyNumberFormatting<T>(ExcelWorksheet worksheet, List<string> headers, int dataRowCount)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int j = 0; j < headers.Count; j++)
            {
                var header = headers[j];
                var columnRange = worksheet.Cells[2, j + 1, dataRowCount + 1, j + 1];

                // Format tiền tệ cho các trường giá tiền
                if (IsPriceField(header))
                {
                    columnRange.Style.Numberformat.Format = "#,##0 ₫";
                    columnRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }
                // Format số nguyên
                else if (IsCountField(header))
                {
                    columnRange.Style.Numberformat.Format = "0";
                    columnRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
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

        private bool IsDateTimeFieldByHeader(string header)
        {
            var dateTimeHeaders = new[] {
                "Ngày nhận sân", "Thời gian đặt sân", "Thời gian thanh toán",
                "Ngày tạo", "Ngày cập nhật", "Thời gian"
            };

            return dateTimeHeaders.Any(pattern =>
                header.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                header.Contains("Ngày", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("Thời gian", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPriceField(string fieldName)
        {
            var priceFieldNames = new[]
            {
        "TotalPrice", "PaymentAmount", "Price", "Amount",
        "Tổng giá tiền", "Số tiền thanh toán", "Giá tiền", "Thành tiền",
        // Admin report
        "TotalRevenue", "AverageRevenuePerBooking",
        "Tổng doanh thu", "Doanh thu trung bình/Đơn"
    };

            return priceFieldNames.Any(pattern =>
                fieldName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Revenue", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("tiền", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Giá", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Doanh thu", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsCountField(string fieldName)
        {
            var countFieldNames = new[] {
                "CourtCount", "TimeSlotCount", "Count",
                "Số lượng sân", "Số khung giờ", "Số lượng"
            };

            return countFieldNames.Any(pattern =>
                fieldName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Count", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Số lượng", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Contains("Số khung", StringComparison.OrdinalIgnoreCase));
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

                // Chuyển đổi tên property sang tiếng Việt
                var vietnameseName = GetVietnamesePropertyName(prop.Name);

                // Xử lý DateTime
                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    mappings[vietnameseName] = item =>
                    {
                        var value = prop.GetValue(item);
                        return value ?? DateTime.MinValue;
                    };
                }
                // Xử lý TimeSpan
                else if (prop.PropertyType == typeof(TimeSpan) || prop.PropertyType == typeof(TimeSpan?))
                {
                    mappings[vietnameseName] = item =>
                    {
                        var value = prop.GetValue(item);
                        return value ?? TimeSpan.Zero;
                    };
                }
                // Xử lý TimeOnly
                else if (prop.PropertyType == typeof(TimeOnly) || prop.PropertyType == typeof(TimeOnly?))
                {
                    mappings[vietnameseName] = item =>
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
                    mappings[vietnameseName] = item =>
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
                    mappings[vietnameseName] = item => prop.GetValue(item) ?? "";
                }
            }

            return mappings;
        }

        private string GetVietnamesePropertyName(string propertyName)
        {
            var propertyMappings = new Dictionary<string, string>
            {
                // Booking
                ["BookingId"] = "Mã đặt sân",
                ["CustomerName"] = "Tên khách hàng",
                ["CustomerEmail"] = "Email khách hàng",
                ["CustomerPhone"] = "Số điện thoại khách hàng",
                ["CourtCount"] = "Số lượng sân",
                ["CourtCategories"] = "Loại sân",
                ["TimeSlotCount"] = "Số khung giờ",
                ["CheckInDate"] = "Ngày nhận sân",
                ["TotalPrice"] = "Tổng giá tiền",
                ["BookingTime"] = "Thời gian đặt sân",
                ["BookingStatus"] = "Trạng thái đặt sân",

                // Common
                ["Id"] = "Mã",
                ["Name"] = "Tên",
                ["Description"] = "Mô tả",
                ["CreatedDate"] = "Ngày tạo",
                ["UpdatedDate"] = "Ngày cập nhật",
                ["Status"] = "Trạng thái",
                ["IsActive"] = "Hoạt động",
                ["Phone"] = "Số điện thoại",
                ["Email"] = "Email",
                ["Address"] = "Địa chỉ",

                // Admin report
                ["TotalBooking"] = "Tổng lượt đặt sân",
                ["TotalRevenue"] = "Tổng doanh thu",
                ["AverageRevenuePerBooking"] = "Doanh thu trung bình/Đơn",
                ["CompletedBookings"] = "Đơn hoàn thành",
                ["CancelledBookings"] = "Đơn huỷ",
                ["TotalFacilities"] = "Tổng số cơ sở",
                ["TotalCourts"] = "Tổng số sân",
                ["ActiveUsers"] = "Người dùng hoạt động",
                ["FacilityId"] = "Mã cơ sở",
                ["FacilityName"] = "Tên cơ sở",
                ["CategoryName"] = "Tên loại sân"
            };

            return propertyMappings.ContainsKey(propertyName) ? propertyMappings[propertyName] : propertyName;
        }
    }
}
