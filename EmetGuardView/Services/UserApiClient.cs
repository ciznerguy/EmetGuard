using System.Net.Http.Json;
using Model;

namespace EmetGuardView.Services
{
    /// <summary>
    /// נוסף בגרסה 5.
    /// המחלקה שדרכה התצוגה מדברת עם שכבת השירות בנושא משתמשים.
    /// התצוגה לא ניגשת לבסיס הנתונים בעצמה: היא שולחת בקשות ברשת לשכבת השירות, ומקבלת תשובות.
    /// הכתובת של שכבת השירות נקבעת בקובץ ההגדרות של התצוגה, ונמסרת למחלקה בזמן הרישום בקובץ ההפעלה.
    /// </summary>
    public class UserApiClient
    {
        // הכלי ששולח בקשות ברשת. מגיע מוכן, עם הכתובת של שכבת השירות
        private readonly HttpClient http;

        public UserApiClient(HttpClient http)
        {
            this.http = http;
        }

        /// <summary>
        /// קריאה: האם שם המשתמש כבר תפוס
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            // הערך מקודד לפני שהוא נכנס לכתובת, כדי שתווים מיוחדים לא ישברו אותה
            return await http.GetFromJsonAsync<bool>($"api/users/exists/{Uri.EscapeDataString(username)}");
        }

        /// <summary>
        /// קריאה: האם האימייל כבר תפוס
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await http.GetFromJsonAsync<bool>($"api/users/email-exists/{Uri.EscapeDataString(email)}");
        }

        /// <summary>
        /// כתיבה: הוספת משתמש חדש לבסיס הנתונים
        /// </summary>
        /// <returns>אמת אם שכבת השירות הודיעה שהמשתמש נוצר</returns>
        public async Task<bool> AddUserAsync(User user)
        {
            // העצם מומר לפורמט של נתונים ברשת ונשלח בגוף הבקשה
            HttpResponseMessage response = await http.PostAsJsonAsync("api/users", user);

            // קוד מצב בטווח ההצלחה אומר שהמשתמש נשמר
            return response.IsSuccessStatusCode;
        }
    }
}
