using System.Data;
using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "EmetGuardDB";

        /// <summary>
        /// פעולת האתחול הראשית - נקראת בעת עליית האפליקציה
        /// </summary>
        /// <param name="serverConnectionString">מחרוזת חיבור לשרת (ללא ציון בסיס הנתונים)</param>
        public static async Task InitializeAsync(string serverConnectionString)
        {
            // שלב א': יצירת בסיס הנתונים אם אינו קיים
            await CreateDatabaseAsync(serverConnectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים
            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            // שלב ב': יצירת הטבלאות
            await CreateTablesAsync(dbConnectionString);

            // שלב ג': הכנסת נתוני ברירת מחדל לטבלת התפקידים (Roles)
            await SeedRolesAsync(dbConnectionString);
        }

        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task CreateTablesAsync(string dbConnectionString)
        {
            // שאילתת DDL המגדירה את 7 הטבלאות, מפתחות ראשיים, זרים וערכי ברירת מחדל
            string createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleID INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(30) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Users (
                    UserID INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL UNIQUE,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(255) NOT NULL,
                    RoleID INT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Categories (
                    CategoryID INT AUTO_INCREMENT PRIMARY KEY,
                    CategoryName VARCHAR(50) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Lessons (
                    LessonID INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(150) NOT NULL,
                    NewsContent TEXT NOT NULL,
                    MediaUrl VARCHAR(255),
                    TeacherID INT NOT NULL,
                    CategoryID INT NOT NULL,
                    IsVotingOpen BOOLEAN DEFAULT TRUE,
                    FinalConclusion VARCHAR(30),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (TeacherID) REFERENCES Users(UserID) ON DELETE RESTRICT ON UPDATE CASCADE,
                    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Evidences (
                    EvidenceID INT AUTO_INCREMENT PRIMARY KEY,
                    LessonID INT NOT NULL,
                    SubmittedByUserID INT NOT NULL,
                    EvidenceText TEXT NOT NULL,
                    SourceUrl VARCHAR(255),
                    IsSupporting BOOLEAN NOT NULL,
                    Status VARCHAR(20) DEFAULT 'Pending',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (SubmittedByUserID) REFERENCES Users(UserID) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Votes (
                    VoteID INT AUTO_INCREMENT PRIMARY KEY,
                    LessonID INT NOT NULL,
                    UserID INT NULL,
                    GuestSessionID VARCHAR(100),
                    VoteChoice VARCHAR(10) NOT NULL,
                    VotedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE SET NULL ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS LessonAttendees (
                    LessonID INT NOT NULL,
                    UserID INT NOT NULL,
                    JoinedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (LessonID, UserID),
                    FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;
            ";

            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task SeedRolesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם הטבלה ריקה לפני הכנסת נתוני ברירת המחדל
            string checkSql = "SELECT COUNT(*) FROM Roles;";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                string insertRolesSql = @"
                    INSERT INTO Roles (RoleName) VALUES 
                    ('Admin'),
                    ('Teacher'),
                    ('Student');
                ";
                using var insertCmd = new MySqlCommand(insertRolesSql, connection);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}