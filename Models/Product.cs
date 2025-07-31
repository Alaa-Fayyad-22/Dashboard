namespace UniversalDashboard.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }      // Not "Name"
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal? CostPrice { get; set; }
        // If you want, add rating as a nested object or ignore for now
    }

}
