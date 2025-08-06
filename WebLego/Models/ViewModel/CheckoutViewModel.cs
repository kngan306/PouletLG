namespace WebLego.Models.ViewModel
{
    public class CheckoutViewModel
    {
        public List<AddressViewModel> Addresses { get; set; }
        public List<CartItemViewModel> CartItems { get; set; }
        public string SelectedPaymentMethod { get; set; }
        public int SelectedAddressId { get; set; }
        public string DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; } = 30000;
        public decimal TotalAmount { get; set; }
    }

}
