# IdentityService — OTP revision
Based on the previously delivered IdentityService.zip, not the live working tree. Keep your current appsettings.json and appsettings.Development.json, or merge settings; remove Email:FrontendBaseUrl. Gmail SMTP uses port 587, EnableSsl=true, and a Google App Password supplied as Email__Password. Preserve ConnectionStrings__DefaultConnection and Jwt__SigningKey securely. No frontend is required. No new database model or migration; existing UserTokens and DataProtectionKeys tables are reused. Existing migrations must already be applied. Data protection keys need restricted access and production protection at rest.

The two embedded HTML templates are newly authored, not copies of unavailable SurveyBasket HTML. OTP is six digits, expires in ten minutes, has five failed attempts per issuance, and replaces the prior challenge on resend. A repeated numerical code is statistically possible. Resend and verification use the existing IP-based rate limiter; production multi-instance deployments should add distributed per-account issuance throttling and monitoring. OTP and the underlying native Identity token are protected using ASP.NET Core Data Protection. Confirmation/reset and challenge consumption are in one database transaction. The native reset token's existing lifespan is unchanged, but the emailed OTP has a ten-minute limit. Existing link tokens are not accepted by the revised endpoints. No new JWT behavior.

## Verification (not run in authoring environment: .NET SDK unavailable)
From repository root:
```
dotnet build .\IdentityService\IdentityService.csproj
dotnet ef migrations has-pending-model-changes --project .\IdentityService\IdentityService.csproj --startup-project .\IdentityService\IdentityService.csproj
```
Run with your existing Docker Compose profile. Test POST /api/auth/register (new user), verify emailSent, POST /api/auth/confirm-email with email and code, then login with email and password. Confirm email before login. Test invalid code five times, expired code, resend (old code invalid unless coincidentally equal), duplicate confirmation, concurrent requests, forgot password and reset-password (email, code, newPassword), and refresh token revocation. Existing unconfirmed users should request resend, not register again. SMTP acceptance is not a guarantee of inbox delivery. Never log codes or secrets. 
