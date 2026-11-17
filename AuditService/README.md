# AuditService — أساس الخدمة، المرحلة الثانية

خدمة ASP.NET Core مستقلة تستهدف .NET 10 وتملك قاعدة SQL Server مخصصة (`AuditDb`). تعرض حاليًا فحوص صحة فقط. لا توجد واجهات قراءة تاريخ أو كتابة، ولا consumers أو producers أو MassTransit أو RabbitMQ أو transactional outbox. لم يتغير أي مشروع أعمال أو عقد V1.

## الملكية والبنية

```text
AuditService/
  Program.cs
  appsettings.json
  Auditing/
    AuditEvent.cs
    AuditEventReceipt.cs
  Health/
    AuditDatabaseReadinessCheck.cs
  Persistence/
    AuditDbContext.cs
    AuditDbContextFactory.cs
    AuditDatabaseConfiguration.cs
    Configurations/
      AuditEventConfiguration.cs
      AuditEventReceiptConfiguration.cs
    Migrations/
      20261117185026_InitialAuditStorage.cs
      20261117185026_InitialAuditStorage.Designer.cs
      AuditDbContextModelSnapshot.cs
  Operations/
    ConfigureDatabasePermissions.sql
```

المرجع المشترك الوحيد هو `Audit.Contracts` لاستخدام تعريفَي actor/status المعتمدين، دون ربط الخدمة بكيانات IdentityService أو assembly أعمال. تعتمد الخدمة على `Microsoft.EntityFrameworkCore.SqlServer` و`Microsoft.EntityFrameworkCore.Design` بالإصدار `10.0.15`؛ حزمة Design خاصة بأدوات التطوير.

`CreatedBy`, `CreatedAt`, `LastModifiedBy`, `LastModifiedAt` أعمدة محلية في كيانات الأعمال داخل كل خدمة. ليست جداول أو metadata مركزية هنا. AuditService تحفظ التاريخ فقط، ولا تصل إلى قاعدة أي خدمة أخرى. تنشئ عمليات provisioning قاعدة الخدمة وحسابها المنفصلين؛ لا تستخدم اتصال IdentityDb أو حسابًا يمتلك جميع قواعد الخدمات.

## المخطط

الجدول `dbo.AuditEvents`:

| الحقول | النوع | null |
| --- | --- | --- |
| `Id` | `bigint identity`، PK clustered | لا |
| `EventId` | `uniqueidentifier` | لا |
| `SourceService` | `varchar(64)` | لا |
| `ApplicationId` | `varchar(100)` | لا |
| `TenantId` | `nvarchar(128)` | نعم |
| `ActorKind`, `Status` | `tinyint`، قيم V1: 1، 2، 3 | لا |
| `ActorUserId` | `nvarchar(450)` | نعم |
| `ActorUserNameSnapshot` | `nvarchar(256)` | نعم |
| `ActorSystemId` | `varchar(128)` | نعم |
| `Action` | `varchar(160)` | لا |
| `EntityType` | `varchar(128)` | لا |
| `EntityId` | `nvarchar(450)` | نعم؛ مطلوب مع Committed |
| `TimestampUtc`, `IngestedAtUtc` | `datetimeoffset(7)` | لا |
| `IpAddress` | `varchar(45)` | نعم |
| `DeviceName` | `nvarchar(128)` | نعم |
| `UserAgent` | `nvarchar(1024)` | نعم |
| `CorrelationId` | `varchar(128)` | لا |
| `OldValues`, `NewValues` | `nvarchar(max)`، JSON object فقط | نعم |

الجدول `dbo.AuditEventReceipts`:

| الحقل | النوع | القاعدة |
| --- | --- | --- |
| `SourceService` | `varchar(64)` | جزء من PK المركب |
| `EventId` | `uniqueidentifier` | جزء من PK المركب، لا يولده EF |
| `PayloadHash` | `binary(32)` | digest SHA-256، صيغة canonical input تعتمد قبل consumer |
| `FirstIngestedAtUtc` | `datetimeoffset(7)` | وقت أول قبول UTC |

كل حقول receipts إلزامية. لا FK بين الجدولين؛ يجب أن يبقى receipt بعد أرشفة الحدث. صيغة hash ثنائية ثابتة بدل تمثيل hex نصي؛ ليست تغييرًا لعقد الرسالة لأن receipts بيانات داخلية. لا يوجد تنفيذ hashing أو deduplication consumer بعد.

القيود التسعة:

- رفض UUID فارغ في الجدولين.
- منع المعرفات الأساسية الفارغة في الأحداث والمصدر الفارغ في receipts.
- قواعد الفاعل: مستخدم بمعرف بشري دون system id، أو نظام بمعرف نظام دون مستخدم/snapshot، أو مجهول دون هويات مدعاة.
- حالات V1 المسموحة فقط، وإلزام entity id للحدث الملتزم.
- وقت الحدث والاستقبال وأول receipt غير افتراضي وoffset صفر.
- JSON صالح من نوع object أو SQL NULL في before/after؛ arrays/scalars ليست payload المستوى الأعلى المسموح.

تستخدم المعرفات والنطاقات collation `Latin1_General_100_BIN2` لمقارنة حساسة لحالة الأحرف، دون تغيير قاعدة البيانات العامة. التحقق الكامل من ASCII والمعرفات وwhitespace والنطاق والتفويض وقائمة سماح القيم سيبقى مسؤولية إدخال الأحداث في المرحلة الرابعة؛ قيود SQL ليست إثباتًا للثقة أو فلترًا للأسرار.

الفهارس:

- `UX_AuditEvents_Source_EventId`: فريد على المصدر ومعرف الحدث.
- `IX_AuditEvents_Scope_Time`: التطبيق والمستأجر ثم الوقت والمعرف تنازليًا.
- `IX_AuditEvents_Scope_Source_Time`: السابق مع المصدر.
- `IX_AuditEvents_Scope_Actor_Time`: السابق مع actor user id.
- `IX_AuditEvents_Scope_Entity_Time`: النطاق ونوع الكيان ومعرفه ثم الوقت والمعرف.
- `IX_AuditEvents_Scope_Correlation`: النطاق وcorrelation والمعرف.
- PK receipts فريد على `(SourceService, EventId)`.

فُحصت الحدود القصوى لأحجام مفاتيح الفهارس مقابل 900 بايت للمفتاح clustered و1700 للفهرس nonclustered. فهارس action/status/device/IP إضافية مؤجلة إلى قياس أحمال التقارير.

## التشغيل والإعداد الآمن

الإعداد المطلوب هو `ConnectionStrings:AuditDb`. يقرأ التطبيق وأداة EF إعدادات ASP.NET Core المعتادة، ومنها environment variables وuser-secrets في Development. لا توجد credentials أو connection string فعلية في الملفات.

مثال جلسة PowerShell للتطوير المحلي باستخدام Windows Authentication:

```powershell
$env:ConnectionStrings__AuditDb = 'Server=localhost;Database=AuditDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5085'
dotnet run --project AuditService/AuditService.csproj --no-launch-profile
```

`TrustServerCertificate=True` للتطوير المحلي فقط؛ الإنتاج يستخدم شهادة SQL موثوقة وTLS. يمكن تخزين الاتصال عبر `dotnet user-secrets` بدل session environment. استخدم مخزن أسرار بيئيًا في الإنتاج، ولا تمرر كلمات المرور في CLI أو source control.

يجب تحديد server وdatabase صراحة؛ يفشل بدء التشغيل برسالة عامة إذا غاب الإعداد أو كان غير صالح. لا يتصل التطبيق بـSQL عند startup لفرض migrations؛ يبقى قابلًا لفحص liveness عند انقطاع القاعدة. يحصر الاتصال في قاعدة الاتصال المخصصة، دون SQL عابر للقواعد. مدة connect وأوامر قاعدة الأساس محدودة بخمس ثوان؛ connect timeout صفر غير المحدود يخفض إلى خمس ثوان أيضًا.

`AllowedHosts` الافتراضي `localhost;127.0.0.1`. اضبطه بأسماء المضيفين الفعلية قبل النشر. TLS والوصول الشبكي وreverse proxy من مسؤولية deployment اللاحقة؛ لم تتغير Compose أو المنافذ المشتركة.

الفحوص المتاحة:

- `GET /health/live`: HTTP 200 إذا كان process يستجيب، دون SQL.
- `GET /health/ready`: HTTP 200 فقط مع اتصال ناجح ودون تغييرات model غير ممثلة في migration ودون migrations معلقة؛ وإلا HTTP 503.
- الردود status فقط، دون تفاصيل اتصال أو schema أو exception. لا تعني الجاهزية أن ingestion أو reporting متاحان؛ كلاهما غير منفذ.

يستخدم التطبيق JSON console logging فقط، دون اعتماد على صلاحيات Windows Event Log. قُيدت logs الخاصة بـEF وSqlClient برمجيًا، مع تعطيل sensitive data logging وdetailed errors، وإرجاع نتيجة صحية عامة دون حفظ exception في HealthCheckResult. لا يوجد request-body أو payload logging. تضاف observability آمنة ذات أكواد أخطاء محدودة في المراحل اللاحقة دون إخراج الاستثناء الخام.

## migrations والنشر

المتطلب SQL Server 2022 أوأحدث، لأن `ISJSON(value, OBJECT)` يستخدم قيد نوع JSON المتاح من 2022. راجع [توثيق Microsoft](https://learn.microsoft.com/en-us/sql/t-sql/functions/isjson-transact-sql?view=sql-server-ver16).

ولد EF migration `20261117185026_InitialAuditStorage` وdesigner وsnapshot. ينشئ الجدولين والقيود والفهارس فقط، دون seeds أو حسابات أو tables messaging أو تعديل قواعد الخدمات الأخرى. توليد migration/script لا يطبق التغييرات على SQL.

بعد provisioning قاعدة مخصصة ومراجعة script، يستخدم مشغل النشر اتصال حساب migrations منفصلًا:

```powershell
dotnet ef migrations script 0 --idempotent --project AuditService/AuditService.csproj --output AuditService/obj/InitialAuditStorage.sql
dotnet ef database update --project AuditService/AuditService.csproj
dotnet ef migrations has-pending-model-changes --project AuditService/AuditService.csproj
```

هذه أوامر تشغيل يدوي، وليست خطوات startup. لا يوجد `EnsureCreated` أو`Migrate` في التطبيق. `Down` المولد يحذف الجدولين؛ لا يستخدم كرجوع روتيني بعد حفظ التاريخ. الرجوع يفضل إعادة إصدار التطبيق دون إسقاط البيانات، مع backup واسترداد مخططين.

أداة `dotnet-ef` المتاحة أثناء التحقق `10.0.13` مقابل مكتبات `10.0.15`، فأصدرت تحذير إصدار دون فشل التوليد أو فحص snapshot. يفضل توحيد الأداة إلى `10.0.15` في بيئة النشر؛ لم أغير أداة المستخدم العامة.

## append-only والأرشفة

يرفض `AuditDbContext` تعديل أو حذف الأحداث والـreceipts في مسارات SaveChanges المتزامنة وغير المتزامنة. لا يغير timestamps ولا يولد تاريخًا تلقائيًا. هذه حماية تطبيقية فقط: bulk update/delete وraw SQL وحساب DBA تتجاوزها.

الملف `Operations/ConfigureDatabasePermissions.sql` يعرّف دور قاعدة محدودًا، ويمنع update/delete/alter على الجدولين. ينفذ يدويًا بواسطة مسؤول القاعدة بعد migration، ثم يضيف المشغل مستخدم التطبيق إليه. لا ينشئ login أو يخزن password ولا ينفذ عند startup. لا تجعل حساب التشغيل db_owner أوsysadmin، ولا تعطه migration credentials. صلاحيات retention/archival تبقى لهوية أخرى بعد اعتماد سياستها. جداول inbox/outbox اللاحقة ستحتاج صلاحيات تحديث وتنظيف مستقلة؛ لا توسع صلاحيات التاريخ لأجلها.

الأحداث الساخنة والـreceipts لهما مدتا حياة مختلفتان. لا تحذف receipts بسبب خروج الأحداث من التخزين الساخن؛ يجب أن تغطي كامل أفق replay من queues وoutbox وarchives وbackups. لم يثبت retention job أوarchive implementation لأن المدد وlegal holds وأفق replay لم تعتمد بعد.

## التحقق وحدوده

```powershell
dotnet build AuditService/AuditService.csproj -c Release
dotnet build Audit.Contracts/Audit.Contracts.csproj -c Release
```

أزيل مشروعا التحقق التنفيذي المؤقتان وfixture بطلب المستخدم في تنظيف نهاية اليوم. النتائج السابقة محفوظة في تقارير الحالة والتشخيص بوصفها تاريخية، ولا يوجد حاليًا runner ضمن solution. يلزم بناء اختبارات دائمة للنموذج وSQL والتفرد والفهارس وsnapshot وappend-only وreadiness عند الاستئناف، دون استخدام InMemory لإثبات معاملات SQL.

نجاح هذه الفحوص ليس نجاح تكامل قاعدة فعلية. يبقى مطلوبًا على SQL Server 2022 معزول: تطبيق migration، وتجربة قبول/رفض constraints تحت null وUnicode، والتكرار المتزامن، وUTC، وJSON، وصلاحيات حساب التشغيل، وحالات readiness قبل وبعد التطبيق. Docker daemon غير متاح في جلسة التنفيذ الحالية، فلم تدع المراجعة تنفيذ ذلك.

## قبل المرحلة الثالثة

إعادة التحقق واعتماد إصدار MassTransit وترخيصه وتوافقه مع EF Core 10، ثم اعتماد source isolation وbroker credentials وTLS وqueues وخطة retries/inbox/outbox. لا تضف هذه الجداول إلى migration الحالية؛ ستكون migration مستقلة بعد الموافقة. تبقى schema grants ونطاقات التقارير وعقد التحقق الداخلي مع IdentityService واعتماد canonical hashing وحد payload ومنع الأسرار وآلية failure auditing وretention قرارات مراحلها، وليست ميزات جاهزة هنا.
