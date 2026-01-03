using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quote.Application.Common.Interfaces;
using Quote.Application.Payments.Services;
using Quote.Domain.Entities;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;
using Stripe;

namespace Quote.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly decimal _platformFeePercentage;
    private readonly string _webhookSecret;

    public StripeService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<StripeService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        var secretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(secretKey))
        {
            StripeConfiguration.ApiKey = secretKey;
        }

        _platformFeePercentage = configuration.GetValue<decimal>("Stripe:PlatformFeePercentage", 5.0m);
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
    }

    #region Connected Accounts

    public async Task<ConnectedAccountResponse> CreateConnectedAccountAsync(Guid tradieId, CreateConnectedAccountRequest request, CancellationToken cancellationToken = default)
    {
        var existingAccount = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken);

        if (existingAccount != null)
        {
            return new ConnectedAccountResponse(
                existingAccount.Id,
                existingAccount.StripeAccountId,
                existingAccount.IsOnboardingComplete,
                existingAccount.ChargesEnabled,
                existingAccount.PayoutsEnabled,
                existingAccount.DetailsSubmitted,
                null
            );
        }

        var tradie = await _context.Users.FirstOrDefaultAsync(u => u.Id == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Tradie not found");

        // Create Stripe Connected Account
        var accountService = new AccountService();
        var accountOptions = new AccountCreateOptions
        {
            Type = "express",
            Country = request.Country,
            Email = request.Email ?? tradie.Email,
            BusinessType = request.BusinessType,
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            },
            BusinessProfile = new AccountBusinessProfileOptions
            {
                Mcc = "7349", // Building cleaning, maintenance
                ProductDescription = "Trade services"
            }
        };

        var stripeAccount = await accountService.CreateAsync(accountOptions, cancellationToken: cancellationToken);

        var account = new StripeAccount
        {
            Id = Guid.NewGuid(),
            TradieId = tradieId,
            StripeAccountId = stripeAccount.Id,
            IsOnboardingComplete = false,
            ChargesEnabled = stripeAccount.ChargesEnabled,
            PayoutsEnabled = stripeAccount.PayoutsEnabled,
            DetailsSubmitted = stripeAccount.DetailsSubmitted,
            DefaultCurrency = stripeAccount.DefaultCurrency,
            Country = stripeAccount.Country,
            BusinessType = stripeAccount.BusinessType,
            Email = stripeAccount.Email
        };

        _context.StripeAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return new ConnectedAccountResponse(
            account.Id,
            account.StripeAccountId,
            account.IsOnboardingComplete,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            null
        );
    }

    public async Task<OnboardingLinkResponse> GetOnboardingLinkAsync(Guid tradieId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Stripe account not found. Please create one first.");

        var linkService = new AccountLinkService();
        var linkOptions = new AccountLinkCreateOptions
        {
            Account = account.StripeAccountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding"
        };

        var link = await linkService.CreateAsync(linkOptions, cancellationToken: cancellationToken);

        return new OnboardingLinkResponse(
            link.Url,
            link.ExpiresAt
        );
    }

    public async Task<AccountStatusResponse?> GetAccountStatusAsync(Guid tradieId, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken);

        if (account == null)
            return null;

        return new AccountStatusResponse(
            account.Id,
            account.StripeAccountId,
            account.IsOnboardingComplete,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            account.DefaultCurrency,
            account.Country,
            account.OnboardingCompletedAt
        );
    }

    public async Task RefreshAccountStatusAsync(Guid tradieId, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Stripe account not found");

        var accountService = new AccountService();
        var stripeAccount = await accountService.GetAsync(account.StripeAccountId, cancellationToken: cancellationToken);

        account.ChargesEnabled = stripeAccount.ChargesEnabled;
        account.PayoutsEnabled = stripeAccount.PayoutsEnabled;
        account.DetailsSubmitted = stripeAccount.DetailsSubmitted;
        account.DefaultCurrency = stripeAccount.DefaultCurrency;

        if (stripeAccount.ChargesEnabled && stripeAccount.PayoutsEnabled && stripeAccount.DetailsSubmitted)
        {
            if (!account.IsOnboardingComplete)
            {
                account.IsOnboardingComplete = true;
                account.OnboardingCompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Payment Intents

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid customerId, CreatePaymentIntentRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found");

        Guid? tradieId = null;
        string? tradieStripeAccountId = null;
        string description = request.Description ?? "Quote Payment";

        // Determine tradie from invoice, quote, or milestone
        if (request.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Job)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value, cancellationToken);
            if (invoice != null)
            {
                tradieId = invoice.TradieId;
                description = $"Invoice {invoice.InvoiceNumber}";
            }
        }
        else if (request.QuoteId.HasValue)
        {
            var quote = await _context.Quotes
                .Include(q => q.Job)
                .FirstOrDefaultAsync(q => q.Id == request.QuoteId.Value, cancellationToken);
            if (quote != null)
            {
                tradieId = quote.TradieId;
                description = request.IsDeposit ? $"Deposit for {quote.Job.Title}" : $"Payment for {quote.Job.Title}";
            }
        }
        else if (request.MilestoneId.HasValue)
        {
            var milestone = await _context.Milestones
                .Include(m => m.Job)
                .FirstOrDefaultAsync(m => m.Id == request.MilestoneId.Value, cancellationToken);
            if (milestone != null)
            {
                tradieId = milestone.Job.Quotes?.FirstOrDefault()?.TradieId;
                description = $"Milestone: {milestone.Title}";
            }
        }

        // Get tradie's Stripe account if they have one
        if (tradieId.HasValue)
        {
            var tradieAccount = await _context.StripeAccounts
                .FirstOrDefaultAsync(s => s.TradieId == tradieId.Value && s.ChargesEnabled, cancellationToken);
            tradieStripeAccountId = tradieAccount?.StripeAccountId;
        }

        var amountInCents = (long)(request.Amount * 100);
        var platformFee = CalculatePlatformFee(request.Amount);
        var platformFeeInCents = (long)(platformFee * 100);

        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "aud",
            Description = description,
            ReceiptEmail = customer.Email,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "customer_id", customerId.ToString() },
                { "tradie_id", tradieId?.ToString() ?? "" },
                { "invoice_id", request.InvoiceId?.ToString() ?? "" },
                { "quote_id", request.QuoteId?.ToString() ?? "" },
                { "milestone_id", request.MilestoneId?.ToString() ?? "" },
                { "is_deposit", request.IsDeposit.ToString() }
            }
        };

        // If tradie has a connected account, set up transfer
        if (!string.IsNullOrEmpty(tradieStripeAccountId))
        {
            paymentIntentOptions.ApplicationFeeAmount = platformFeeInCents;
            paymentIntentOptions.TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = tradieStripeAccountId
            };
        }

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions, cancellationToken: cancellationToken);

        // Create transaction record
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TradieId = tradieId,
            JobId = request.QuoteId.HasValue
                ? (await _context.Quotes.FirstOrDefaultAsync(q => q.Id == request.QuoteId.Value, cancellationToken))?.JobId
                : request.InvoiceId.HasValue
                    ? (await _context.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value, cancellationToken))?.JobId
                    : request.MilestoneId.HasValue
                        ? (await _context.Milestones.FirstOrDefaultAsync(m => m.Id == request.MilestoneId.Value, cancellationToken))?.JobId
                        : null,
            QuoteId = request.QuoteId,
            InvoiceId = request.InvoiceId,
            MilestoneId = request.MilestoneId,
            StripePaymentIntentId = paymentIntent.Id,
            Amount = request.Amount,
            PlatformFee = platformFee,
            TradiePayout = request.Amount - platformFee,
            Currency = "aud",
            Status = paymentIntent.Status,
            Description = description,
            IsDeposit = request.IsDeposit,
            IsMilestonePayment = request.MilestoneId.HasValue,
            ReceiptEmail = customer.Email
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentIntentResponse(
            paymentIntent.ClientSecret,
            paymentIntent.Id,
            request.Amount,
            "aud",
            paymentIntent.Status
        );
    }

    public async Task<ConfirmPaymentResponse> ConfirmPaymentAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(request.PaymentIntentId, cancellationToken: cancellationToken);

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == request.PaymentIntentId, cancellationToken);

        if (transaction != null)
        {
            transaction.Status = paymentIntent.Status;
            transaction.StripeChargeId = paymentIntent.LatestChargeId;
            transaction.PaymentMethodType = paymentIntent.PaymentMethodTypes?.FirstOrDefault();

            if (paymentIntent.Status == "succeeded")
            {
                transaction.PaidAt = DateTime.UtcNow;

                // Update related entities
                if (transaction.InvoiceId.HasValue)
                {
                    var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == transaction.InvoiceId.Value, cancellationToken);
                    if (invoice != null)
                    {
                        invoice.AmountPaid += transaction.Amount;
                        if (invoice.AmountPaid >= invoice.TotalAmount)
                        {
                            invoice.Status = InvoiceStatus.Paid;
                            invoice.PaidAt = DateTime.UtcNow;
                        }
                        else
                        {
                            invoice.Status = InvoiceStatus.PartiallyPaid;
                        }
                    }
                }

                if (transaction.MilestoneId.HasValue)
                {
                    var milestone = await _context.Milestones.FirstOrDefaultAsync(m => m.Id == transaction.MilestoneId.Value, cancellationToken);
                    if (milestone != null)
                    {
                        milestone.IsPaid = true;
                        milestone.PaidAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ConfirmPaymentResponse(
            paymentIntent.Status == "succeeded",
            paymentIntent.Status,
            transaction?.Id,
            paymentIntent.LastPaymentError?.Message
        );
    }

    public async Task<PaymentTransaction?> GetTransactionByPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntentId, cancellationToken);
    }

    #endregion

    #region Deposits and Milestones

    public async Task<DepositResponse> ProcessDepositAsync(Guid customerId, ProcessDepositRequest request, CancellationToken cancellationToken = default)
    {
        var quote = await _context.Quotes
            .Include(q => q.Job)
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken)
            ?? throw new InvalidOperationException("Quote not found");

        var paymentIntentRequest = new CreatePaymentIntentRequest(
            InvoiceId: null,
            QuoteId: request.QuoteId,
            MilestoneId: null,
            Amount: request.Amount,
            IsDeposit: true,
            Description: $"Deposit for {quote.Job.Title}"
        );

        var paymentIntent = await CreatePaymentIntentAsync(customerId, paymentIntentRequest, cancellationToken);

        // Confirm with payment method
        var paymentIntentService = new PaymentIntentService();
        var confirmedIntent = await paymentIntentService.ConfirmAsync(paymentIntent.PaymentIntentId, new PaymentIntentConfirmOptions
        {
            PaymentMethod = request.PaymentMethodId
        }, cancellationToken: cancellationToken);

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.PaymentIntentId, cancellationToken);

        if (transaction != null && confirmedIntent.Status == "succeeded")
        {
            transaction.Status = "succeeded";
            transaction.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new DepositResponse(
            transaction?.Id ?? Guid.Empty,
            request.Amount,
            confirmedIntent.Status,
            DateTime.UtcNow
        );
    }

    public async Task<MilestonePaymentResponse> ProcessMilestonePaymentAsync(Guid customerId, ProcessMilestonePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var milestone = await _context.Milestones
            .Include(m => m.Job)
            .FirstOrDefaultAsync(m => m.Id == request.MilestoneId, cancellationToken)
            ?? throw new InvalidOperationException("Milestone not found");

        if (milestone.IsPaid)
        {
            throw new InvalidOperationException("Milestone has already been paid");
        }

        var paymentIntentRequest = new CreatePaymentIntentRequest(
            InvoiceId: null,
            QuoteId: null,
            MilestoneId: request.MilestoneId,
            Amount: milestone.Amount,
            IsDeposit: false,
            Description: $"Milestone: {milestone.Title}"
        );

        var paymentIntent = await CreatePaymentIntentAsync(customerId, paymentIntentRequest, cancellationToken);

        // Confirm with payment method
        var paymentIntentService = new PaymentIntentService();
        var confirmedIntent = await paymentIntentService.ConfirmAsync(paymentIntent.PaymentIntentId, new PaymentIntentConfirmOptions
        {
            PaymentMethod = request.PaymentMethodId
        }, cancellationToken: cancellationToken);

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.PaymentIntentId, cancellationToken);

        if (transaction != null && confirmedIntent.Status == "succeeded")
        {
            transaction.Status = "succeeded";
            transaction.PaidAt = DateTime.UtcNow;
            milestone.IsPaid = true;
            milestone.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new MilestonePaymentResponse(
            transaction?.Id ?? Guid.Empty,
            milestone.Id,
            milestone.Amount,
            confirmedIntent.Status,
            DateTime.UtcNow
        );
    }

    #endregion

    #region Payment History

    public async Task<PaymentHistoryResponse> GetPaymentHistoryAsync(Guid userId, bool isTradie, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.PaymentTransactions
            .Include(t => t.Customer)
            .Include(t => t.Tradie)
            .Include(t => t.Job)
            .Include(t => t.Invoice)
            .AsQueryable();

        if (isTradie)
        {
            query = query.Where(t => t.TradieId == userId);
        }
        else
        {
            query = query.Where(t => t.CustomerId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var allTransactions = await query.ToListAsync(cancellationToken);

        var stats = new PaymentStats(
            TotalReceived: allTransactions.Where(t => t.Status == "succeeded").Sum(t => isTradie ? t.TradiePayout : t.Amount),
            TotalPending: allTransactions.Where(t => t.Status != "succeeded" && t.Status != "canceled").Sum(t => t.Amount),
            TotalThisMonth: allTransactions.Where(t => t.Status == "succeeded" && t.PaidAt >= monthStart).Sum(t => isTradie ? t.TradiePayout : t.Amount),
            PlatformFeesThisMonth: allTransactions.Where(t => t.Status == "succeeded" && t.PaidAt >= monthStart).Sum(t => t.PlatformFee),
            TransactionCount: allTransactions.Count
        );

        return new PaymentHistoryResponse(
            Transactions: transactions.Select(t => new PaymentTransactionDto(
                Id: t.Id,
                CustomerId: t.CustomerId,
                CustomerName: $"{t.Customer.FirstName} {t.Customer.LastName}",
                TradieId: t.TradieId,
                TradieName: t.Tradie != null ? $"{t.Tradie.FirstName} {t.Tradie.LastName}" : null,
                JobId: t.JobId,
                JobTitle: t.Job?.Title,
                InvoiceId: t.InvoiceId,
                InvoiceNumber: t.Invoice?.InvoiceNumber,
                Amount: t.Amount,
                PlatformFee: t.PlatformFee,
                TradiePayout: t.TradiePayout,
                Currency: t.Currency,
                Status: t.Status,
                PaymentMethodType: t.PaymentMethodType,
                IsDeposit: t.IsDeposit,
                IsMilestonePayment: t.IsMilestonePayment,
                PaidAt: t.PaidAt,
                CreatedAt: t.CreatedAt
            )).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            Stats: stats
        );
    }

    #endregion

    #region Payouts

    public async Task<PayoutListResponse> GetPayoutsAsync(Guid tradieId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken);

        if (account == null)
        {
            return new PayoutListResponse(
                Payouts: new List<PayoutDto>(),
                TotalCount: 0,
                Page: page,
                PageSize: pageSize,
                TotalPaidOut: 0,
                PendingBalance: 0
            );
        }

        var payouts = await _context.Payouts
            .Where(p => p.TradieId == tradieId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalCount = await _context.Payouts.CountAsync(p => p.TradieId == tradieId, cancellationToken);

        var allPayouts = await _context.Payouts.Where(p => p.TradieId == tradieId).ToListAsync(cancellationToken);
        var totalPaidOut = allPayouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.Amount);

        // Get pending balance from Stripe
        decimal pendingBalance = 0;
        try
        {
            var balanceService = new BalanceService();
            var balance = await balanceService.GetAsync(new RequestOptions { StripeAccount = account.StripeAccountId }, cancellationToken);
            pendingBalance = balance.Available?.Sum(b => b.Amount / 100m) ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Stripe balance for account {AccountId}", account.StripeAccountId);
        }

        return new PayoutListResponse(
            Payouts: payouts.Select(p => new PayoutDto(
                Id: p.Id,
                StripePayoutId: p.StripePayoutId,
                Amount: p.Amount,
                Currency: p.Currency,
                Status: p.Status.ToString(),
                ArrivalDate: p.ArrivalDate,
                Description: p.Description,
                CreatedAt: p.CreatedAt
            )).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPaidOut: totalPaidOut,
            PendingBalance: pendingBalance
        );
    }

    public async Task<RequestPayoutResponse> RequestPayoutAsync(Guid tradieId, RequestPayoutRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId && s.PayoutsEnabled, cancellationToken)
            ?? throw new InvalidOperationException("Stripe account not found or payouts not enabled");

        var payoutService = new Stripe.PayoutService();
        var amountInCents = (long)(request.Amount * 100);

        var payout = await payoutService.CreateAsync(new PayoutCreateOptions
        {
            Amount = amountInCents,
            Currency = "aud",
            Description = request.Description ?? "Quote payout"
        }, new RequestOptions { StripeAccount = account.StripeAccountId }, cancellationToken);

        var payoutEntity = new Domain.Entities.Payout
        {
            Id = Guid.NewGuid(),
            StripeAccountId = account.Id,
            TradieId = tradieId,
            StripePayoutId = payout.Id,
            Amount = request.Amount,
            Currency = "aud",
            Status = MapPayoutStatus(payout.Status),
            ArrivalDate = payout.ArrivalDate,
            Description = request.Description
        };

        _context.Payouts.Add(payoutEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new RequestPayoutResponse(
            PayoutId: payoutEntity.Id,
            StripePayoutId: payout.Id,
            Amount: request.Amount,
            Status: payout.Status,
            ExpectedArrivalDate: payout.ArrivalDate
        );
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid tradieId, CancellationToken cancellationToken = default)
    {
        var account = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.TradieId == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Stripe account not found");

        var balanceService = new BalanceService();
        var balance = await balanceService.GetAsync(new RequestOptions { StripeAccount = account.StripeAccountId }, cancellationToken);

        return new BalanceResponse(
            Available: balance.Available?.Where(b => b.Currency == "aud").Sum(b => b.Amount / 100m) ?? 0,
            Pending: balance.Pending?.Where(b => b.Currency == "aud").Sum(b => b.Amount / 100m) ?? 0,
            Currency: "aud"
        );
    }

    #endregion

    #region Refunds

    public async Task<RefundResponse> RefundPaymentAsync(Guid tradieId, RefundRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.TradieId == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Transaction not found");

        if (transaction.Status != "succeeded")
        {
            throw new InvalidOperationException("Can only refund succeeded payments");
        }

        var refundAmount = request.Amount ?? transaction.Amount;
        var refundService = new RefundService();
        var refund = await refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = transaction.StripePaymentIntentId,
            Amount = (long)(refundAmount * 100),
            Reason = request.Reason
        }, cancellationToken: cancellationToken);

        transaction.RefundedAt = DateTime.UtcNow;
        transaction.RefundedAmount = (transaction.RefundedAmount ?? 0) + refundAmount;

        if (transaction.RefundedAmount >= transaction.Amount)
        {
            transaction.Status = "refunded";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RefundResponse(
            Success: refund.Status == "succeeded",
            RefundId: refund.Id,
            Amount: refundAmount,
            Status: refund.Status,
            ErrorMessage: null
        );
    }

    #endregion

    #region Webhooks

    public async Task HandleWebhookEventAsync(string json, string signature, CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to construct Stripe event");
            throw;
        }

        _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceeded(stripeEvent, cancellationToken);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailed(stripeEvent, cancellationToken);
                break;
            case "account.updated":
                await HandleAccountUpdated(stripeEvent, cancellationToken);
                break;
            case "payout.paid":
            case "payout.failed":
                await HandlePayoutUpdated(stripeEvent, cancellationToken);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id, cancellationToken);

        if (transaction != null)
        {
            transaction.Status = "succeeded";
            transaction.PaidAt = DateTime.UtcNow;
            transaction.StripeChargeId = paymentIntent.LatestChargeId;

            // Update invoice if linked
            if (transaction.InvoiceId.HasValue)
            {
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == transaction.InvoiceId.Value, cancellationToken);
                if (invoice != null)
                {
                    invoice.AmountPaid += transaction.Amount;
                    if (invoice.AmountPaid >= invoice.TotalAmount)
                    {
                        invoice.Status = InvoiceStatus.Paid;
                        invoice.PaidAt = DateTime.UtcNow;
                    }
                    else
                    {
                        invoice.Status = InvoiceStatus.PartiallyPaid;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandlePaymentIntentFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id, cancellationToken);

        if (transaction != null)
        {
            transaction.Status = "failed";
            transaction.FailureCode = paymentIntent.LastPaymentError?.Code;
            transaction.FailureMessage = paymentIntent.LastPaymentError?.Message;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleAccountUpdated(Event stripeEvent, CancellationToken cancellationToken)
    {
        var account = stripeEvent.Data.Object as Account;
        if (account == null) return;

        var stripeAccount = await _context.StripeAccounts
            .FirstOrDefaultAsync(s => s.StripeAccountId == account.Id, cancellationToken);

        if (stripeAccount != null)
        {
            stripeAccount.ChargesEnabled = account.ChargesEnabled;
            stripeAccount.PayoutsEnabled = account.PayoutsEnabled;
            stripeAccount.DetailsSubmitted = account.DetailsSubmitted;

            if (account.ChargesEnabled && account.PayoutsEnabled && account.DetailsSubmitted && !stripeAccount.IsOnboardingComplete)
            {
                stripeAccount.IsOnboardingComplete = true;
                stripeAccount.OnboardingCompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandlePayoutUpdated(Event stripeEvent, CancellationToken cancellationToken)
    {
        var payout = stripeEvent.Data.Object as Stripe.Payout;
        if (payout == null) return;

        var payoutEntity = await _context.Payouts
            .FirstOrDefaultAsync(p => p.StripePayoutId == payout.Id, cancellationToken);

        if (payoutEntity != null)
        {
            payoutEntity.Status = MapPayoutStatus(payout.Status);
            if (payout.Status == "failed")
            {
                payoutEntity.FailureCode = payout.FailureCode;
                payoutEntity.FailureMessage = payout.FailureMessage;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region Helpers

    private decimal CalculatePlatformFee(decimal amount)
    {
        return Math.Round(amount * (_platformFeePercentage / 100), 2);
    }

    private static PayoutStatus MapPayoutStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "pending" => PayoutStatus.Pending,
            "in_transit" => PayoutStatus.InTransit,
            "paid" => PayoutStatus.Paid,
            "failed" => PayoutStatus.Failed,
            "canceled" => PayoutStatus.Cancelled,
            _ => PayoutStatus.Pending
        };
    }

    #endregion
}
