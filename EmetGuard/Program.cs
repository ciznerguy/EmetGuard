// נקודת הכניסה של שכבת השירות.
// נוצר בגרסה 1 מהתבנית, ובגרסה 2 נוסף אתחול בסיס הנתונים.
namespace EmetGuard
{
    public class Program
    {
        // הפעולה הראשית אסינכרונית, כדי שאפשר יהיה לחכות בתוכה לאתחול בסיס הנתונים
        public static async Task Main(string[] args)
        {
            // הכנת השרת: קריאת ההגדרות ורישום השירותים
            var builder = WebApplication.CreateBuilder(args);

            // רישום הבקרים, כך שהשרת ימצא אותם ויפנה אליהם בקשות
            builder.Services.AddControllers();

            // רישום מסמך שמתאר אוטומטית את כל נקודות הקצה של השרת
            builder.Services.AddOpenApi();

            // בניית האפליקציה מתוך ההגדרות שנרשמו
            var app = builder.Build();

            // מאיפה מגיעה מחרוזת החיבור:
            // השרת קורא את ההגדרות שלו מכמה מקורות ומאחד אותם: קובץ ההגדרות של הפרויקט, ומעליו קובץ הסודות של המשתמש.
            // את מחרוזת החיבור שמרנו בקובץ הסודות, בתוך קבוצת מחרוזות החיבור, תחת השם שמופיע כאן בין המירכאות.
            // פותחים את קובץ הסודות בלחיצה ימנית על פרויקט שכבת השירות, ובחירה בפריט של ניהול הסודות בתפריט.
            // הקובץ נשמר בתיקייה של המשתמש במחשב, מחוץ לתיקיית הפרויקט, ולכן הסיסמה לא עולה לגיט.
            // אם המחרוזת לא נמצאה, נזרקת חריגה, והשרת לא עולה.
            // המחרוזת מתארת רק את השרת, בלי שם בסיס הנתונים, כי האתחול מתחבר קודם לשרת עצמו
            string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // האתחול מסתיים לפני שהשרת מתחיל לקבל בקשות
            await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

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
            await app.RunAsync();
        }
    }
}
