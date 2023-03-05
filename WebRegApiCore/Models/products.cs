using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRegApiCore.Models
{
    public class objProducts
    {
        public List<regProduct> Products { get; set; }
    }
    public class regProduct
    {
        public String productId { get; set; }
        public String productName { get; set; }
    }
}