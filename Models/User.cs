namespace ThrdCtrl2.Models
{
    public class User
    {
        public int UserID { get; set; }
        public int RoleID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string? RoleName { get; set; }
        public string? CompanyStatus { get; set; }
        public int AccessFailedCount { get; set; }
        public System.DateTime? LockoutEnd { get; set; }
    }
}
