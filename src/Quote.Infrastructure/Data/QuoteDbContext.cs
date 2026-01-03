using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data;

public class QuoteDbContext : DbContext, IApplicationDbContext
{
    public QuoteDbContext(DbContextOptions<QuoteDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TradieProfile> TradieProfiles => Set<TradieProfile>();
    public DbSet<TradeCategory> TradeCategories => Set<TradeCategory>();
    public DbSet<TradieLicence> TradieLicences => Set<TradieLicence>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobMedia> JobMedia => Set<JobMedia>();
    public DbSet<JobQuote> Quotes => Set<JobQuote>();
    public DbSet<QuoteMaterial> QuoteMaterials => Set<QuoteMaterial>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();

    // New entities for tradie features
    public DbSet<QuoteTemplate> QuoteTemplates => Set<QuoteTemplate>();
    public DbSet<QuoteTemplateMaterial> QuoteTemplateMaterials => Set<QuoteTemplateMaterial>();
    public DbSet<QuoteView> QuoteViews => Set<QuoteView>();
    public DbSet<BlockedSuburb> BlockedSuburbs => Set<BlockedSuburb>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<PortfolioMedia> PortfolioMedia => Set<PortfolioMedia>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<JobAssignment> JobAssignments => Set<JobAssignment>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<PhotoAnnotation> PhotoAnnotations => Set<PhotoAnnotation>();
    public DbSet<MaterialEstimateTemplate> MaterialEstimateTemplates => Set<MaterialEstimateTemplate>();
    public DbSet<CalendarIntegration> CalendarIntegrations => Set<CalendarIntegration>();

    // Lead Scoring entities
    public DbSet<LeadScore> LeadScores => Set<LeadScore>();
    public DbSet<CustomerQuality> CustomerQualities => Set<CustomerQuality>();

    // Scheduling entities
    public DbSet<ScheduleEvent> ScheduleEvents => Set<ScheduleEvent>();

    // Invoice entities
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();

    // Stripe/Payment entities
    public DbSet<StripeAccount> StripeAccounts => Set<StripeAccount>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Recurring Jobs
    public DbSet<RecurringJobTemplate> RecurringJobTemplates => Set<RecurringJobTemplate>();

    // Material Bundles
    public DbSet<MaterialBundle> MaterialBundles => Set<MaterialBundle>();
    public DbSet<MaterialBundleItem> MaterialBundleItems => Set<MaterialBundleItem>();

    // Verification
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();

    // Disputes
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeEvidence> DisputeEvidence => Set<DisputeEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuoteDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
