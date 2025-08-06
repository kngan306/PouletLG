using WebLego.DataSet.GdrService;
using System.Collections.Generic;

namespace WebLego.Models.ViewModel
{
    public class ProductListViewModel
    {
        public string CategoryName { get; set; }
        public List<Product> Products { get; set; }
    }
}
