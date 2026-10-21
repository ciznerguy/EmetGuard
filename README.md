<div dir="rtl">

# EmetGuard: פרויקט דוגמה

פרויקט זה הוא פרויקט דוגמה שמטרתו להראות, שלב אחר שלב, איך בונים פרויקט גמר בהנדסת תוכנה בארבע שכבות: Model, DAL, API ו-View, עם Blazor Server, ASP.NET Core Web API ו-MySQL.

## גרסאות ומדריכים

הפרויקט נבנה בשלבים. כל גרסה מוסיפה שלב אחד, ולכל גרסה מצורף מדריך שמסביר מה נעשה בה.

| גרסה | מה נוסף | קוד | מדריך |
|---|---|---|---|
| 1 | מבנה ראשוני של ארבע שכבות | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v1) | [מדריך יצירת פרויקט ראשוני](https://github.com/ciznerguy/EmetGuard/releases/download/v1/v1-initial-project-guide.pdf) |
| 2 | יצירה של בסיס הנתונים והטבלאות | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v2) | [מדריך חיבור לבסיס הנתונים](https://github.com/ciznerguy/EmetGuard/releases/download/v2/v2-database-connection-guide.pdf) |
| 3 | מחלקות Model, שאילתות משתמשים ב-DAL ו-endpoints ב-API | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v3) | [מדריך גישה לטבלת המשתמשים](https://github.com/ciznerguy/EmetGuard/releases/download/v3/v3-users-table-guide.pdf) |
| 4 | דף בית, דף הרשמה ודף כניסה בעיצוב Bootstrap | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v4) | [מדריך דף בית, הרשמה וכניסה](https://github.com/ciznerguy/EmetGuard/releases/download/v4/v4-pages-design-guide.pdf) |
| 5 | הפעלת דף ההרשמה: בדיקה שהמשתמש לא קיים ושמירה בבסיס הנתונים | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v5) | [מדריך הפעלת דף ההרשמה](https://github.com/ciznerguy/EmetGuard/releases/download/v5/v5-register-logic-guide.pdf) |
| 6 | כניסה למערכת, ושירות שזוכר את המשתמש המחובר בכל הדפים | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v6) | [מדריך כניסה ושירות משתמש](https://github.com/ciznerguy/EmetGuard/releases/download/v6/v6-login-session-guide.pdf) |
| 7 | גיבוב סיסמאות עם BCrypt, ומשתמשים התחלתיים | [לעיון בקוד](https://github.com/ciznerguy/EmetGuard/tree/v7) | [מדריך גיבוב סיסמאות](https://github.com/ciznerguy/EmetGuard/releases/download/v7/v7-password-hashing-guide.pdf) |

אפשר גם להוריד כל גרסה כקובץ zip מדף ה-[Releases](https://github.com/ciznerguy/EmetGuard/releases).

## נספחים

הסברים מורחבים על נושאים שפגשנו בדרך. ליד כל נספח מצוין באיזו גרסה פגשנו את הנושא לראשונה.

| נספח | פגשנו בגרסה |
|---|---|
| [שאלות להבנה וחזרה](https://github.com/ciznerguy/EmetGuard/releases/download/v7/review-questions.pdf) | כל הגרסאות |
| [גיבוי הפרויקט ב-Git וב-GitHub](https://github.com/ciznerguy/EmetGuard/releases/download/v1/appendix-git-backup.pdf) | 1 |
| [סרטון: איך עובדים עם הגיט של EmetGuard](#סרטון-איך-עובדים-עם-הגיט-של-emetguard) | 1 |
| [המחלקה DatabaseInitializer: אתחול בסיס הנתונים](https://github.com/ciznerguy/EmetGuard/releases/download/v2/v2-appendix-database-initializer.pdf) | 2 |
| [למה צריך מחלקת בסיס (BaseEntity)](https://github.com/ciznerguy/EmetGuard/releases/download/v3/v3-users-table-guide.pdf) | 3 |
| [תכונות ב-C#: get ו-set, ולמה לא ויתרנו על הכימוס](https://github.com/ciznerguy/EmetGuard/releases/download/v3/v3-appendix-properties.pdf) | 3 |
| [Bootstrap לעומק, עם דוגמאות מהאתר](https://github.com/ciznerguy/EmetGuard/releases/download/v4/v4-pages-design-guide.pdf) | 4 |
| [פביקון: מה זה, איך יוצרים ואיך מעלים לאתר](https://github.com/ciznerguy/EmetGuard/releases/download/v4/v4-appendix-favicon.pdf) | 4 |

### סרטון: איך עובדים עם הגיט של EmetGuard

סרטון קצר, בלי קול: הדף הראשי, מעבר בין הגרסאות, המדריך של כל גרסה והנספחים.

https://github.com/user-attachments/assets/e67dbaf8-90ac-417f-8dd4-0d96cc923746

</div>
