using System.Collections.Generic;

namespace UniversalDashboard.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Date { get; set; }
        public List<OrderProduct> Products { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } // e.g., "Pending", "Shipped"
    }

    public class OrderProduct
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
