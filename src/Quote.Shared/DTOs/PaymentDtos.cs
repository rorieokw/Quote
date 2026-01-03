namespace Quote.Shared.DTOs;

// Stripe Connect
public record CreateConnectedAccountRequest(
    string Email,
    string BusinessType = "individual",
    string Country = "AU"
);

public record ConnectedAccountResponse(
    Guid Id,
    string StripeAccountId,
    bool IsOnboardingComplete,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    string? OnboardingUrl
);

public record OnboardingLinkResponse(
    string Url,
    DateTime ExpiresAt
);

public record AccountStatusResponse(
    Guid Id,
    string StripeAccountId,
    bool IsOnboardingComplete,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    string? DefaultCurrency,
    string? Country,
    DateTime? OnboardingCompletedAt
);

// Payment Intent
public record CreatePaymentIntentRequest(
    Guid? InvoiceId,
    Guid? QuoteId,
    Guid? MilestoneId,
    decimal Amount,
    bool IsDeposit = false,
    string? Description = null
);

public record PaymentIntentResponse(
    string ClientSecret,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    string Status
);

public record ConfirmPaymentRequest(
    string PaymentIntentId
);

public record ConfirmPaymentResponse(
    bool Success,
    string Status,
    Guid? TransactionId,
    string? ErrorMessage
);

// Deposit
public record ProcessDepositRequest(
    Guid QuoteId,
    decimal Amount,
    string PaymentMethodId
);

public record DepositResponse(
    Guid TransactionId,
    decimal Amount,
    string Status,
    DateTime CreatedAt
);

// Milestone Payment
public record ProcessMilestonePaymentRequest(
    Guid MilestoneId,
    string PaymentMethodId
);

public record MilestonePaymentResponse(
    Guid TransactionId,
    Guid MilestoneId,
    decimal Amount,
    string Status,
    DateTime CreatedAt
);

// Payment History
public record PaymentHistoryResponse(
    List<PaymentTransactionDto> Transactions,
    int TotalCount,
    int Page,
    int PageSize,
    PaymentStats Stats
);

public record PaymentTransactionDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? TradieId,
    string? TradieName,
    Guid? JobId,
    string? JobTitle,
    Guid? InvoiceId,
    string? InvoiceNumber,
    decimal Amount,
    decimal PlatformFee,
    decimal TradiePayout,
    string Currency,
    string Status,
    string? PaymentMethodType,
    bool IsDeposit,
    bool IsMilestonePayment,
    DateTime? PaidAt,
    DateTime CreatedAt
);

public record PaymentStats(
    decimal TotalReceived,
    decimal TotalPending,
    decimal TotalThisMonth,
    decimal PlatformFeesThisMonth,
    int TransactionCount
);

// Payouts
public record PayoutListResponse(
    List<PayoutDto> Payouts,
    int TotalCount,
    int Page,
    int PageSize,
    decimal TotalPaidOut,
    decimal PendingBalance
);

public record PayoutDto(
    Guid Id,
    string StripePayoutId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime? ArrivalDate,
    string? Description,
    DateTime CreatedAt
);

public record RequestPayoutRequest(
    decimal Amount,
    string? Description = null
);

public record RequestPayoutResponse(
    Guid PayoutId,
    string StripePayoutId,
    decimal Amount,
    string Status,
    DateTime? ExpectedArrivalDate
);

// Webhook
public record StripeWebhookEvent(
    string Type,
    object Data
);

// Balance
public record BalanceResponse(
    decimal Available,
    decimal Pending,
    string Currency
);

// Refund
public record RefundRequest(
    Guid TransactionId,
    decimal? Amount = null,
    string? Reason = null
);

public record RefundResponse(
    bool Success,
    string RefundId,
    decimal Amount,
    string Status,
    string? ErrorMessage
);
