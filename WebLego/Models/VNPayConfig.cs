namespace WebLego.Models
{
    public class VNPayConfig
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string ReturnUrl { get; set; }
        public string Url { get; set; }
        public string RefundUrl { get; set; }
        public bool SimulateRefund { get; set; }
    }
}
