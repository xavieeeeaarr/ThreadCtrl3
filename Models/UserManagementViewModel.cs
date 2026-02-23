using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Role> Roles { get; set; } = new List<Role>();
        public User NewUser { get; set; } = new User();
        public string CurrentFilter { get; set; } = "All";
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }
}
