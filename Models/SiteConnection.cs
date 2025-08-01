namespace UniversalDashboard.Models
{
    public class SiteConnection
    {
        public int Id { get; set; }
        public string Name { get; set; }          // Display name (e.g. "MyShop1")
        public string ApiUrl { get; set; }        // Base API endpoint
        public string ApiKey { get; set; }        // Optional: API key/token
        public string? IconUrl { get; set; }
    }

}
