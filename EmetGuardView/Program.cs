using EmetGuardView.Components;

// נקודת הכניסה של התצוגה. נוצר בגרסה 1 מהתבנית.
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
