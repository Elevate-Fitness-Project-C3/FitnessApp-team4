# AuditService

خدمة التدقيق المركزية للمشروع. تستقبل أحداث الخدمات عبر MassTransit، وتحفظها في قاعدة SQL Server خاصة بها (`AuditDb`)، وتعرض تقارير التدقيق وفحوص الصحة. يستخدم MassTransit وسيط RabbitMQ لنقل الرسائل بين العمليات.

## التشغيل والربط

- للتشغيل المستقل من مجلد `AuditService` استخدم `docker compose -f compose.audit.yml up -d` بعد إعداد قاعدة Audit ومتغيرات الاتصال والوسيط. إعدادات الوسيط وسكربت تهيئته في `Operations/RabbitMq`.
- لتشغيل Audit مع Identity من جذر المستودع اضبط `AUDIT_SERVICE_DIRECTORY=./AuditService` ثم استخدم `docker compose -f docker-compose.yml -f AuditService/compose.audit.yml -f IdentityService/Messaging/compose.audit.yml up -d`. يمكن إضافة ملف التطوير المعتاد عند التشغيل المحلي.
- شغّل الوسيط وطبّق migrations الخاصة بكل خدمة وهيّئ الـtopology عبر `Operations/RabbitMq/InitializeBroker.ps1` قبل تفعيل `AUDIT_MESSAGING_ENABLED` و`IDENTITY_AUDIT_MESSAGING_ENABLED`. إعداد الوسيط المرفق يستخدم المنفذ `5672` للتطوير. لاستخدامه اضبط بيئتي التطبيق على `Development`، ومنفذي `AUDIT_BROKER_PORT` و`IDENTITY_AUDIT_BROKER_PORT` على `5672`، وخياري `AUDIT_BROKER_USE_TLS` و`IDENTITY_AUDIT_BROKER_USE_TLS` على `false`. خارج التطوير استخدم وسيطًا مجهزًا بـTLS واضبط عنوانه ومنفذه لكل خدمة.
- لربط خدمة أخرى، أضف عقد V1 إلى مشروعها ثم أرسل الحدث عبر MassTransit إلى `exchange:audit.<source-service>.v1`. أضف ملف المصدر وسياسات أفعاله إلى `Messaging:Sources` في Audit، وشغّل سكربت التهيئة مع `-SourceService <source-service>` وبحساب ناشر مستقل. تفاصيل الإعداد في [عقد Audit V1](Messaging/Contracts/README.md).
- للتحقق من صلاحية قراءة التقارير اضبط `AUDIT_IDENTITY_BASE_URL`. يمرر Compose قيمة `AUDIT_REPORTING_SERVICE_KEY` إلى الخدمتين. إذا ضبطت `IDENTITY_AUDIT_REPORTING_SERVICE_KEY` صراحةً، يجب أن يطابق المفتاح المشترك. استخدم مفتاحًا مستقلًا لتوقيع مؤشر الصفحات في `AUDIT_REPORTING_CURSOR_KEY`.

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

تعريفات عقد الرسائل موجودة داخل `Messaging/Contracts` في كل خدمة تستخدمه. تعتمد خدمة Audit على `Microsoft.EntityFrameworkCore.SqlServer` و`Microsoft.EntityFrameworkCore.Design` بالإصدار `10.0.15`.

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

كل حقول receipts إلزامية. يبقى receipt بعد أرشفة الحدث. يحسب المستهلك بصمة SHA-256 من تمثيل JSON مرتب المفاتيح ويقارن البصمة قبل قبول إعادة إرسال معرّف الحدث نفسه.

القيود التسعة:

- رفض UUID فارغ في الجدولين.
- منع المعرفات الأساسية الفارغة في الأحداث والمصدر الفارغ في receipts.
- قواعد الفاعل: مستخدم بمعرف بشري دون system id، أو نظام بمعرف نظام دون مستخدم/snapshot، أو مجهول دون هويات مدعاة.
- حالات V1 المسموحة فقط، وإلزام entity id للحدث الملتزم.
- وقت الحدث والاستقبال وأول receipt غير افتراضي وoffset صفر.
- JSON صالح من نوع object أو SQL NULL في before/after؛ arrays/scalars ليست payload المستوى الأعلى المسموح.

تستخدم المعرفات والنطاقات collation `Latin1_General_100_BIN2`. يفرض المستهلك حدود المعرفات، ويطابق نطاقات التطبيق والمستأجر وقوائم السماح بالقيم قبل الحفظ.

الفهارس:

- `UX_AuditEvents_Source_EventId`: فريد على المصدر ومعرف الحدث.
- `IX_AuditEvents_Scope_Time`: التطبيق والمستأجر ثم الوقت والمعرف تنازليًا.
- `IX_AuditEvents_Scope_Source_Time`: السابق مع المصدر.
- `IX_AuditEvents_Scope_Actor_Time`: السابق مع actor user id.
- `IX_AuditEvents_Scope_Entity_Time`: النطاق ونوع الكيان ومعرفه ثم الوقت والمعرف.
- `IX_AuditEvents_Scope_Correlation`: النطاق وcorrelation والمعرف.
- PK receipts فريد على `(SourceService, EventId)`.

فُحصت الحدود القصوى لأحجام مفاتيح الفهارس مقابل 900 بايت للمفتاح clustered و1700 للفهرس nonclustered. فهارس action/status/device/IP إضافية مؤجلة إلى قياس أحمال التقارير.

يستخدم بحث التقارير نطاقًا زمنيًا على `TimestampUtc` حتى عند ترتيب النتائج حسب `IngestedAtUtc`. تدعم الفهارس الحالية النطاقات والتصفية والترتيب حسب وقت الحدث مع `Id`. ترتيب وقت الإدخال قد يحتاج خطوة فرز؛ يتوقف قرار فهرس إضافي على خطط التنفيذ وقياسات الاستخدام.

## التشغيل والإعداد الآمن

تحدد `ConnectionStrings:AuditDb` الخادم وقاعدة البيانات واسم مستخدم التطبيق وإعدادات TLS. الإعداد المرفق يستخدم `sql-server,1433` و`AuditDb` و`audit_app`. يمرر Compose كلمة المرور من `AUDIT_SQL_PASSWORD` إلى `Database:Password`. عند تشغيل Audit بصورة مستقلة، عيّن اسم خادم SQL المتاح لها عبر إعدادات الخدمة. يقرأ التطبيق وأداة EF إعدادات ASP.NET Core المعتادة، ومنها environment variables وuser-secrets في Development.

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
- `GET /health/messaging`: حالة إعداد الاستقبال والناقل. يعيد HTTP 503 عند التعطيل أو تدهور الناقل.
  استخدمه مع فحص جاهزية قاعدة البيانات عند تشغيل الاستقبال.
- فحوص الصحة تعرض الحالة فقط. يعتمد توفر التقارير أيضًا على وصول Audit إلى نقطة التفويض في Identity وعلى إعداد مفتاح المؤشر.

يستخدم التطبيق JSON console logging. تظهر تحذيرات الناقل واتصال SQL، وتظل سجلات أوامر EF وتحديثاته التي قد تتضمن قيم الصفوف معطلة في Audit. يسجل معالج أخطاء التقارير معرّف التتبع ونوع الخطأ ورقم SQL المتاح. إعدادات EF تعطل sensitive data logging وdetailed errors.

## migrations والنشر

المتطلب SQL Server 2022 أوأحدث، لأن `ISJSON(value, OBJECT)` يستخدم قيد نوع JSON المتاح من 2022. راجع [توثيق Microsoft](https://learn.microsoft.com/en-us/sql/t-sql/functions/isjson-transact-sql?view=sql-server-ver16).

تنشئ migration `20261117185026_InitialAuditStorage` جدولي السجل والإيصالات والفهارس والقيود. تضيف `20261117191028_AddMessagingInboxOutbox` جداول الرسائل المطلوبة لتشغيل المستهلك.

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
```

أزيل مشروعا التحقق التنفيذي المؤقتان وfixture بطلب المستخدم في تنظيف نهاية اليوم. النتائج السابقة محفوظة في تقارير الحالة والتشخيص بوصفها تاريخية، ولا يوجد حاليًا runner ضمن solution. يلزم بناء اختبارات دائمة للنموذج وSQL والتفرد والفهارس وsnapshot وappend-only وreadiness عند الاستئناف، دون استخدام InMemory لإثبات معاملات SQL.

نجاح هذه الفحوص ليس نجاح تكامل قاعدة فعلية. يبقى مطلوبًا على SQL Server 2022 معزول: تطبيق migration، وتجربة قبول/رفض constraints تحت null وUnicode، والتكرار المتزامن، وUTC، وJSON، وصلاحيات حساب التشغيل، وحالات readiness قبل وبعد التطبيق. Docker daemon غير متاح في جلسة التنفيذ الحالية، فلم تدع المراجعة تنفيذ ذلك.
