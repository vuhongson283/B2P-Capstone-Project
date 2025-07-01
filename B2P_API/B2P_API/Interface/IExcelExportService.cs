using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface IExcelExportService
    {
        Task<ApiResponse<byte[]>> ExportToExcelAsync<T>( List<T> data, string sheetName = "Data", 
            Dictionary<string, Func<T, object>>? columnMappings = null);
    }
}
