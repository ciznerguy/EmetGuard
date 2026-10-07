using System.Data.Common;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// כל השאילתות מול טבלת Users
    /// </summary>
    public class UserDB
    {
        private readonly string connectionString;

        // שליפה משותפת לכל הפעולות שמחזירות משתמש: Users יחד עם שם התפקיד מטבלת Roles
        private const string SelectUserSql = @"
            SELECT u.UserID, u.Username, u.Email, u.PasswordHash, u.RoleID, r.RoleName, u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON u.RoleID = r.RoleID";

        /// <param name="connectionString">מחרוזת חיבור מלאה, כולל שם בסיס הנתונים</param>
        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// הוספת משתמש חדש. ה-RoleID נמצא לפי שם התפקיד
        /// </summary>
        /// <returns>המזהה (UserID) שבסיס הנתונים נתן למשתמש החדש</returns>
        public async Task<int> AddUserAsync(User user, string roleName = "Student")
        {
            string sql = @"
                INSERT INTO Users (Username, Email, PasswordHash, RoleID)
                SELECT @Username, @Email, @Password, RoleID
                FROM Roles
                WHERE RoleName = @RoleName;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@RoleName", roleName);

            int rows = await command.ExecuteNonQueryAsync();

            // אם התפקיד לא קיים בטבלת Roles, לא נוספה אף שורה
            if (rows == 0)
                throw new InvalidOperationException($"Role '{roleName}' not found.");

            user.Id = (int)command.LastInsertedId;
            return user.Id;
        }

        /// <summary>
        /// בדיקה אם שם משתמש כבר תפוס
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            string sql = "SELECT EXISTS(SELECT 1 FROM Users WHERE Username = @Username);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        /// <summary>
        /// בדיקה אם אימייל כבר תפוס
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            string sql = "SELECT EXISTS(SELECT 1 FROM Users WHERE Email = @Email);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        /// <summary>
        /// בדיקת התחברות
        /// </summary>
        /// <returns>המשתמש אם השם והסיסמה נכונים, אחרת null</returns>
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            string sql = SelectUserSql + " WHERE u.Username = @Username;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            User user = ReadUser(reader);

            // ההשוואה נעשית ב-C# ולא ב-SQL, כי MySQL משווה מחרוזות בלי הבדל בין אותיות גדולות לקטנות
            return user.Password == password ? user : null;
        }

        /// <summary>
        /// שליפת משתמש לפי מזהה
        /// </summary>
        /// <returns>המשתמש, או null אם לא נמצא</returns>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            string sql = SelectUserSql + " WHERE u.UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserID", userId);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadUser(reader) : null;
        }

        /// <summary>
        /// שליפת כל המשתמשים עם שם התפקיד, ממוינים לפי שם משתמש
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            string sql = SelectUserSql + " ORDER BY u.Username;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(ReadUser(reader));

            return users;
        }

        /// <summary>
        /// ספירת משתמשים לכל תפקיד. LEFT JOIN כדי שגם תפקיד בלי משתמשים יופיע עם 0
        /// </summary>
        /// <returns>מילון: שם התפקיד ומספר המשתמשים בו</returns>
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            string sql = @"
                SELECT r.RoleName, COUNT(u.UserID) AS UserCount
                FROM Roles r
                LEFT JOIN Users u ON u.RoleID = r.RoleID
                GROUP BY r.RoleID, r.RoleName
                ORDER BY r.RoleID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            var counts = new Dictionary<string, int>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                counts[reader.GetString(reader.GetOrdinal("RoleName"))] = Convert.ToInt32(reader["UserCount"]);

            return counts;
        }

        /// <summary>
        /// שינוי תפקיד של משתמש, לפי שם התפקיד החדש
        /// </summary>
        /// <returns>true אם המשתמש והתפקיד נמצאו</returns>
        public async Task<bool> UpdateUserRoleAsync(int userId, string roleName)
        {
            string sql = @"
                UPDATE Users u
                INNER JOIN Roles r ON r.RoleName = @RoleName
                SET u.RoleID = r.RoleID
                WHERE u.UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RoleName", roleName);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// עדכון סיסמה
        /// </summary>
        /// <returns>true אם המשתמש נמצא</returns>
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            string sql = "UPDATE Users SET PasswordHash = @Password WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Password", newPassword);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// עדכון אימייל. אם האימייל כבר שייך למשתמש אחר, MySQL יזרוק שגיאה כי העמודה מוגדרת UNIQUE
        /// </summary>
        /// <returns>true אם המשתמש נמצא</returns>
        public async Task<bool> UpdateEmailAsync(int userId, string newEmail)
        {
            string sql = "UPDATE Users SET Email = @Email WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", newEmail);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// מחיקת משתמש. מורה שיצר שיעורים לא יימחק, כי Lessons מוגדר עם ON DELETE RESTRICT
        /// </summary>
        /// <returns>true אם המשתמש נמחק</returns>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            string sql = "DELETE FROM Users WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // הופך שורה מהשליפה המשותפת לאובייקט User
        private static User ReadUser(DbDataReader reader)
        {
            int createdAtIndex = reader.GetOrdinal("CreatedAt");

            return new User(
                reader.GetInt32(reader.GetOrdinal("UserID")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetString(reader.GetOrdinal("PasswordHash")),
                reader.GetInt32(reader.GetOrdinal("RoleID")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.IsDBNull(createdAtIndex) ? DateTime.MinValue : reader.GetDateTime(createdAtIndex));
        }
    }
}
