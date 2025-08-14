using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace B2P_API.Models
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public int Amount { get; set; }

        [Required(ErrorMessage = "Mô tả đơn hàng là bắt buộc")]
        [StringLength(200, ErrorMessage = "Mô tả không được quá 200 ký tự")]
        public string Description { get; set; }

        public string? RedirectUrl { get; set; }

        public string? CallbackUrl { get; set; }

        public string? AppUser { get; set; } = "default_user";

        public Dictionary<string, object> EmbedData { get; set; } = new();

        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public int Price { get; set; }
        public string? Description { get; set; }
    }

    public class ZaloPayOrderResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; set; } = "";

        [JsonPropertyName("order_url")]
        public string OrderUrl { get; set; } = "";

        [JsonPropertyName("zp_trans_token")]
        public string ZpTransToken { get; set; } = "";

        [JsonPropertyName("qr_code")]
        public string QrCode { get; set; } = "";

        [JsonPropertyName("app_trans_id")]
        public string AppTransId { get; set; } = "";
    }

    public class CallbackRequest
    {
        public string Data { get; set; } = "";
        public string Mac { get; set; } = "";
        public int Type { get; set; }
    }

    public class CallbackData
    {
        [JsonPropertyName("app_id")]
        public int AppId { get; set; }

        [JsonPropertyName("app_trans_id")]
        public string AppTransId { get; set; } = string.Empty;

        [JsonPropertyName("app_time")]
        public long AppTime { get; set; }

        [JsonPropertyName("app_user")]
        public string AppUser { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("embed_data")]
        public string EmbedData { get; set; } = string.Empty;

        [JsonPropertyName("item")]
        public string Item { get; set; } = string.Empty;

        [JsonPropertyName("zp_trans_id")]
        public long ZpTransId { get; set; }

        [JsonPropertyName("server_time")]
        public long ServerTime { get; set; }

        [JsonPropertyName("channel")]
        public int Channel { get; set; }

        [JsonPropertyName("merchant_user_id")]
        public string MerchantUserId { get; set; } = string.Empty;

        [JsonPropertyName("zp_user_id")]
        public string ZpUserId { get; set; } = string.Empty;

        [JsonPropertyName("user_fee_amount")]
        public long UserFeeAmount { get; set; }

        [JsonPropertyName("discount_amount")]
        public long DiscountAmount { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<string, object> EmbedDict { get; set; } = new();


    }


    public class QueryOrderRequest
    {
        [Required(ErrorMessage = "App transaction ID là bắt buộc")]
        public string AppTransId { get; set; } = "";
    }

    public class QueryOrderResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; set; } = "";

        [JsonPropertyName("is_processing")]
        public bool IsProcessing { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("zp_trans_id")]
        public long ZpTransId { get; set; }

        [JsonPropertyName("discount_amount")]
        public int DiscountAmount { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
        public int? ErrorCode { get; set; }
    }

    public class ZaloPayConfig
    {
        public string AppId { get; set; } = "";
        public string Key1 { get; set; } = "";
        public string Key2 { get; set; } = "";
        public string Endpoint { get; set; } = "";
    }
}
