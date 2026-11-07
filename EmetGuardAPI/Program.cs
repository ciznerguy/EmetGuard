// נקודת הכניסה של שכבת השירות.
// נוצר בגרסה 1 מהתבנית, בגרסה 2 נוסף אתחול בסיס הנתונים, ובגרסה 3 נוסף רישום מחלקת הגישה למשתמשים.
// בהעשרה Remote DB נוספה בחירה בין בסיס הנתונים המקומי לבסיס הנתונים בענן.
namespace EmetGuardAPI
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

            // מאיפה מגיעה מחרוזת החיבור:
            // השרת קורא את ההגדרות שלו מכמה מקורות ומאחד אותם: קובץ ההגדרות של הפרויקט, ומעליו קובץ הסודות של המשתמש.
            // את מחרוזת החיבור שמרנו בקובץ הסודות, בתוך קבוצת מחרוזות החיבור, תחת השם שמופיע כאן בין המירכאות.
            // פותחים את קובץ הסודות בלחיצה ימנית על פרויקט שכבת השירות, ובחירה בפריט של ניהול הסודות בתפריט.
            // הקובץ נשמר בתיקייה של המשתמש במחשב, מחוץ לתיקיית הפרויקט, ולכן הסיסמה לא עולה לגיט.
            // אם המחרוזת לא נמצאה, נזרקת חריגה, והשרת לא עולה.
            //
            // נוסף בהעשרה Remote DB: בחירת השרת של בסיס הנתונים.
            // בקובץ הסודות שמורות שתי מחרוזות חיבור: DefaultConnection לשרת המקומי, ו-AivenConnection לשרת בענן.
            // כדי לעבור לשרת האחר, משנים את השם שבין המירכאות ומריצים את השרת מחדש
            string connectionName = "DefaultConnection";
            Console.WriteLine($"[DB] Using connection string: {connectionName}");

            // נוסף בהעשרה Remote DB: קריאת מחרוזת החיבור פעם אחת בלבד, לפי השם שנבחר.
            // המחרוזת מכילה רק את פרטי השרת, בלי שם בסיס הנתונים. אם היא לא נמצאה בקובץ הסודות, נזרקת חריגה והשרת לא עולה
            string? serverConnectionString = builder.Configuration.GetConnectionString(connectionName);
            if (serverConnectionString == null)
            {
                throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
            }

            // מחרוזת חיבור מלאה: פרטי השרת ועוד שם בסיס הנתונים
            string dbConnectionString = serverConnectionString + "Database=EmetGuardDB;";

            // רישום מחלקת הגישה למשתמשים עם מחרוזת החיבור המלאה, כדי שהבקרים יקבלו אותה אוטומטית.
            // נוצר עצם אחד בלבד, וכל הבקשות משתמשות בו, כי אין בו נתונים שמשתנים
            builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

            // בניית האפליקציה מתוך ההגדרות שנרשמו
            var app = builder.Build();

            // האתחול מסתיים לפני שהשרת מתחיל לקבל בקשות.
            // נוסף בגרסה 10: אם החיבור לבסיס הנתונים נכשל (למשל סיסמה שגויה בקובץ הסודות, או ששרת MySQL כבוי),
            // מודפסת הודעה ברורה עם סיבת השגיאה, והחריגה נזרקת שוב כדי שהשרת לא יעלה בלי בסיס נתונים
            try
            {
                await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Database connection failed: {ex.Message}");
                throw;
            }

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
