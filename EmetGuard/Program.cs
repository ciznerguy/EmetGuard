// נקודת הכניסה של שכבת השירות. נוצר בגרסה 1 מהתבנית.
namespace EmetGuard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // הכנת השרת: קריאת ההגדרות ורישום השירותים
            var builder = WebApplication.CreateBuilder(args);

            // רישום הבקרים, כך שהשרת ימצא אותם ויפנה אליהם בקשות
            builder.Services.AddControllers();

            // רישום מסמך שמתאר אוטומטית את כל נקודות הקצה של השרת
            builder.Services.AddOpenApi();

            // בניית האפליקציה מתוך ההגדרות שנרשמו
            var app = builder.Build();

            // בסביבת הפיתוח בלבד: חשיפת מסמך התיאור של נקודות הקצה
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // בדיקת הרשאות. בשלב הזה של הפרויקט עוד אין דפים מוגבלים
            app.UseAuthorization();

            // חיבור כל כתובת לפעולה המתאימה בבקר, לפי השורות שבסוגריים מרובעים מעל הפעולות
            app.MapControllers();

            // הפעלת השרת. מכאן הוא מקבל בקשות עד שעוצרים אותו
            app.Run();
        }
    }
}
