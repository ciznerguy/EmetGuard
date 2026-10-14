using EmetGuardView.Components;
using EmetGuardView.Services;

// נקודת הכניסה של התצוגה. נוצר בגרסה 1 מהתבנית, ובגרסה 5 נוסף רישום המחלקה שמדברת עם שכבת השירות.
namespace EmetGuardView
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // הכנת השרת של התצוגה: קריאת ההגדרות ורישום השירותים
            var builder = WebApplication.CreateBuilder(args);

            // רישום הרכיבים. הרכיבים רצים בשרת, והדף בדפדפן מתעדכן דרך חיבור קבוע ביניהם
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // הכתובת של שכבת השירות נקראת מקובץ ההגדרות של התצוגה.
            // אם היא חסרה, נזרקת חריגה, והתצוגה לא עולה
            string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                ?? throw new InvalidOperationException("ApiBaseUrl not found in appsettings.json.");

            // רישום המחלקה שמדברת עם שכבת השירות. כל דף שצריך אותה מקבל אותה אוטומטית,
            // יחד עם כלי לשליחת בקשות ברשת שכבר מכיר את הכתובת של שכבת השירות
            builder.Services.AddHttpClient<UserApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

            var app = builder.Build();

            // מחוץ לסביבת הפיתוח: שגיאה לא צפויה מעבירה לדף השגיאה, בלי לחשוף פרטים טכניים למשתמש
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // כתובת שלא קיימת מציגה את הדף "לא נמצא"
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            // הגנה מפני שליחת טפסים מאתר זר
            app.UseAntiforgery();

            // הגשת הקבצים הקבועים: עיצוב, תמונות וסמל האתר
            app.MapStaticAssets();

            // הרכיב הראשי של האתר, שממנו נטענים כל הדפים
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // הפעלת השרת של התצוגה
            app.Run();
        }
    }
}
