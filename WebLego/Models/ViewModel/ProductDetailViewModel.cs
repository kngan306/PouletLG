using WebLego.DataSet.GdrService;

namespace WebLego.Models.ViewModel
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public bool IsFavorite { get; set; }
        public int StockQuantity { get; set; }
        //public List<ProductReview> Reviews { get; set; }
        public List<ProductReviewViewModel> Reviews { get; set; }
    }
}