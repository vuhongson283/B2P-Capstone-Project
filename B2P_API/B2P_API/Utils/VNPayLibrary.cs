using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace B2P_API.Utils
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _requestData[key] = value;
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _responseData[key] = value;
        }

        public string GetResponseData(string key) =>
            _responseData.TryGetValue(key, out var value) ? value : string.Empty;

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var queryBuilder = new StringBuilder(256);

            foreach (var (key, value) in _requestData)
            {
                queryBuilder.Append(Uri.EscapeDataString(key))
                           .Append('=')
                           .Append(Uri.EscapeDataString(value))
                           .Append('&');
            }

            string queryString = queryBuilder.ToString(0, queryBuilder.Length - 1); // Remove last '&'
            string secureHash = ComputeHash(queryString, hashSecret);

            return new StringBuilder(baseUrl)
                .Append('?')
                .Append(queryString)
                .Append("&vnp_SecureHash=")
                .Append(secureHash)
                .ToString();
        }

        public bool ValidateSignature(string secureHash, string hashSecret)
        {
            var signDataBuilder = new StringBuilder(128);

            foreach (var (key, value) in _responseData)
            {
                if (key != "vnp_SecureHash" && !string.IsNullOrWhiteSpace(value))
                {
                    signDataBuilder.Append(Uri.EscapeDataString(key))
                                 .Append('=')
                                 .Append(Uri.EscapeDataString(value))
                                 .Append('&');
                }
            }

            if (signDataBuilder.Length == 0) return false;

            string unsignedData = signDataBuilder.ToString(0, signDataBuilder.Length - 1);
            return ComputeHash(unsignedData, hashSecret)
                .Equals(secureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHash(string input, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y) =>
            string.Compare(x, y, StringComparison.Ordinal);
    }
}