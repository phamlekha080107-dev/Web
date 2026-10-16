using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace _25DH110384_WebStore.Models.ViewModel
{
    public class ProductSearchVM
    {
        public string SearchTerm { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string SortOrder { get; set; }

        public List<Product> Products { get; set; }
    }
}