using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Models
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; }

        [Required]
        public string PromotionName { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [ValidateNever]
        public string Status { get; set; }

        [ValidateNever]
        public List<Product> AvailableProducts { get; set; }

        public List<int> SelectedProductIds { get; set; }
    }
}
