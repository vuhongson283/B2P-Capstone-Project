namespace B2P_API.Response
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
        public T Data { get; set; }
    }
}
