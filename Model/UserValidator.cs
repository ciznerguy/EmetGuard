using System.Net.Mail;

namespace Model
{
    /// <summary>
    /// נוסף בגרסה 11.
    /// מחלקה שבודקת אם הנתונים של משתמש תקינים: שם משתמש, אימייל וסיסמה.
    /// המחלקה נמצאת בשכבת Model, כי זו השכבה היחידה שכל השכבות מכירות:
    /// התצוגה מפנה אליה ישירות, וה-API מכיר אותה דרך DAL.
    /// כך הכללים כתובים פעם אחת, וגם הדף וגם השרת בודקים לפי אותם כללים.
    /// המחלקה לא ניגשת לבסיס הנתונים. בדיקה כמו "שם המשתמש כבר תפוס" נשארת ב-UserDB.
    /// </summary>
    // static: למחלקה אין נתונים משלה, היא רק מקבלת ערך ומחזירה תשובה.
    // לכן לא יוצרים ממנה עצם עם new, וקוראים לפעולות דרך שם המחלקה, כמו Math.Max
    public static class UserValidator
    {
        // הכללים במקום אחד. כדי לשנות כלל, משנים רק כאן,
        // וגם הבדיקה וגם הודעת השגיאה מתעדכנות איתו
        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 20;
        public const int PasswordMinLength = 8;

        // בדיקת שם משתמש: 3 עד 20 תווים, רק אותיות באנגלית, ספרות וקו תחתון.
        // מחזירה הודעת שגיאה, או מחרוזת ריקה אם הכול תקין
        public static string ValidateUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "יש להקליד שם משתמש.";
            }

            // מורידים רווחים מההתחלה ומהסוף, כמו שהדף עושה לפני השמירה
            string value = username.Trim();
            if (value.Length < UsernameMinLength || value.Length > UsernameMaxLength)
            {
                return $"שם המשתמש צריך להכיל בין {UsernameMinLength} ל-{UsernameMaxLength} תווים.";
            }

            // מעבר על כל התווים: מותרות רק אותיות באנגלית, ספרות וקו תחתון
            foreach (char c in value)
            {
                bool isEnglishLetter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                bool isDigit = c >= '0' && c <= '9';
                if (!isEnglishLetter && !isDigit && c != '_')
                {
                    return "שם המשתמש יכול להכיל רק אותיות באנגלית, ספרות וקו תחתון.";
                }
            }

            return "";
        }

        // בדיקת אימייל: מבנה תקין של כתובת, ונקודה אחרי ה-@.
        // מחזירה הודעת שגיאה, או מחרוזת ריקה אם הכול תקין
        public static string ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "יש להקליד אימייל.";
            }

            string value = email.Trim();

            // מחלקה מוכנה של השפה בודקת את המבנה הכללי של הכתובת.
            // אם הבדיקה מצליחה, המשתנה address מקבל את הכתובת אחרי הפירוק
            if (!MailAddress.TryCreate(value, out MailAddress? address))
            {
                return "כתובת האימייל לא תקינה.";
            }

            // MailAddress מקבלת גם טקסט כמו "Name <a@b.com>",
            // לכן בודקים שהכתובת שפורקה היא כל הטקסט שהוקלד
            if (address.Address != value)
            {
                return "כתובת האימייל לא תקינה.";
            }

            // MailAddress מקבלת גם כתובת כמו a@b, לכן בודקים שיש נקודה אחרי ה-@
            int atIndex = value.IndexOf('@');
            if (value.IndexOf('.', atIndex) == -1)
            {
                return "כתובת האימייל לא תקינה.";
            }

            return "";
        }

        // בדיקת סיסמה: לפחות 8 תווים, ולפחות אות אחת וספרה אחת.
        // מחזירה הודעת שגיאה, או מחרוזת ריקה אם הכול תקין
        public static string ValidatePassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "יש להקליד סיסמה.";
            }

            if (password.Length < PasswordMinLength)
            {
                return $"הסיסמה צריכה להכיל לפחות {PasswordMinLength} תווים.";
            }

            // מעבר על כל התווים: מחפשים לפחות אות אחת וספרה אחת
            bool hasLetter = false;
            bool hasDigit = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c))
                {
                    hasLetter = true;
                }
                if (char.IsDigit(c))
                {
                    hasDigit = true;
                }
            }

            if (!hasLetter || !hasDigit)
            {
                return "הסיסמה צריכה להכיל לפחות אות אחת וספרה אחת.";
            }

            return "";
        }

        // בדיקת משתמש שלם. מחזירה רשימה של כל השגיאות, ורשימה ריקה אם הכול תקין.
        // רשימה ולא הודעה אחת: מי ששולח בקשה ל-API מקבל את כל הבעיות בבת אחת
        public static List<string> Validate(User user)
        {
            List<string> errors = new List<string>();

            string error = ValidateUsername(user.Username);
            if (error != "")
            {
                errors.Add(error);
            }

            error = ValidateEmail(user.Email);
            if (error != "")
            {
                errors.Add(error);
            }

            error = ValidatePassword(user.Password);
            if (error != "")
            {
                errors.Add(error);
            }

            return errors;
        }
    }
}
