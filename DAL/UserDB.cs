using System.Data.Common;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// נוסף בגרסה 3.
    /// כל השאילתות מול טבלת המשתמשים נמצאות כאן, ורק כאן.
    /// שכבת השירות לא כותבת שאילתות בעצמה: היא קוראת לפעולות של המחלקה הזו.
    /// כל פעולה פותחת חיבור משלה, מריצה שאילתה אחת וסוגרת את החיבור.
    /// כל הערכים שמגיעים מהמשתמש מועברים כפרמטרים, ולא מודבקים לתוך טקסט השאילתה.
    /// כך המשתמש לא יכול להחדיר פקודות משלו לבסיס הנתונים.
    /// </summary>
    public class UserDB
    {
        // מחרוזת החיבור לבסיס הנתונים. מתקבלת פעם אחת בבנאי ולא משתנה
        private readonly string connectionString;

        // שליפה משותפת לכל הפעולות שמחזירות משתמש:
        // טבלת המשתמשים מצורפת לטבלת התפקידים, כדי לקבל גם את שם התפקיד ולא רק את המזהה שלו
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
        /// הוספת משתמש חדש. מזהה התפקיד נמצא לפי שם התפקיד
        /// </summary>
        /// <returns>המזהה שבסיס הנתונים נתן למשתמש החדש</returns>
        public async Task<int> AddUserAsync(User user, string roleName = "Student")
        {
            // ההוספה והחיפוש של מזהה התפקיד נעשים בשאילתה אחת:
            // הערכים של המשתמש נלקחים מהפרמטרים, ומזהה התפקיד נלקח מטבלת התפקידים
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

            // הפקודה מחזירה את מספר השורות שנוספו
            int rows = await command.ExecuteNonQueryAsync();

            // אם התפקיד לא קיים בטבלת התפקידים, לא נוספה אף שורה
            if (rows == 0)
                throw new InvalidOperationException($"Role '{roleName}' not found.");

            // המזהה שבסיס הנתונים יצר לשורה החדשה נשמר בתוך העצם ומוחזר
            user.Id = (int)command.LastInsertedId;
            return user.Id;
        }

        /// <summary>
        /// בדיקה אם שם משתמש כבר תפוס
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            // השאילתה מחזירה 1 אם נמצאה לפחות שורה אחת, ו-0 אם לא
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
        /// <returns>המשתמש אם השם והסיסמה נכונים, אחרת ערך ריק</returns>
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            string sql = SelectUserSql + " WHERE u.Username = @Username;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            // קורא התוצאות עובר על השורות שחזרו, שורה אחרי שורה.
            // אם אין אף שורה, שם המשתמש לא קיים
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            User user = ReadUser(reader);

            // ההשוואה נעשית בקוד ולא בשאילתה, כי בסיס הנתונים משווה מחרוזות בלי הבדל בין אותיות גדולות לקטנות
            return user.Password == password ? user : null;
        }

        /// <summary>
        /// שליפת משתמש לפי מזהה
        /// </summary>
        /// <returns>המשתמש, או ערך ריק אם לא נמצא</returns>
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

            // כל שורה שחוזרת הופכת לעצם משתמש ונוספת לרשימה
            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(ReadUser(reader));

            return users;
        }

        /// <summary>
        /// ספירת משתמשים לכל תפקיד.
        /// הצירוף נעשה מצד טבלת התפקידים, כך שגם תפקיד בלי משתמשים יופיע, עם 0
        /// </summary>
        /// <returns>מילון: שם התפקיד ומספר המשתמשים בו</returns>
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            // קיבוץ לפי תפקיד, וספירה של המשתמשים בכל קבוצה
            string sql = @"
                SELECT r.RoleName, COUNT(u.UserID) AS UserCount
                FROM Roles r
                LEFT JOIN Users u ON u.RoleID = r.RoleID
                GROUP BY r.RoleID, r.RoleName
                ORDER BY r.RoleID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            // המפתח במילון הוא שם התפקיד, והערך הוא מספר המשתמשים
            var counts = new Dictionary<string, int>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                counts[reader.GetString(reader.GetOrdinal("RoleName"))] = Convert.ToInt32(reader["UserCount"]);

            return counts;
        }

        /// <summary>
        /// שינוי תפקיד של משתמש, לפי שם התפקיד החדש
        /// </summary>
        /// <returns>אמת אם המשתמש והתפקיד נמצאו</returns>
        public async Task<bool> UpdateUserRoleAsync(int userId, string roleName)
        {
            // עדכון עם צירוף: מזהה התפקיד החדש נלקח מטבלת התפקידים לפי השם שלו
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

            // אם לא עודכנה אף שורה, המשתמש או התפקיד לא נמצאו
            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// עדכון סיסמה
        /// </summary>
        /// <returns>אמת אם המשתמש נמצא</returns>
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
        /// עדכון אימייל. אם האימייל כבר שייך למשתמש אחר, בסיס הנתונים יזרוק שגיאה, כי העמודה מוגדרת ייחודית
        /// </summary>
        /// <returns>אמת אם המשתמש נמצא</returns>
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
        /// מחיקת משתמש. מורה שיצר שיעורים לא יימחק, כי טבלת השיעורים מוגדרת עם מניעת מחיקה
        /// </summary>
        /// <returns>אמת אם המשתמש נמחק</returns>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            string sql = "DELETE FROM Users WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // הופך שורה מהשליפה המשותפת לעצם משתמש.
        // הפעולה משותפת לכל השליפות, כדי שקריאת העמודות תיכתב פעם אחת בלבד
        private static User ReadUser(DbDataReader reader)
        {
            // מועד ההרשמה יכול להיות ריק, ולכן בודקים אותו לפני הקריאה
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
