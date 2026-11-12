using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EmetGuardView.Services
{
    /// <summary>
    /// נוסף בהעשרה: Website Hosting.
    /// שירות שמעיר את שרת ה-API ואת בסיס הנתונים כשהם כבויים.
    /// בתוכנית החינמית שרת ה-API ב-Render נרדם כשלא משתמשים בו,
    /// ובסיס הנתונים ב-Aiven נכבה אחרי כמה ימים בלי שימוש.
    /// </summary>
    public class ServerWakeService
    {
        // חיבור נפרד לשרתים של Aiven. הוא משותף לכל המשתמשים באתר
        private static readonly HttpClient aivenClient = new HttpClient();

        // החיבור לשרת ה-API, אותו חיבור שכל הדפים משתמשים בו
        private readonly HttpClient http;

        // הכתובת של בסיס הנתונים ב-Aiven, מקובץ ההגדרות
        private readonly string? aivenServiceUrl;

        // המפתח לניהול Aiven. הוא סודי, ולכן נשמר רק במשתני הסביבה של השרת ולא בקוד
        private readonly string? aivenToken;

        public ServerWakeService(HttpClient http, IConfiguration configuration)
        {
            this.http = http;
            aivenServiceUrl = configuration["AivenServiceUrl"];
            aivenToken = configuration["Aiven:AuthToken"];
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

        /// <summary>
        /// האם אפשר להדליק את בסיס הנתונים: רק אם הוגדרו הכתובת והמפתח של Aiven
        /// </summary>
        public bool CanWakeDatabase()
        {
            if (string.IsNullOrEmpty(aivenServiceUrl) || string.IsNullOrEmpty(aivenToken))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// מחזיר את המצב של בסיס הנתונים ב-Aiven, למשל RUNNING או POWEROFF
        /// </summary>
        public async Task<string> GetDatabaseStateAsync()
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, aivenServiceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("aivenv1", aivenToken);

            HttpResponseMessage response = await aivenClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // התשובה היא JSON. המצב נמצא בתוך service, בשדה state
            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(json);
            string? state = document.RootElement.GetProperty("service").GetProperty("state").GetString();
            if (state == null)
            {
                return string.Empty;
            }
            return state;
        }

        /// <summary>
        /// שולח ל-Aiven בקשה להדליק את בסיס הנתונים
        /// </summary>
        public async Task PowerOnDatabaseAsync()
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, aivenServiceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("aivenv1", aivenToken);
            request.Content = new StringContent("{\"powered\":true}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await aivenClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Console.WriteLine("[Wake] Power on request sent to the database");
        }
    }
}
