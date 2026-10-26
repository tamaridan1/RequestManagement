# מערכת ניהול פניות (Request Management)

מערכת לניהול פניות/בקשות מארגונים – שליפה, סינון, עדכון סטטוס, היסטוריה ונתונים מסכמים.

## טכנולוגיות

- **Backend:** .NET 8, ASP.NET Core Web API, Entity Framework Core 8
- **מסד נתונים:** SQL Server (LocalDB)
- **Frontend:** Angular 21 (standalone components, signals, RxJS)
- **בדיקות:** xUnit
- **ולידציה:** FluentValidation

## מבנה הפתרון

```
RequestManagement/              (Backend)
├── RequestManagementAPI    – שכבת API (Controllers, Middleware, Program)
├── RequestManagementBL     – לוגיקה עסקית (Services, Validators, Exceptions)
├── RequestManagementDAL    – גישה לנתונים (DbContext, Repository, Entity, Seed)
├── RequestManagementDTO    – אובייקטים משותפים (DTOs, Enums)
└── RequestManagement.Tests – בדיקות אוטומטיות

request-management-client/      (Frontend - Angular)
└── src/app
    ├── models    – טיפוסים
    ├── services  – קריאות API
    └── components – רשימה, summary, היסטוריה
```

זרימת תלויות: `API → BL → DAL → DTO`.

## הוראות הרצה

### Backend
```bash
cd RequestManagementAPI
dotnet run
```
- ה-API עולה על `http://localhost:5066`.
- בהרצה ראשונה נזרעים אוטומטית 100,000 רשומות (אם ה-DB ריק).
- Swagger: `http://localhost:5066/swagger`.

**יצירת נתונים מחדש:** `POST /api/requests/reseed` (זמין בסביבת Development בלבד).

### Frontend
```bash
cd request-management-client
npm install
npm start
```
- האפליקציה עולה על `http://localhost:4200`.

## מסד הנתונים והאינדקסים

**ישות Request:** Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion.

**טבלת Audit:** RequestStatusHistory (RequestId, PreviousStatus, NewStatus, ChangedAt, ChangedBy).

**אינדקסים:**
- מורכב `(Status, Priority, CreatedAt)` – לסינון ומיון נפוצים.
- `OrganizationName`, `AssignedTo`, `CreatedAt` – לסינון.
- `RequestId` בטבלת ההיסטוריה – לשליפה מהירה.

כל הסינון/מיון/דפדוף רץ ב-SQL (IQueryable), ללא טעינת כל הנתונים 

## Concurrency (עדכונים מתחרים)

שימוש ב-**Optimistic Concurrency** עם עמודת `RowVersion` (rowversion של SQL Server). הלקוח מקבל את ה-RowVersion בשליפה ומחזיר אותו בעדכון. אם מישהו שינה בינתיים – ה-UPDATE פוגע ב-0 שורות, EF זורק `DbUpdateConcurrencyException`, והמערכת מחזירה **409 Conflict**. כך נמנעת דריסה שקטה (Lost Update). האכיפה היא ברמת ה-DB, לא רק ב-UI.

## Bulk Update

`POST /api/requests/bulk-status` – עדכון עד 100 פניות בבקשה אחת.

נבחרה גישת **Partial Success:** כל פנייה מטופלת עצמאית. התשובה מפרטת כמה הצליחו, כמה נכשלו, והסיבה לכל כישלון (לא קיימת / concurrency / מעבר אסור). נימוק: כשל של פנייה אחת לא צריך להפיל עדכון של 99 תקינות.

## Cache

ה-summary (נתונים מסכמים) נשמר ב-**IMemoryCache** עם תפוגה (Expiration) של 2 דקות.

- **Invalidation:** ה-cache מתנקה אוטומטית בכל עדכון סטטוס, כדי למנוע מידע מיושן.
- **מניעת Cache Stampede:** נעילה (SemaphoreSlim) מבטיחה שרק חישוב אחד ירוץ גם אם כמה בקשות מגיעות יחד.
- **ריבוי מופעי שרת:** למערכת multi-server היינו עוברים ל-Redis (IDistributedCache) כדי שה-cache יהיה משותף בין המופעים.

## שתי החלטות טכנולוגיות משמעותיות

1. **SQL Server + EF Core** (ולא MongoDB) – כי RowVersion ל-concurrency, אינדקסים ו-query plans נתמכים בצורה טבעית.
2. **Middleware מרכזי לטיפול בשגיאות** (ולא try/catch בכל controller) – טיפול אחיד, קוד נקי, ו-ProblemDetails תקני.

## מגבלה ידועה ושיפור אפשרי

**מגבלה:** החיפוש הטקסטואלי משתמש ב-`LIKE '%term%'`, שלא נתמך ביעילות ע"י אינדקס וגורם לסריקה על 100K.

**שיפור:** מעבר ל-Full-Text Search של SQL Server.

## בדיקות אוטומטיות

13 בדיקות (xUnit): מעברי סטטוס, ולידציה, שליפה/סינון/חיפוש (Integration), concurrency אמיתי מול SQL Server, Audit, ו-Bulk.

הרצה: `dotnet test`.

## שימוש בכלי AI

נעשה שימוש בכלי AI כעזר לפיתוח: ייצור שלד קוד, הסברים, ובדיקת החלטות. כל קוד נבדק והובן ידנית, הותאם למבנה הפרויקט (enums, DTOs, שכבות), ואומת בבנייה ובבדיקות אוטומטיות.



## ביצועים – מדידה על 100,000 רשומות

נבחרו שתי פעולות API מרכזיות לבחינת היעילות.

## 1. שליפה עם סינון ומיון (GET /api/requests)

**השאילתה:** סינון לפי Status + Priority, מיון לפי CreatedAt, דפדוף (OFFSET/FETCH).

**האינדקס התומך:** `IX_Requests_Status_Priority_CreatedAt` – אינדקס מורכב על `(Status, Priority, CreatedAt)`.

**למה הוא תומך:** הסינון לפי Status ו-Priority משתמש בחלק הראשון של האינדקס (Index Seek), והמיון לפי CreatedAt כבר ממוין בתוך האינדקס – כך שאין צורך בפעולת מיון נוספת. התוצאה: SQL Server מבצע Index Seek ולא Table Scan, ומחזיר רק את העמוד המבוקש (10 רשומות) מתוך 100,000.

## 2. נתונים מסכמים (GET /api/requests/summary)

**השאילתה:** `GROUP BY Status` ו-`GROUP BY Priority` – ספירה לכל קבוצה.

**האינדקסים התומכים:** האינדקס על Status (חלק מהמורכב) מאפשר ל-SQL Server לספור ישירות מהאינדקס בלי לקרוא את כל השורות.

**אופטימיזציה נוספת – Cache:** ה-summary נשמר ב-IMemoryCache ל-2 דקות, כך שרוב הקריאות לא מגיעות ל-DB כלל. ה-cache מתנקה אוטומטית בכל עדכון סטטוס (invalidation).

## האינדקסים שנוספו והסיבה

| אינדקס | עמודות | למה |
|--------|--------|-----|
| מורכב | Status, Priority, CreatedAt | סינון נפוץ לפי סטטוס+עדיפות + מיון לפי תאריך |
| בודד | OrganizationName | סינון/חיפוש לפי ארגון |
| בודד | AssignedTo | סינון לפי מטפל |
| בודד | CreatedAt | סינון/מיון לפי טווח תאריכים |
| בודד | RequestId (בטבלת ההיסטוריה) | שליפת היסטוריה מהירה לפנייה |

## כיצד נמנעת טעינת כל הנתונים לזיכרון

כל הסינון, המיון והדפדוף נבנים כ-`IQueryable` ומתורגמים ל-**SQL אחד** שרץ במסד הנתונים. ה-`ToListAsync` נקרא רק בסוף, אחרי Skip/Take – כך שה-DB מחזיר רק את העמוד (10 רשומות), ולא 100K. השימוש ב-`AsNoTracking` בקריאות חוסך מעקב מיותר של EF ומשפר ביצועים.

## Bottleneck ידוע והצעת שיפור

**Bottleneck:** החיפוש הטקסטואלי משתמש ב-`LIKE '%term%'` (Contains), שלא נתמך ביעילות ע"י אינדקס רגיל וגורם לסריקה על 100K רשומות.

**הצעת שיפור:** מעבר ל-Full-Text Search של SQL Server על Title ו-OrganizationName, שתומך בחיפוש מהיר בטקסט חופשי.



## תכנון עבודה – מערכת ניהול פניות

## החלטות עיקריות

| נושא | ההחלטה | למה |
|------|--------|-----|
| מבנה | 4 שכבות: API / BL / DAL / DTO | הפרדת אחריות, קל לתחזק ולבדוק |
| מסד נתונים | SQL Server + EF Core 8 | RowVersion מובנה ל-concurrency, תמיכה באינדקסים |
| Concurrency | RowVersion נשלח מהלקוח ונאכף ברמת ה-DB | מונע Lost Update, לא רק ב-UI |
| Bulk בכשל חלקי | Partial Success | כשל של פנייה אחת לא מפיל את השאר |
| שגיאות | Middleware מרכזי + ProblemDetails | טיפול אחיד, קודי HTTP נכונים |
| Cache | IMemoryCache על ה-summary + invalidation בעדכון | ה-summary יקר לחשב ומשתנה לאט |

## משימות (לפי סדר ביצוע)

| # | משימה | דרישה | מאמץ (שעות) | סטטוס |
|---|-------|-------|-------------|-------|
| 1 | הקמת Solution + 4 שכבות + תלויות | R10 | 1 | ✅ |
| 2 | מודל (RequestEntity + Enums) | R2 | 1 | ✅ |
| 3 | DbContext + אינדקסים + Migration | R1, R6 | 1 | ✅ |
| 4 | זריעת 100K + reseed | D | 1.5 | ✅ |
| 5 | שליפה: pagination, סינון, חיפוש, מיון, aggregations | R1 | 2.5 | ✅ |
| 6 | Validation (FluentValidation) | R1, R10 | 1 | ✅ |
| 7 | עדכון סטטוס + concurrency + מעברים (404/400/409) | R2 | 2 | ✅ |
| 8 | היסטוריית שינויים (Audit) + endpoint | R3 | 1.5 | ✅ |
| 9 | Bulk (Partial Success) | R4 | 1.5 | ✅ |
| 10 | Cache + invalidation | R7 | 1.5 | ✅ |
| 11 | Logging | R10 | 0.5 | ✅ |
| 12 | בדיקות אוטומטיות (13 בדיקות) | R8 | 2.5 | ✅ |
| 13 | Angular: רשימה, סינון, מיון, דפדוף, debounce, 409, summary, היסטוריה, bulk | R5 | 5 | ✅ |
| 14 | מדידת ביצועים + query plans | R6 | 1.5 | ✅ |
| 15 | README + העלאה לגיט | R11 | 1.5 | ✅ |

**סה"כ מאמץ מוערך:** ~26 שעות.

## תלויות עיקריות

- המודל (2) חוסם את כל השאר.
- ה-Repository (5) הוא הבסיס לשליפה, עדכון ו-bulk.
- ה-Angular (13) תלוי ב-API מוכן.
- ה-Cache invalidation (10) תלוי בעדכון (7).

## סיכון מרכזי

נכונות ה-Concurrency – באג כאן גורם לדריסה שקטה (Lost Update). מטופל בבדיקה אמיתית מול SQL Server (לא InMemory, שלא אוכף RowVersion).

## משימות שאינן כתיבת קוד

תכנון המודל והאינדקסים · מדידת ביצועים · תיעוד · העלאה לגיט.
