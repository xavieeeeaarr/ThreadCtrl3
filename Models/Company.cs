namespace ThrdCtrl2.Models
{
    public class Company
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int UserCount { get; set; }
    }
}
