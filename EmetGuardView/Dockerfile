# נוסף בגרסה 12.
# הוראות בנייה של התצוגה בשביל Render. Visual Studio לא משתמש בקובץ הזה בהרצה במחשב.
# תיקיית הבנייה היא התיקייה הראשית של הפרויקט, כי התצוגה צריכה גם את Model.

# שלב 1: בנייה. משתמשים בתמונה שיש בה את כל כלי הפיתוח של .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish EmetGuardView/EmetGuardView.csproj -c Release -o /app

# שלב 2: הרצה. משתמשים בתמונה קטנה שיש בה רק מה שצריך כדי להריץ, ומעתיקים אליה את התוצאה של שלב 1
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# השרת מאזין ליציאה 8080, ו-Render מפנה אליה את הבקשות
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EmetGuardView.dll"]
