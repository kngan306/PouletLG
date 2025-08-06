namespace WebLego.Models.ViewModel
{
    public class AddressViewModel
    {
        public int AddressId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string SpecificAddress { get; set; }
        public string AddressType { get; set; }
        public bool IsDefault { get; set; }
    }
}
    
