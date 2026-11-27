namespace EmetGuardView.Services
{
    /// <summary>
    /// נוסף בהעשרה: Website Hosting.
    /// שירות שמעיר את שרת ה-API כשהוא ישן.
    /// בתוכנית החינמית שרת ה-API ב-Render נרדם כשלא משתמשים בו.
    /// בסיס הנתונים ב-TiDB Cloud לא צריך התעוררות: החיבור הראשון אליו מעיר אותו לבד.
    /// </summary>
    public class ServerWakeService
    {
        // החיבור לשרת ה-API, אותו חיבור שכל הדפים משתמשים בו
        private readonly HttpClient http;

        public ServerWakeService(HttpClient http)
        {
            this.http = http;
        }

        /// <summary>
        /// בודק אם שרת ה-API עונה. אם לא הגיעה תשובה בזמן שנקבע, השרת נחשב כבוי
        /// </summary>
        public async Task<bool> IsApiAwakeAsync(int timeoutSeconds)
        {
            // כל בדיקה פותחת חיבור חדש. חיבור שנפתח בזמן שהשרת ישן יכול להיתקע,
            // ואז גם הבדיקות הבאות דרכו נכשלות, אפילו אחרי שהשרת התעורר
            using HttpClient checkClient = new HttpClient();
            checkClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            try
            {
                HttpResponseMessage response = await checkClient.GetAsync(GetApiCheckUrl());
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// הכתובת המלאה של בקשת הבדיקה ל-API. הדפדפן של המשתמש שולח אליה בקשה כדי להעיר את השרת
        /// </summary>
        public string GetApiCheckUrl()
        {
            if (http.BaseAddress == null)
            {
                return string.Empty;
            }
            return new Uri(http.BaseAddress, "api/users/exists/admin").ToString();
        }
    }
}
