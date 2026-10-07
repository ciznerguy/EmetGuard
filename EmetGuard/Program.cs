namespace EmetGuard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // רישום UserDB עם מחרוזת חיבור מלאה (כולל שם בסיס הנתונים), כדי שה-Controllers יקבלו אותו אוטומטית
            string dbConnectionString = (builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
                + "Database=EmetGuardDB;";
            builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

            var app = builder.Build();

            // שליפת מחרוזת החיבור מ-appsettings.json / User Secrets והפעלת אתחול ה-Database
            string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}