using System.Data;
using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// נוסף בגרסה 2.
    /// מחלקה שאחראית להכין את בסיס הנתונים לפני שהמערכת מתחילה לעבוד.
    /// היא נקראת פעם אחת, כשהשרת עולה, ויוצרת את בסיס הנתונים, את הטבלאות ואת נתוני הפתיחה.
    /// אפשר להריץ אותה שוב ושוב בלי נזק: בכל שלב נוצר רק מה שעוד לא קיים.
    /// המחלקה סטטית, כי אין לה מצב משלה ואין צורך ליצור ממנה עצמים.
    /// </summary>
    public static class DatabaseInitializer
    {
        // שם בסיס הנתונים מוגדר במקום אחד בלבד, כדי שכל הפעולות ישתמשו באותו שם
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

            // שלב ג': הכנסת נתוני ברירת מחדל לטבלת התפקידים
            await SeedRolesAsync(dbConnectionString);
        }

        /// <summary>
        /// יוצרת את בסיס הנתונים עצמו, אם הוא עוד לא קיים.
        /// </summary>
        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            // הקידוד שנבחר תומך בכל השפות, כולל עברית וסמלים מיוחדים,
            // וכלל המיון שנבחר משווה טקסט בלי להבדיל בין אותיות גדולות לקטנות
            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            // מתחברים לשרת בלי לציין בסיס נתונים, כי בשלב הזה הוא אולי עוד לא קיים.
            // החיבור והפקודה נסגרים אוטומטית בסוף הפעולה, גם אם נזרקה חריגה
            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            // הרצת פקודה שלא מחזירה נתונים, אלא רק משנה את מבנה השרת
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// יוצרת את שבע הטבלאות של המערכת, אם הן עוד לא קיימות.
        /// </summary>
        private static async Task CreateTablesAsync(string dbConnectionString)
        {
            // שאילתה אחת שמגדירה את מבנה שבע הטבלאות: מפתחות ראשיים, מפתחות זרים וערכי ברירת מחדל.
            // סדר היצירה חשוב: טבלה שמפנה לטבלה אחרת חייבת להיווצר אחריה.
            //
            // הטבלאות לפי הסדר:
            // 1. טבלת התפקידים: מנהל, מורה ותלמיד.
            // 2. טבלת המשתמשים: כל משתמש שייך לתפקיד אחד. שם המשתמש והדואר האלקטרוני ייחודיים.
            //    משתמש לא נמחק אלא מסומן כלא פעיל.
            // 3. טבלת הקטגוריות: נושאי הידיעות.
            // 4. טבלת השיעורים: כל שיעור מציג ידיעה אחת, שייך למורה אחד ולקטגוריה אחת.
            //    בשיעור נשמר אם ההצבעה פתוחה ומה המסקנה הסופית.
            // 5. טבלת הראיות: ראיות שתלמידים מגישים בעד הידיעה או נגדה, עם סטטוס בדיקה.
            // 6. טבלת ההצבעות: כל הצבעה שייכת לשיעור. מצביע יכול להיות משתמש רשום או אורח.
            // 7. טבלת הנוכחות בשיעור: טבלה מקשרת בין שיעורים למשתמשים (קשר של רבים לרבים).
            //    המפתח הראשי שלה מורכב משני שדות, כך שמשתמש לא יכול להצטרף לאותו שיעור פעמיים.
            //
            // מה קורה למפתח זר כשמוחקים את השורה שהוא מפנה אליה:
            // - מניעת מחיקה: אי אפשר למחוק תפקיד, קטגוריה או מורה שיש שורות שתלויות בהם.
            // - מחיקה מדורגת: מחיקת שיעור מוחקת איתו את הראיות, ההצבעות והנוכחות שלו.
            // - איפוס לריק: מחיקת משתמש משאירה את ההצבעה שלו, בלי לדעת מי הצביע.
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
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
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

            // כאן כבר מתחברים לבסיס הנתונים עצמו, ומריצים את כל פקודות היצירה בבת אחת
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// מכניסה לטבלת התפקידים את שלושת התפקידים הקבועים של המערכת.
        /// </summary>
        private static async Task SeedRolesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם הטבלה ריקה לפני הכנסת נתוני ברירת המחדל
            string checkSql = "SELECT COUNT(*) FROM Roles;";
            using var checkCmd = new MySqlCommand(checkSql, connection);

            // הפקודה מחזירה ערך בודד, מספר השורות, ומשם ממירים אותו למספר שלם
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            // מכניסים את התפקידים רק בהפעלה הראשונה.
            // בלי הבדיקה, כל עלייה של השרת הייתה מנסה להכניס אותם שוב
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
