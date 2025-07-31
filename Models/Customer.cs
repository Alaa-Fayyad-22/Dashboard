namespace UniversalDashboard.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public DateTime Created_Date { get; set; } // match JSON field
    }
}
