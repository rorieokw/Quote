# Quote - Development Setup Guide

## Version
Current: **1.0.0**

## Prerequisites

### Required Software
- **.NET 8.0 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
- **Git** - https://git-scm.com/downloads
- **Visual Studio 2022** or **VS Code** with C# extension

### Optional (for Mobile Development)
- **Android SDK** (via Android Studio) - Required for Quote.Mobile Android builds
- **Xcode** (macOS only) - Required for iOS builds

## Project Structure
```
Quote/
├── src/
│   ├── Quote.Domain/        # Entities, Enums, Value Objects
│   ├── Quote.Application/   # CQRS Commands/Queries, Services, Interfaces
│   ├── Quote.Infrastructure/# EF Core DbContext, External Services
│   ├── Quote.API/           # ASP.NET Core Web API (Backend)
│   ├── Quote.Web/           # Blazor Server + WebAssembly (Frontend)
│   ├── Quote.Shared/        # DTOs shared between projects
│   └── Quote.Mobile/        # MAUI Blazor Mobile App
└── tests/
    ├── Quote.UnitTests/
    └── Quote.IntegrationTests/
```

## Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/rorieokw/Quote.git
cd Quote
```

### 2. Build the Solution
```bash
# Build API and Web only (skip Mobile if Android SDK not configured)
dotnet build src/Quote.API/Quote.API.csproj
dotnet build src/Quote.Web/Quote.Web/Quote.Web.csproj
```

### 3. Run the Application

**Terminal 1 - API Server:**
```bash
dotnet run --project src/Quote.API/Quote.API.csproj --urls "http://localhost:5102"
```

**Terminal 2 - Web Server:**
```bash
dotnet run --project src/Quote.Web/Quote.Web/Quote.Web.csproj --urls "http://localhost:5200"
```

### 4. Access the Application
- **Web App**: http://localhost:5200
- **API Swagger**: http://localhost:5102/swagger

## Database
SQLite database (`Quote.db`) is created automatically on first run in the API project directory.

## NuGet Packages Used

### Quote.Application
- MediatR (CQRS pattern)
- FluentValidation
- AutoMapper

### Quote.Infrastructure
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.Design
- QuestPDF (Invoice PDF generation)
- Stripe.net (Payment processing)

### Quote.Web
- Blazor Server + WebAssembly (Interactive Auto mode)
- Bootstrap 5

## Configuration

### API Configuration (`src/Quote.API/appsettings.json`)
```json
{
  "App": {
    "Name": "Quote",
    "Version": "1.0.0",
    "Description": "Australia's Trade Marketplace"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Quote.db"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "Quote.API",
    "Audience": "Quote.App",
    "ExpiryHours": "24"
  }
}
```

### Production Configuration (add to appsettings.Production.json)
```json
{
  "Stripe": {
    "SecretKey": "sk_live_...",
    "PublishableKey": "pk_live_...",
    "WebhookSecret": "whsec_..."
  },
  "GoogleMaps": {
    "ApiKey": "AIza..."
  }
}
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### Jobs
- `GET /api/jobs` - List jobs
- `POST /api/jobs` - Create job
- `GET /api/jobs/{id}` - Get job details

### Quotes
- `GET /api/quotes` - List quotes
- `POST /api/quotes` - Create quote for job
- `POST /api/quotes/quick` - Quick quote creation

### Lead Scoring
- `GET /api/leads/scored` - Get scored leads for tradie
- `GET /api/leads/{jobId}/score` - Get specific job score

### Scheduling
- `GET /api/schedule` - Get tradie's schedule
- `POST /api/schedule/events` - Create schedule event

### Invoicing
- `GET /api/invoices` - List invoices
- `POST /api/invoices` - Create invoice from quote
- `GET /api/invoices/{id}/pdf` - Download invoice PDF

### Payments (Stripe)
- `POST /api/payments/create-intent` - Create payment intent
- `POST /api/stripe/connect` - Create connected account
- `GET /api/stripe/onboarding-link` - Get Stripe onboarding URL

### Recurring Jobs
- `GET /api/recurring-jobs` - List recurring job templates
- `POST /api/recurring-jobs` - Create recurring template

## Features Implemented (v1.0.0)

### Phase 1 - Business Growth
- [x] Lead Scoring System (rule-based)
- [x] Smart Scheduling with Google Maps integration
- [x] Recurring Jobs templates
- [x] Invoice Generation with PDF export
- [x] Stripe Payment Processing (Connected Accounts)

### Coming Soon
- [ ] Material Cost Templates
- [ ] Photo Markup Tools
- [ ] Quote Comparison View
- [ ] Price Benchmarking
- [ ] Multi-trade Jobs
- [ ] In-app Messaging (SignalR)
- [ ] Dispute Resolution

## Mobile App (Quote.Mobile)

### Android Build
```bash
# Ensure Android SDK is installed and ANDROID_HOME is set
dotnet build src/Quote.Mobile/Quote.Mobile.csproj -f net8.0-android
```

### iOS Build (macOS only)
```bash
dotnet build src/Quote.Mobile/Quote.Mobile.csproj -f net8.0-ios
```

## Troubleshooting

### "Android SDK directory could not be found"
Set the `ANDROID_HOME` environment variable to your Android SDK path:
- Windows: `C:\Users\{username}\AppData\Local\Android\Sdk`
- macOS: `~/Library/Android/sdk`

### Database migrations
```bash
dotnet ef migrations add MigrationName --project src/Quote.Infrastructure --startup-project src/Quote.API
dotnet ef database update --project src/Quote.Infrastructure --startup-project src/Quote.API
```

### Port already in use
Kill existing dotnet processes:
```bash
# Windows
taskkill /F /IM dotnet.exe

# macOS/Linux
pkill -f dotnet
```

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `STRIPE_SECRET_KEY` | Stripe API secret key | Production |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret | Production |
| `GOOGLE_MAPS_API_KEY` | Google Maps API key | Production |
| `ANDROID_HOME` | Android SDK path | Mobile dev |

## Contributing
1. Create a feature branch
2. Make changes
3. Run tests: `dotnet test`
4. Submit PR to main branch
