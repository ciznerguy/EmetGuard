namespace Model
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public User() { }

        // משתמש שעדיין לא נשמר בבסיס הנתונים (אין לו מזהה)
        public User(string username, string email, string password)
        {
            Username = username;
            Email = email;
            Password = password;
        }

        // משתמש שנשלף מבסיס הנתונים
        public User(int id, string username, string email, string password,
                    int roleId, string roleName, DateTime createdAt) : base(id)
        {
            Username = username;
            Email = email;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            CreatedAt = createdAt;
        }

        public override string ToString() => $"{Username} ({RoleName})";
    }
}
