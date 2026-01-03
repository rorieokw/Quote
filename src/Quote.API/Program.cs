using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quote.API.Hubs;
using Quote.API.Services;
using Quote.Application;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;
using Quote.Domain.Enums;
using Quote.Infrastructure;
using Quote.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add SignalR
builder.Services.AddSignalR();

// Add SignalR notification service
builder.Services.AddScoped<IMessageNotificationService, MessageNotificationService>();

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Support SignalR token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("user_type", "Admin"));
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Quote API",
        Version = "v1",
        Description = "API for Quote - Australia's Trade Job Marketplace"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS (SignalR requires credentials which means no AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5200",
                "https://localhost:5200",
                "http://localhost:5102",
                "http://10.0.2.2:5102"  // Android emulator
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();

    // Create database and tables if they don't exist
    context.Database.EnsureCreated();

    // Seed trade categories if empty
    if (!context.TradeCategories.Any())
    {
        var categories = new List<TradeCategory>
        {
            new() { Id = Guid.NewGuid(), Name = "Electrician", Icon = "⚡", Description = "Electrical installations, repairs, and maintenance" },
            new() { Id = Guid.NewGuid(), Name = "Plumber", Icon = "🔧", Description = "Plumbing installations, repairs, and drainage" },
            new() { Id = Guid.NewGuid(), Name = "Builder", Icon = "🏗️", Description = "Construction, renovations, and structural work" },
            new() { Id = Guid.NewGuid(), Name = "Painter", Icon = "🎨", Description = "Interior and exterior painting" },
            new() { Id = Guid.NewGuid(), Name = "Carpenter", Icon = "🪚", Description = "Woodwork, cabinetry, and timber framing" },
            new() { Id = Guid.NewGuid(), Name = "Tiler", Icon = "🧱", Description = "Floor and wall tiling" },
            new() { Id = Guid.NewGuid(), Name = "Roofer", Icon = "🏠", Description = "Roof repairs, installations, and maintenance" },
            new() { Id = Guid.NewGuid(), Name = "HVAC Technician", Icon = "❄️", Description = "Heating, ventilation, and air conditioning" },
            new() { Id = Guid.NewGuid(), Name = "Landscaper", Icon = "🌳", Description = "Garden design, maintenance, and outdoor work" },
            new() { Id = Guid.NewGuid(), Name = "Locksmith", Icon = "🔐", Description = "Lock installations, repairs, and security" },
            new() { Id = Guid.NewGuid(), Name = "Glazier", Icon = "🪟", Description = "Glass installation and repairs" },
            new() { Id = Guid.NewGuid(), Name = "Concreter", Icon = "🧱", Description = "Concrete work, driveways, and slabs" },
            new() { Id = Guid.NewGuid(), Name = "Fencer", Icon = "🚧", Description = "Fence installation and repairs" },
            new() { Id = Guid.NewGuid(), Name = "Plasterer", Icon = "🪣", Description = "Plastering and gyprock work" },
            new() { Id = Guid.NewGuid(), Name = "Cleaner", Icon = "🧹", Description = "Residential and commercial cleaning" }
        };

        context.TradeCategories.AddRange(categories);
        context.SaveChanges();

        Console.WriteLine("✅ Database created and seeded with trade categories!");
    }
    // Seed admin user for verification reviews
    var adminEmail = "admin@quote.local";
    var adminUser = context.Users.FirstOrDefault(u => u.Email == adminEmail);
    if (adminUser is null)
    {
        adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            UserType = UserType.Admin,
            FirstName = "System",
            LastName = "Admin",
            IsVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
        Console.WriteLine("Seeded admin user: admin@quote.local / Admin123!");
    }

    // Seed a demo customer to own showcase jobs
    var demoCustomerEmail = "demo.customer@quote.local";
    var demoCustomer = context.Users.FirstOrDefault(u => u.Email == demoCustomerEmail);
    if (demoCustomer is null)
    {
        demoCustomer = new User
        {
            Id = Guid.NewGuid(),
            Email = demoCustomerEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("QuoteDemo123!"),
            UserType = UserType.Customer,
            FirstName = "Harper",
            LastName = "Patel",
            Phone = "0412 345 678",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(demoCustomer);
        context.SaveChanges();
        Console.WriteLine("Seeded demo customer user for showcase jobs.");
    }

    // Seed showcase jobs per trade category if none exist
    if (!context.Jobs.Any())
    {
        var categories = context.TradeCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        Guid Cat(string name) => categories.TryGetValue(name, out var id)
            ? id
            : throw new InvalidOperationException($"Missing trade category seed: {name}");

        var seededAt = DateTime.UtcNow;
        var jobs = new List<Job>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Electrician"),
                Title = "Upgrade switchboard and circuits in 1960s weatherboard",
                Description = "Upgrade existing rewireable fuses to RCBOs, add dedicated 32A circuit for induction cooktop, and balance existing lighting/power circuits. Single-storey weatherboard, easy under-house access. Include compliance certificate and test results.",
                Status = JobStatus.Open,
                BudgetMin = 4200,
                BudgetMax = 6200,
                PreferredStartDate = DateTime.UtcNow.AddDays(14),
                PreferredEndDate = DateTime.UtcNow.AddDays(28),
                IsFlexibleDates = false,
                Latitude = -33.9120,
                Longitude = 151.1550,
                SuburbName = "Marrickville",
                State = AustralianState.NSW,
                Postcode = "2204",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Electrician"),
                Title = "Three-phase EV charger and load management in townhouse garage",
                Description = "Install wall-mounted 22kW EV charger with tidy conduit run, CT clamps for load management on existing 63A supply, and compliance certificate. Meter board is 12m from garage; ceiling space accessible.",
                Status = JobStatus.Open,
                BudgetMin = 2600,
                BudgetMax = 3400,
                PreferredStartDate = DateTime.UtcNow.AddDays(10),
                PreferredEndDate = DateTime.UtcNow.AddDays(21),
                IsFlexibleDates = true,
                Latitude = -33.8150,
                Longitude = 151.1680,
                SuburbName = "Lane Cove",
                State = AustralianState.NSW,
                Postcode = "2066",
                PropertyType = PropertyType.Townhouse
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Plumber"),
                Title = "Replace corroded galvanised water lines and kitchen mixer",
                Description = "Remove remaining gal water lines under 1950s brick veneer, replace with PEX, install isolation valves, and fit new gooseneck mixer (supplied). Crawl space accessible, hot water storage unit is external.",
                Status = JobStatus.Open,
                BudgetMin = 1800,
                BudgetMax = 2600,
                PreferredStartDate = DateTime.UtcNow.AddDays(7),
                PreferredEndDate = DateTime.UtcNow.AddDays(14),
                IsFlexibleDates = false,
                Latitude = -37.7700,
                Longitude = 144.9630,
                SuburbName = "Brunswick",
                State = AustralianState.VIC,
                Postcode = "3056",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Plumber"),
                Title = "Blocked stormwater line and gutter re-fall",
                Description = "Jet and CCTV 40m stormwater run to street, locate and rectify low spots, and adjust rear gutter fall. Double-storey brick home with side access. Please include report and photos.",
                Status = JobStatus.Open,
                BudgetMin = 2400,
                BudgetMax = 3200,
                PreferredStartDate = DateTime.UtcNow.AddDays(5),
                PreferredEndDate = DateTime.UtcNow.AddDays(12),
                IsFlexibleDates = true,
                Latitude = -37.9078,
                Longitude = 145.0026,
                SuburbName = "Brighton",
                State = AustralianState.VIC,
                Postcode = "3186",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Builder"),
                Title = "Remove wall and install LVL beam for open-plan kitchen",
                Description = "Engineer-approved design to remove 3.6m non-loadbearing nibs and install LVL for open-plan kitchen/dining. Includes making-good ceilings/walls ready for paint. Waste removal required.",
                Status = JobStatus.Open,
                BudgetMin = 12500,
                BudgetMax = 16500,
                PreferredStartDate = DateTime.UtcNow.AddDays(21),
                PreferredEndDate = DateTime.UtcNow.AddDays(35),
                IsFlexibleDates = false,
                Latitude = -27.4412,
                Longitude = 152.9772,
                SuburbName = "Ashgrove",
                State = AustralianState.QLD,
                Postcode = "4060",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Builder"),
                Title = "Rebuild 35m² spotted gum deck with privacy screen",
                Description = "Demolish existing Merbau deck, rebuild with H3 framing, spotted gum boards, stainless screws, and 1.8m slatted privacy screen. Include oil finish and waste removal.",
                Status = JobStatus.Open,
                BudgetMin = 14500,
                BudgetMax = 18500,
                PreferredStartDate = DateTime.UtcNow.AddDays(18),
                PreferredEndDate = DateTime.UtcNow.AddDays(32),
                IsFlexibleDates = true,
                Latitude = -27.4920,
                Longitude = 153.1060,
                SuburbName = "Carina",
                State = AustralianState.QLD,
                Postcode = "4152",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Painter"),
                Title = "Interior repaint of 3-bed apartment including ceilings",
                Description = "Prep, patch, and repaint 110m² apartment walls/ceilings/trim in low-VOC white. Include water stain blocking in bathroom ceiling and caulking around frames. Furniture will be moved to center of rooms.",
                Status = JobStatus.Open,
                BudgetMin = 5200,
                BudgetMax = 6800,
                PreferredStartDate = DateTime.UtcNow.AddDays(9),
                PreferredEndDate = DateTime.UtcNow.AddDays(16),
                IsFlexibleDates = false,
                Latitude = -33.9190,
                Longitude = 151.2410,
                SuburbName = "Randwick",
                State = AustralianState.NSW,
                Postcode = "2031",
                PropertyType = PropertyType.Unit
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Painter"),
                Title = "Exterior repaint of coastal weatherboard cottage",
                Description = "Wash, scrape, spot-prime, and repaint weatherboards, fascia, eaves, and windows in Dulux Weathershield. Single-storey on stumps, coastal environment. Include scaffold/trestles as needed.",
                Status = JobStatus.Open,
                BudgetMin = 7800,
                BudgetMax = 10500,
                PreferredStartDate = DateTime.UtcNow.AddDays(20),
                PreferredEndDate = DateTime.UtcNow.AddDays(34),
                IsFlexibleDates = true,
                Latitude = -34.0540,
                Longitude = 151.1520,
                SuburbName = "Cronulla",
                State = AustralianState.NSW,
                Postcode = "2230",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Carpenter"),
                Title = "Custom built-in shelving and desk for home office",
                Description = "Build wall-to-wall MDF/vic ash veneer shelving and floating desk (3.2m) with cable management and soft-close drawers. Include spray paint finish and installation. Plans available.",
                Status = JobStatus.Open,
                BudgetMin = 6200,
                BudgetMax = 8800,
                PreferredStartDate = DateTime.UtcNow.AddDays(12),
                PreferredEndDate = DateTime.UtcNow.AddDays(24),
                IsFlexibleDates = false,
                Latitude = -37.8000,
                Longitude = 144.9830,
                SuburbName = "Fitzroy",
                State = AustralianState.VIC,
                Postcode = "3065",
                PropertyType = PropertyType.Townhouse
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Carpenter"),
                Title = "Timber stair replacement with hardwood treads",
                Description = "Remove existing pine stairs, install new stringers, hardwood treads/risers, and handrail to code. Open stairwell, 15 risers. Finish to stain-ready stage.",
                Status = JobStatus.Open,
                BudgetMin = 7800,
                BudgetMax = 9800,
                PreferredStartDate = DateTime.UtcNow.AddDays(15),
                PreferredEndDate = DateTime.UtcNow.AddDays(28),
                IsFlexibleDates = true,
                Latitude = -37.8180,
                Longitude = 144.9980,
                SuburbName = "Richmond",
                State = AustralianState.VIC,
                Postcode = "3121",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Tiler"),
                Title = "Full bathroom re-tile with waterproofing",
                Description = "Strip existing tiles, waterproof to AS 3740, retile 2.4m x 2.8m bathroom floor/walls to 1200mm, niche to 2100mm. Rectified 600x600 porcelain supplied. Include screed, trims, and siliconing.",
                Status = JobStatus.Open,
                BudgetMin = 6200,
                BudgetMax = 8200,
                PreferredStartDate = DateTime.UtcNow.AddDays(17),
                PreferredEndDate = DateTime.UtcNow.AddDays(30),
                IsFlexibleDates = false,
                Latitude = -33.8980,
                Longitude = 151.1800,
                SuburbName = "Newtown",
                State = AustralianState.NSW,
                Postcode = "2042",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Tiler"),
                Title = "Tile over existing balcony with outdoor porcelain",
                Description = "Prepare and tile 18m² concrete balcony using 20mm exterior porcelain pavers, correct falls to drain, and silicone perimeters. Parapet height compliant. Include primer and grout.",
                Status = JobStatus.Open,
                BudgetMin = 3800,
                BudgetMax = 5200,
                PreferredStartDate = DateTime.UtcNow.AddDays(8),
                PreferredEndDate = DateTime.UtcNow.AddDays(18),
                IsFlexibleDates = true,
                Latitude = -33.9200,
                Longitude = 151.2550,
                SuburbName = "Coogee",
                State = AustralianState.NSW,
                Postcode = "2034",
                PropertyType = PropertyType.Unit
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Roofer"),
                Title = "Colorbond re-roof with anticon blanket",
                Description = "Remove existing tiles, install sarking, battens, Colorbond roof sheets with 60mm anticon, new flashings, and whirlybird. 120m² low-pitch roof. Scaffold by others.",
                Status = JobStatus.Open,
                BudgetMin = 18500,
                BudgetMax = 23500,
                PreferredStartDate = DateTime.UtcNow.AddDays(25),
                PreferredEndDate = DateTime.UtcNow.AddDays(40),
                IsFlexibleDates = true,
                Latitude = -27.3310,
                Longitude = 153.0100,
                SuburbName = "Albany Creek",
                State = AustralianState.QLD,
                Postcode = "4035",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Roofer"),
                Title = "Gutter and fascia replacement on two-storey brick",
                Description = "Replace 42m of guttering and fascia with Colorbond, add 4 x 90mm downpipes, and improve falls. Two-storey access required; front driveway for lift. Provide disposal of old materials.",
                Status = JobStatus.Open,
                BudgetMin = 8800,
                BudgetMax = 11500,
                PreferredStartDate = DateTime.UtcNow.AddDays(14),
                PreferredEndDate = DateTime.UtcNow.AddDays(26),
                IsFlexibleDates = false,
                Latitude = -27.4930,
                Longitude = 153.2340,
                SuburbName = "Wellington Point",
                State = AustralianState.QLD,
                Postcode = "4160",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("HVAC Technician"),
                Title = "12kW ducted AC supply/return upgrade and zoning",
                Description = "Existing ducted system needs re-zoning (day/night) and new supply/return grilles to improve balance. Single-storey with roof access. Please assess existing 12kW unit and recommend duct/zone layout.",
                Status = JobStatus.Open,
                BudgetMin = 5200,
                BudgetMax = 7600,
                PreferredStartDate = DateTime.UtcNow.AddDays(9),
                PreferredEndDate = DateTime.UtcNow.AddDays(18),
                IsFlexibleDates = true,
                Latitude = -33.7030,
                Longitude = 151.1000,
                SuburbName = "Hornsby",
                State = AustralianState.NSW,
                Postcode = "2077",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("HVAC Technician"),
                Title = "Install 7kW split system in master suite",
                Description = "Back-to-back install of 7kW split system on first floor. Short pipe run, switchboard upgraded last year. Include condensate pump, wall bracket, and compliance paperwork.",
                Status = JobStatus.Open,
                BudgetMin = 2400,
                BudgetMax = 3200,
                PreferredStartDate = DateTime.UtcNow.AddDays(6),
                PreferredEndDate = DateTime.UtcNow.AddDays(14),
                IsFlexibleDates = false,
                Latitude = -33.8140,
                Longitude = 151.0010,
                SuburbName = "Parramatta",
                State = AustralianState.NSW,
                Postcode = "2150",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Landscaper"),
                Title = "Front yard makeover with native planting and lighting",
                Description = "Design and install low-maintenance native garden with stepping pavers, drip irrigation, and warm LED garden lighting. Area ~45m². Include soil prep and mulch.",
                Status = JobStatus.Open,
                BudgetMin = 8200,
                BudgetMax = 11200,
                PreferredStartDate = DateTime.UtcNow.AddDays(16),
                PreferredEndDate = DateTime.UtcNow.AddDays(30),
                IsFlexibleDates = true,
                Latitude = -34.9806,
                Longitude = 138.5156,
                SuburbName = "Glenelg",
                State = AustralianState.SA,
                Postcode = "5045",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Landscaper"),
                Title = "Backyard retaining wall and turf refresh",
                Description = "Replace failing timber sleepers with 800mm high concrete sleeper wall (18m), install ag drain, and re-turf 70m² with Sir Walter DNA. Access via side gate.",
                Status = JobStatus.Open,
                BudgetMin = 12500,
                BudgetMax = 16500,
                PreferredStartDate = DateTime.UtcNow.AddDays(19),
                PreferredEndDate = DateTime.UtcNow.AddDays(33),
                IsFlexibleDates = false,
                Latitude = -34.9220,
                Longitude = 138.6400,
                SuburbName = "Norwood",
                State = AustralianState.SA,
                Postcode = "5067",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Locksmith"),
                Title = "Rekey entire house and add deadbolts to rear doors",
                Description = "Rekey 5 external doors to single key profile, install two double-deadbolts on rear entries, and replace keyed-alike window locks (x8). Existing doors timber with standard prep.",
                Status = JobStatus.Open,
                BudgetMin = 950,
                BudgetMax = 1450,
                PreferredStartDate = DateTime.UtcNow.AddDays(4),
                PreferredEndDate = DateTime.UtcNow.AddDays(10),
                IsFlexibleDates = true,
                Latitude = -27.3830,
                Longitude = 153.0300,
                SuburbName = "Chermside",
                State = AustralianState.QLD,
                Postcode = "4032",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Locksmith"),
                Title = "Commercial glass door closer and access control update",
                Description = "Supply/install new heavy-duty door closer on aluminium entry door, replace two keypad readers with compatible models, and reprogram 12 fobs. Office trades during business hours.",
                Status = JobStatus.Open,
                BudgetMin = 1800,
                BudgetMax = 2600,
                PreferredStartDate = DateTime.UtcNow.AddDays(11),
                PreferredEndDate = DateTime.UtcNow.AddDays(18),
                IsFlexibleDates = false,
                Latitude = -27.6500,
                Longitude = 152.9210,
                SuburbName = "Springfield Lakes",
                State = AustralianState.QLD,
                Postcode = "4300",
                PropertyType = PropertyType.Commercial
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Glazier"),
                Title = "Replace fogged double-glazed units in townhouse",
                Description = "Measure and replace 4 x 1200x900 double-glazed units (clear, Low-E) in existing aluminium frames. Two units upper level. Include disposal of failed glass.",
                Status = JobStatus.Open,
                BudgetMin = 1800,
                BudgetMax = 2600,
                PreferredStartDate = DateTime.UtcNow.AddDays(13),
                PreferredEndDate = DateTime.UtcNow.AddDays(22),
                IsFlexibleDates = true,
                Latitude = -32.0830,
                Longitude = 115.9180,
                SuburbName = "Canning Vale",
                State = AustralianState.WA,
                Postcode = "6155",
                PropertyType = PropertyType.Townhouse
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Glazier"),
                Title = "Install frameless shower screen with black hardware",
                Description = "Supply and install 10mm frameless hinged shower screen (2100H) with matte black hardware. Opening 1100mm, fixed panel + door. Measure and template required.",
                Status = JobStatus.Open,
                BudgetMin = 1350,
                BudgetMax = 1850,
                PreferredStartDate = DateTime.UtcNow.AddDays(7),
                PreferredEndDate = DateTime.UtcNow.AddDays(14),
                IsFlexibleDates = false,
                Latitude = -32.0560,
                Longitude = 115.7430,
                SuburbName = "Fremantle",
                State = AustralianState.WA,
                Postcode = "6160",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Concreter"),
                Title = "Exposed aggregate driveway replacement (45m²)",
                Description = "Remove cracked concrete driveway, install 100mm slab with SL72 mesh, control joints, and charcoal exposed aggregate finish. Slight fall to street. Include excavation and spoil removal.",
                Status = JobStatus.Open,
                BudgetMin = 8800,
                BudgetMax = 11800,
                PreferredStartDate = DateTime.UtcNow.AddDays(12),
                PreferredEndDate = DateTime.UtcNow.AddDays(24),
                IsFlexibleDates = true,
                Latitude = -33.8170,
                Longitude = 151.1060,
                SuburbName = "Ryde",
                State = AustralianState.NSW,
                Postcode = "2112",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Concreter"),
                Title = "Slab for garden studio with service conduits",
                Description = "Pour 3m x 4m 100mm reinforced slab with thickened edge beams for future garden studio. Include moisture barrier, mesh, and 2x conduits for power/data. Access via side gate.",
                Status = JobStatus.Open,
                BudgetMin = 4200,
                BudgetMax = 6200,
                PreferredStartDate = DateTime.UtcNow.AddDays(9),
                PreferredEndDate = DateTime.UtcNow.AddDays(18),
                IsFlexibleDates = false,
                Latitude = -33.7960,
                Longitude = 151.1830,
                SuburbName = "Chatswood",
                State = AustralianState.NSW,
                Postcode = "2067",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Fencer"),
                Title = "Colorbond boundary fence with pedestrian gate",
                Description = "Install 25m of 1.8m Colorbond fence with plinths plus 1m pedestrian gate to side access. Remove existing timber fence. Slight slope over length.",
                Status = JobStatus.Open,
                BudgetMin = 5200,
                BudgetMax = 7200,
                PreferredStartDate = DateTime.UtcNow.AddDays(10),
                PreferredEndDate = DateTime.UtcNow.AddDays(20),
                IsFlexibleDates = true,
                Latitude = -37.6480,
                Longitude = 145.0290,
                SuburbName = "Epping",
                State = AustralianState.VIC,
                Postcode = "3076",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Fencer"),
                Title = "Timber paling fence replacement including sleepers",
                Description = "Replace 18m shared boundary with 1.8m treated pine paling fence, capping, and 200mm concrete sleeper retaining to match neighbor's level. Remove old fence and cart away.",
                Status = JobStatus.Open,
                BudgetMin = 4200,
                BudgetMax = 5600,
                PreferredStartDate = DateTime.UtcNow.AddDays(6),
                PreferredEndDate = DateTime.UtcNow.AddDays(14),
                IsFlexibleDates = false,
                Latitude = -33.6610,
                Longitude = 150.8660,
                SuburbName = "Box Hill",
                State = AustralianState.NSW,
                Postcode = "2765",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Plasterer"),
                Title = "Skim and patch living room after electrical rewire",
                Description = "Set and skim patched chases/holes from rewire, square set two openings, and repair 3 minor ceiling cracks. Room ~28m². Finish ready for paint.",
                Status = JobStatus.Open,
                BudgetMin = 1200,
                BudgetMax = 1800,
                PreferredStartDate = DateTime.UtcNow.AddDays(5),
                PreferredEndDate = DateTime.UtcNow.AddDays(10),
                IsFlexibleDates = true,
                Latitude = -33.9010,
                Longitude = 151.1950,
                SuburbName = "Alexandria",
                State = AustralianState.NSW,
                Postcode = "2015",
                PropertyType = PropertyType.House
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Plasterer"),
                Title = "Install suspended ceiling grid in small office",
                Description = "Supply/install 1200x600 suspended grid ceiling with acoustic tiles over 40m² office. Allow for 6 LED panel cutouts (by electrician) and perimeter angles. Clear access, 2.7m height.",
                Status = JobStatus.Open,
                BudgetMin = 3600,
                BudgetMax = 5200,
                PreferredStartDate = DateTime.UtcNow.AddDays(14),
                PreferredEndDate = DateTime.UtcNow.AddDays(24),
                IsFlexibleDates = false,
                Latitude = -33.8840,
                Longitude = 151.1580,
                SuburbName = "Leichhardt",
                State = AustralianState.NSW,
                Postcode = "2040",
                PropertyType = PropertyType.Commercial
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Cleaner"),
                Title = "End-of-lease clean for 3-bed apartment",
                Description = "Deep clean 3-bed, 2-bath unit with balcony: oven, cooktop, rangehood, windows (internal), skirting, grout touch-ups, and carpet steam clean (3 rooms). Lift access, parking available.",
                Status = JobStatus.Open,
                BudgetMin = 520,
                BudgetMax = 780,
                PreferredStartDate = DateTime.UtcNow.AddDays(3),
                PreferredEndDate = DateTime.UtcNow.AddDays(6),
                IsFlexibleDates = true,
                Latitude = -33.9070,
                Longitude = 151.2050,
                SuburbName = "Zetland",
                State = AustralianState.NSW,
                Postcode = "2017",
                PropertyType = PropertyType.Unit
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TradeCategoryId = Cat("Cleaner"),
                Title = "Post-renovation builder clean for 2-storey home",
                Description = "Full post-reno clean: high dusting, window tracks, glass balustrade, kitchen/bath detail, floor scrub/steam, and debris removal from surfaces. 4 bed, 3 bath. Power/water on.",
                Status = JobStatus.Open,
                BudgetMin = 1100,
                BudgetMax = 1500,
                PreferredStartDate = DateTime.UtcNow.AddDays(7),
                PreferredEndDate = DateTime.UtcNow.AddDays(12),
                IsFlexibleDates = false,
                Latitude = -33.7130,
                Longitude = 150.9510,
                SuburbName = "Kellyville",
                State = AustralianState.NSW,
                Postcode = "2155",
                PropertyType = PropertyType.House
            }
        };

        foreach (var job in jobs)
        {
            job.CreatedAt = seededAt;
            job.UpdatedAt = seededAt;
        }

        context.Jobs.AddRange(jobs);
        context.SaveChanges();
        Console.WriteLine($"Seeded {jobs.Count} showcase jobs across trade categories.");
    }

    // Seed demo tradies for testing Smart Leads, My Quotes, etc.
    var demoTradieEmail1 = "demo.tradie1@quote.local";
    var demoTradieEmail2 = "demo.tradie2@quote.local";
    var demoTradieEmail3 = "demo.tradie3@quote.local";

    var demoTradie1 = context.Users.FirstOrDefault(u => u.Email == demoTradieEmail1);
    var demoTradie2 = context.Users.FirstOrDefault(u => u.Email == demoTradieEmail2);
    var demoTradie3 = context.Users.FirstOrDefault(u => u.Email == demoTradieEmail3);

    if (demoTradie1 is null)
    {
        demoTradie1 = new User
        {
            Id = Guid.NewGuid(),
            Email = demoTradieEmail1,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("QuoteDemo123!"),
            UserType = UserType.Tradie,
            FirstName = "Mike",
            LastName = "Thompson",
            Phone = "0423 456 789",
            ABN = "12345678901",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(demoTradie1);
        context.SaveChanges();
        Console.WriteLine("Seeded demo tradie 1: Mike Thompson (Electrician/Plumber)");
    }

    if (demoTradie2 is null)
    {
        demoTradie2 = new User
        {
            Id = Guid.NewGuid(),
            Email = demoTradieEmail2,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("QuoteDemo123!"),
            UserType = UserType.Tradie,
            FirstName = "Sarah",
            LastName = "Chen",
            Phone = "0434 567 890",
            ABN = "23456789012",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(demoTradie2);
        context.SaveChanges();
        Console.WriteLine("Seeded demo tradie 2: Sarah Chen (Builder/Carpenter)");
    }

    if (demoTradie3 is null)
    {
        demoTradie3 = new User
        {
            Id = Guid.NewGuid(),
            Email = demoTradieEmail3,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("QuoteDemo123!"),
            UserType = UserType.Tradie,
            FirstName = "James",
            LastName = "Wilson",
            Phone = "0445 678 901",
            ABN = "34567890123",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(demoTradie3);
        context.SaveChanges();
        Console.WriteLine("Seeded demo tradie 3: James Wilson (Painter/Tiler)");
    }

    // Seed TradieProfiles if they don't exist
    if (!context.TradieProfiles.Any(tp => tp.UserId == demoTradie1.Id))
    {
        var profile1 = new TradieProfile
        {
            Id = Guid.NewGuid(),
            UserId = demoTradie1.Id,
            BusinessName = "Thompson Electrical & Plumbing",
            Bio = "Licensed electrician and plumber with 15+ years experience in residential and commercial work across Sydney.",
            HourlyRate = 95m,
            ServiceRadiusKm = 35,
            Latitude = -33.8688,  // Sydney CBD
            Longitude = 151.2093,
            InsuranceVerified = true,
            InsuranceExpiryDate = DateTime.UtcNow.AddYears(1),
            PoliceCheckVerified = true,
            Rating = 4.8m,
            TotalJobsCompleted = 247,
            TotalReviews = 189,
            AverageResponseTimeHours = 2.5,
            CompletionRate = 0.98,
            IsAvailableNow = true,
            AvailableNowUntil = DateTime.UtcNow.AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TradieProfiles.Add(profile1);
        context.SaveChanges();

        // Add licences for Tradie 1
        var categories = context.TradeCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        context.TradieLicences.AddRange(new[]
        {
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile1.Id,
                TradeCategoryId = categories["Electrician"],
                LicenceNumber = "EC123456",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile1.Id,
                TradeCategoryId = categories["Plumber"],
                LicenceNumber = "PL789012",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
        context.SaveChanges();
    }

    if (!context.TradieProfiles.Any(tp => tp.UserId == demoTradie2.Id))
    {
        var profile2 = new TradieProfile
        {
            Id = Guid.NewGuid(),
            UserId = demoTradie2.Id,
            BusinessName = "Chen Construction & Carpentry",
            Bio = "Quality building and carpentry services. Specializing in renovations, extensions, and custom timber work.",
            HourlyRate = 110m,
            ServiceRadiusKm = 40,
            Latitude = -33.7960,  // Chatswood area
            Longitude = 151.1830,
            InsuranceVerified = true,
            InsuranceExpiryDate = DateTime.UtcNow.AddYears(1),
            PoliceCheckVerified = true,
            Rating = 4.9m,
            TotalJobsCompleted = 156,
            TotalReviews = 134,
            AverageResponseTimeHours = 1.8,
            CompletionRate = 0.99,
            IsAvailableNow = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TradieProfiles.Add(profile2);
        context.SaveChanges();

        var categories = context.TradeCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        context.TradieLicences.AddRange(new[]
        {
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile2.Id,
                TradeCategoryId = categories["Builder"],
                LicenceNumber = "BLD345678",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(3),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile2.Id,
                TradeCategoryId = categories["Carpenter"],
                LicenceNumber = "CARP901234",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(3),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
        context.SaveChanges();
    }

    if (!context.TradieProfiles.Any(tp => tp.UserId == demoTradie3.Id))
    {
        var profile3 = new TradieProfile
        {
            Id = Guid.NewGuid(),
            UserId = demoTradie3.Id,
            BusinessName = "Wilson Painting & Tiling",
            Bio = "Professional painting and tiling services. From interior refreshes to full bathroom renovations.",
            HourlyRate = 85m,
            ServiceRadiusKm = 30,
            Latitude = -33.9010,  // Alexandria area
            Longitude = 151.1950,
            InsuranceVerified = true,
            InsuranceExpiryDate = DateTime.UtcNow.AddMonths(8),
            PoliceCheckVerified = true,
            Rating = 4.7m,
            TotalJobsCompleted = 312,
            TotalReviews = 278,
            AverageResponseTimeHours = 3.2,
            CompletionRate = 0.96,
            IsAvailableNow = true,
            AvailableNowUntil = DateTime.UtcNow.AddHours(4),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TradieProfiles.Add(profile3);
        context.SaveChanges();

        var categories = context.TradeCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        context.TradieLicences.AddRange(new[]
        {
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile3.Id,
                TradeCategoryId = categories["Painter"],
                LicenceNumber = "PAINT567890",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TradieLicence
            {
                Id = Guid.NewGuid(),
                TradieProfileId = profile3.Id,
                TradeCategoryId = categories["Tiler"],
                LicenceNumber = "TILE123456",
                LicenceState = AustralianState.NSW,
                IssuingAuthority = "NSW Fair Trading",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationStatus = VerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
        context.SaveChanges();
    }

    // Seed tradie profile for rorieokw@gmail.com (user's personal account)
    var userEmail = "rorieokw@gmail.com";
    // Debug: List all users in database
    var allUsers = context.Users.Select(u => u.Email).ToList();
    Console.WriteLine($"Users in database: {string.Join(", ", allUsers)}");

    var userTradie = context.Users.FirstOrDefault(u => u.Email.ToLower() == userEmail.ToLower());
    TradieProfile? userTradieProfile = null;

    if (userTradie is not null)
    {
        // Ensure user type is Tradie
        if (userTradie.UserType != UserType.Tradie)
        {
            userTradie.UserType = UserType.Tradie;
            context.SaveChanges();
            Console.WriteLine($"Updated {userEmail} to Tradie user type.");
        }

        // Create or UPDATE TradieProfile with proper location data
        userTradieProfile = context.TradieProfiles.FirstOrDefault(tp => tp.UserId == userTradie.Id);
        if (userTradieProfile is null)
        {
            userTradieProfile = new TradieProfile
            {
                Id = Guid.NewGuid(),
                UserId = userTradie.Id,
                BusinessName = "Rorie's Trade Services",
                Bio = "Full-service tradie covering electrical, plumbing, building, carpentry, painting and tiling. Licensed and insured.",
                HourlyRate = 100m,
                ServiceRadiusKm = 50,  // Large radius to see all jobs
                Latitude = -33.8688,   // Sydney CBD
                Longitude = 151.2093,
                InsuranceVerified = true,
                InsuranceExpiryDate = DateTime.UtcNow.AddYears(1),
                PoliceCheckVerified = true,
                Rating = 4.9m,
                TotalJobsCompleted = 150,
                TotalReviews = 120,
                AverageResponseTimeHours = 1.5,
                CompletionRate = 0.98,
                IsAvailableNow = true,
                AvailableNowUntil = DateTime.UtcNow.AddHours(10),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.TradieProfiles.Add(userTradieProfile);
            context.SaveChanges();
            Console.WriteLine($"Created TradieProfile for {userEmail}.");
        }
        else
        {
            // UPDATE existing profile with proper location and stats (registration creates with defaults)
            userTradieProfile.BusinessName = string.IsNullOrEmpty(userTradieProfile.BusinessName) ? "Rorie's Trade Services" : userTradieProfile.BusinessName;
            userTradieProfile.Bio = string.IsNullOrEmpty(userTradieProfile.Bio) ? "Full-service tradie covering electrical, plumbing, building, carpentry, painting and tiling. Licensed and insured." : userTradieProfile.Bio;
            userTradieProfile.HourlyRate = userTradieProfile.HourlyRate == 0 ? 100m : userTradieProfile.HourlyRate;
            userTradieProfile.ServiceRadiusKm = 50;  // Large radius to see all jobs
            userTradieProfile.Latitude = -33.8688;   // Sydney CBD
            userTradieProfile.Longitude = 151.2093;
            userTradieProfile.InsuranceVerified = true;
            userTradieProfile.InsuranceExpiryDate = DateTime.UtcNow.AddYears(1);
            userTradieProfile.PoliceCheckVerified = true;
            userTradieProfile.Rating = 4.9m;
            userTradieProfile.TotalJobsCompleted = 150;
            userTradieProfile.TotalReviews = 120;
            userTradieProfile.AverageResponseTimeHours = 1.5;
            userTradieProfile.CompletionRate = 0.98;
            userTradieProfile.IsAvailableNow = true;
            userTradieProfile.AvailableNowUntil = DateTime.UtcNow.AddHours(10);
            userTradieProfile.UpdatedAt = DateTime.UtcNow;
            context.SaveChanges();
            Console.WriteLine($"UPDATED TradieProfile for {userEmail} with Sydney location (lat: -33.8688, lng: 151.2093) and 50km service radius.");
        }

        // Add licences for ALL categories so user can see all job types
        var existingLicences = context.TradieLicences.Where(l => l.TradieProfileId == userTradieProfile.Id).ToList();
        if (existingLicences.Count == 0)
        {
            var categories = context.TradeCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var licencesToAdd = new List<TradieLicence>();

            var licenceConfigs = new[]
            {
                ("Electrician", "EC-USER-001"),
                ("Plumber", "PL-USER-002"),
                ("Builder", "BLD-USER-003"),
                ("Carpenter", "CARP-USER-004"),
                ("Painter", "PAINT-USER-005"),
                ("Tiler", "TILE-USER-006")
            };

            foreach (var (categoryName, licenceNumber) in licenceConfigs)
            {
                if (categories.TryGetValue(categoryName, out var categoryId))
                {
                    licencesToAdd.Add(new TradieLicence
                    {
                        Id = Guid.NewGuid(),
                        TradieProfileId = userTradieProfile.Id,
                        TradeCategoryId = categoryId,
                        LicenceNumber = licenceNumber,
                        LicenceState = AustralianState.NSW,
                        IssuingAuthority = "NSW Fair Trading",
                        ExpiryDate = DateTime.UtcNow.AddYears(2),
                        VerificationStatus = VerificationStatus.Verified,
                        VerifiedAt = DateTime.UtcNow.AddDays(-30),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            context.TradieLicences.AddRange(licencesToAdd);
            context.SaveChanges();
            Console.WriteLine($"Added {licencesToAdd.Count} licences for {userEmail}.");
        }
    }
    else
    {
        Console.WriteLine($"User {userEmail} not found - register and log in first, then restart API.");
    }

    // Seed CustomerQuality for demo customer (if table exists)
    try
    {
        if (!context.CustomerQualities.Any(cq => cq.CustomerId == demoCustomer.Id))
        {
            context.CustomerQualities.Add(new CustomerQuality
            {
                Id = Guid.NewGuid(),
                CustomerId = demoCustomer.Id,
                TotalJobsPosted = 40,
                JobsCompleted = 32,
                JobsCancelled = 2,
                AverageJobValue = 6500m,
                PaymentReliabilityScore = 0.95m,
                AverageResponseTimeHours = 4.5,
                TotalReviewsGiven = 28,
                AverageRatingGiven = 4.6m,
                LastCalculatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            Console.WriteLine("Seeded CustomerQuality for demo customer.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Skipped CustomerQuality seeding: {ex.Message}");
    }

    // Seed JobQuotes for user's account (if exists and has no quotes yet)
    if (userTradie is not null && !context.Quotes.Any(q => q.TradieId == userTradie.Id))
    {
        var allJobs = context.Jobs.Where(j => j.Status == JobStatus.Open).Take(15).ToList();
        var categories = context.TradeCategories.ToDictionary(c => c.Id, c => c.Name);
        var userQuotes = new List<JobQuote>();
        var random = new Random(42);

        foreach (var job in allJobs)
        {
            var categoryName = categories[job.TradeCategoryId];
            var budgetMid = ((job.BudgetMin ?? 1000) + (job.BudgetMax ?? 5000)) / 2;
            var variation = (decimal)(random.NextDouble() * 0.3 - 0.15);
            var totalCost = Math.Round(budgetMid * (1 + variation), 2);
            var labourRatio = 0.6m + (decimal)(random.NextDouble() * 0.2);

            userQuotes.Add(new JobQuote
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                TradieId = userTradie.Id,
                Status = QuoteStatus.Pending,
                LabourCost = Math.Round(totalCost * labourRatio, 2),
                MaterialsCost = Math.Round(totalCost * (1 - labourRatio), 2),
                EstimatedDurationHours = random.Next(4, 40),
                ProposedStartDate = job.PreferredStartDate?.AddDays(random.Next(-2, 5)),
                Notes = $"Happy to provide a competitive quote for this {categoryName.ToLower()} work. I have extensive experience with similar projects in the area.",
                ValidUntil = DateTime.UtcNow.AddDays(14),
                ViewCount = random.Next(0, 5),
                FirstViewedAt = random.NextDouble() > 0.3 ? DateTime.UtcNow.AddDays(-random.Next(1, 7)) : null,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 10)),
                UpdatedAt = DateTime.UtcNow
            });
        }

        context.Quotes.AddRange(userQuotes);
        context.SaveChanges();
        Console.WriteLine($"Seeded {userQuotes.Count} job quotes for {userEmail}.");
    }

    // Seed JobQuotes for demo tradies if none exist
    if (!context.Quotes.Any(q => q.TradieId == demoTradie1.Id || q.TradieId == demoTradie2.Id || q.TradieId == demoTradie3.Id))
    {
        var allJobs = context.Jobs.Where(j => j.Status == JobStatus.Open).Take(20).ToList();
        var categories = context.TradeCategories.ToDictionary(c => c.Id, c => c.Name);
        var quotes = new List<JobQuote>();
        var random = new Random(42); // Fixed seed for reproducibility

        foreach (var job in allJobs)
        {
            var categoryName = categories[job.TradeCategoryId];

            // Determine which tradies can quote on this job based on their licences
            var eligibleTradies = new List<User>();

            if (categoryName is "Electrician" or "Plumber")
                eligibleTradies.Add(demoTradie1);
            if (categoryName is "Builder" or "Carpenter")
                eligibleTradies.Add(demoTradie2);
            if (categoryName is "Painter" or "Tiler")
                eligibleTradies.Add(demoTradie3);

            foreach (var tradie in eligibleTradies)
            {
                // Random chance to quote (70%)
                if (random.NextDouble() > 0.3)
                {
                    var budgetMid = ((job.BudgetMin ?? 1000) + (job.BudgetMax ?? 5000)) / 2;
                    var variation = (decimal)(random.NextDouble() * 0.3 - 0.15); // -15% to +15%
                    var totalCost = Math.Round(budgetMid * (1 + variation), 2);
                    var labourRatio = 0.6m + (decimal)(random.NextDouble() * 0.2); // 60-80% labour

                    quotes.Add(new JobQuote
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        TradieId = tradie.Id,
                        Status = QuoteStatus.Pending,
                        LabourCost = Math.Round(totalCost * labourRatio, 2),
                        MaterialsCost = Math.Round(totalCost * (1 - labourRatio), 2),
                        EstimatedDurationHours = random.Next(4, 40),
                        ProposedStartDate = job.PreferredStartDate?.AddDays(random.Next(-2, 5)),
                        Notes = $"Happy to provide a competitive quote for this {categoryName.ToLower()} work. I have extensive experience with similar projects in the area.",
                        ValidUntil = DateTime.UtcNow.AddDays(14),
                        ViewCount = random.Next(0, 5),
                        FirstViewedAt = random.NextDouble() > 0.3 ? DateTime.UtcNow.AddDays(-random.Next(1, 7)) : null,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 10)),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        context.Quotes.AddRange(quotes);
        context.SaveChanges();
        Console.WriteLine($"Seeded {quotes.Count} job quotes from demo tradies.");
    }

    // Seed LeadScores for user's account (if exists and has no lead scores yet)
    try
    {
        if (userTradie is not null && userTradieProfile is not null && !context.LeadScores.Any(ls => ls.TradieId == userTradie.Id))
        {
            var openJobs = context.Jobs.Where(j => j.Status == JobStatus.Open).ToList();
            var userLeadScores = new List<LeadScore>();
            var random = new Random(42);

            foreach (var job in openJobs)
            {
                // Calculate distance (simplified)
                var latDiff = Math.Abs(job.Latitude - userTradieProfile.Latitude);
                var lonDiff = Math.Abs(job.Longitude - userTradieProfile.Longitude);
                var distanceKm = Math.Sqrt(latDiff * latDiff + lonDiff * lonDiff) * 111;

                // User has all licences, so always licensed
                var distanceScore = Math.Max(0, 25 - (int)(distanceKm / 2));
                var budgetMatchScore = random.Next(18, 25);
                var skillMatchScore = random.Next(20, 25); // Always high since user is licensed for all
                var customerQualityScore = random.Next(18, 25);
                var urgencyScore = job.PreferredStartDate.HasValue && job.PreferredStartDate.Value < DateTime.UtcNow.AddDays(7)
                    ? random.Next(20, 25) : random.Next(12, 20);

                var totalScore = distanceScore + budgetMatchScore + skillMatchScore + customerQualityScore + urgencyScore;

                userLeadScores.Add(new LeadScore
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    TradieId = userTradie.Id,
                    TotalScore = totalScore,
                    DistanceScore = distanceScore,
                    BudgetMatchScore = budgetMatchScore,
                    SkillMatchScore = skillMatchScore,
                    CustomerQualityScore = customerQualityScore,
                    UrgencyScore = urgencyScore,
                    DistanceKm = Math.Round(distanceKm, 1),
                    CalculatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            context.LeadScores.AddRange(userLeadScores);
            context.SaveChanges();
            Console.WriteLine($"Seeded {userLeadScores.Count} lead scores for {userEmail}.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Skipped user LeadScores seeding: {ex.Message}");
    }

    // Seed LeadScores for demo tradies (if table exists)
    try
    {
        if (!context.LeadScores.Any(ls => ls.TradieId == demoTradie1.Id || ls.TradieId == demoTradie2.Id || ls.TradieId == demoTradie3.Id))
        {
            var openJobs = context.Jobs.Where(j => j.Status == JobStatus.Open).ToList();
            var tradies = new[] { demoTradie1, demoTradie2, demoTradie3 };
            var profiles = context.TradieProfiles.ToDictionary(tp => tp.UserId);
            var categories = context.TradeCategories.ToDictionary(c => c.Id, c => c.Name);
            var leadScores = new List<LeadScore>();
            var random = new Random(42);

            foreach (var job in openJobs)
            {
                var categoryName = categories[job.TradeCategoryId];

                foreach (var tradie in tradies)
                {
                    if (!profiles.TryGetValue(tradie.Id, out var profile)) continue;

                    // Calculate distance (simplified Haversine approximation)
                    var latDiff = Math.Abs(job.Latitude - profile.Latitude);
                    var lonDiff = Math.Abs(job.Longitude - profile.Longitude);
                    var distanceKm = Math.Sqrt(latDiff * latDiff + lonDiff * lonDiff) * 111; // Rough km conversion

                    // Skip if outside service radius
                    if (distanceKm > profile.ServiceRadiusKm) continue;

                    // Check if tradie is licensed for this category
                    var isLicensed = categoryName switch
                    {
                        "Electrician" or "Plumber" => tradie.Id == demoTradie1.Id,
                        "Builder" or "Carpenter" => tradie.Id == demoTradie2.Id,
                        "Painter" or "Tiler" => tradie.Id == demoTradie3.Id,
                        _ => false
                    };

                    // Calculate scores
                    var distanceScore = Math.Max(0, 25 - (int)(distanceKm / 2)); // Max 25 points
                    var budgetMatchScore = random.Next(15, 25); // 15-25 points
                    var skillMatchScore = isLicensed ? random.Next(20, 25) : random.Next(5, 15); // Max 25 points
                    var customerQualityScore = random.Next(15, 25); // 15-25 points
                    var urgencyScore = job.PreferredStartDate.HasValue && job.PreferredStartDate.Value < DateTime.UtcNow.AddDays(7)
                        ? random.Next(18, 25) : random.Next(10, 18);

                    var totalScore = distanceScore + budgetMatchScore + skillMatchScore + customerQualityScore + urgencyScore;

                    leadScores.Add(new LeadScore
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        TradieId = tradie.Id,
                        TotalScore = totalScore,
                        DistanceScore = distanceScore,
                        BudgetMatchScore = budgetMatchScore,
                        SkillMatchScore = skillMatchScore,
                        CustomerQualityScore = customerQualityScore,
                        UrgencyScore = urgencyScore,
                        DistanceKm = Math.Round(distanceKm, 1),
                        CalculatedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            context.LeadScores.AddRange(leadScores);
            context.SaveChanges();
            Console.WriteLine($"Seeded {leadScores.Count} lead scores for demo tradies.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Skipped demo LeadScores seeding: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Quote API v1");
    });
}
else
{
    // Only force HTTPS outside development to avoid local cert issues when using HTTP
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");

// Serve uploaded verification documents
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
