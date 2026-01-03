using Microsoft.EntityFrameworkCore;
using Quote.Domain.Entities;

namespace Quote.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TradieProfile> TradieProfiles { get; }
    DbSet<TradeCategory> TradeCategories { get; }
    DbSet<TradieLicence> TradieLicences { get; }
    DbSet<Job> Jobs { get; }
    DbSet<JobMedia> JobMedia { get; }
    DbSet<JobQuote> Quotes { get; }
    DbSet<QuoteMaterial> QuoteMaterials { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationParticipant> ConversationParticipants { get; }
    DbSet<Message> Messages { get; }
    DbSet<Review> Reviews { get; }

    // New entities for tradie features
    DbSet<QuoteTemplate> QuoteTemplates { get; }
    DbSet<QuoteTemplateMaterial> QuoteTemplateMaterials { get; }
    DbSet<QuoteView> QuoteViews { get; }
    DbSet<BlockedSuburb> BlockedSuburbs { get; }
    DbSet<PortfolioItem> PortfolioItems { get; }
    DbSet<PortfolioMedia> PortfolioMedia { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<JobAssignment> JobAssignments { get; }
    DbSet<Deposit> Deposits { get; }
    DbSet<PhotoAnnotation> PhotoAnnotations { get; }
    DbSet<MaterialEstimateTemplate> MaterialEstimateTemplates { get; }
    DbSet<CalendarIntegration> CalendarIntegrations { get; }

    // Lead Scoring entities
    DbSet<LeadScore> LeadScores { get; }
    DbSet<CustomerQuality> CustomerQualities { get; }

    // Scheduling entities
    DbSet<ScheduleEvent> ScheduleEvents { get; }

    // Invoice entities
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<InvoicePayment> InvoicePayments { get; }

    // Stripe/Payment entities
    DbSet<StripeAccount> StripeAccounts { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }

    // Recurring Jobs
    DbSet<RecurringJobTemplate> RecurringJobTemplates { get; }

    // Material Bundles
    DbSet<MaterialBundle> MaterialBundles { get; }
    DbSet<MaterialBundleItem> MaterialBundleItems { get; }

    // Verification
    DbSet<IdentityVerification> IdentityVerifications { get; }

    // Disputes
    DbSet<Dispute> Disputes { get; }
    DbSet<DisputeEvidence> DisputeEvidence { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
