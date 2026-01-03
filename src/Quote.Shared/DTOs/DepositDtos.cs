namespace Quote.Shared.DTOs;

public record DepositDto(
    Guid Id,
    Guid QuoteId,
    decimal Amount,
    decimal PercentageOfTotal,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt
);

public record RequestDepositRequest(
    Guid QuoteId,
    decimal? FixedAmount,
    decimal? Percentage  // Either fixed amount or percentage, not both
);

public record DepositPaymentRequest(
    Guid DepositId,
    string PaymentMethodId  // Stripe payment method ID
);

public record DepositPaymentResponse(
    Guid DepositId,
    string Status,
    string? ClientSecret,  // For Stripe payment confirmation if needed
    string? ErrorMessage
);

public record RefundDepositRequest(
    Guid DepositId,
    string Reason
);

public record DepositStatusDto(
    Guid QuoteId,
    bool DepositRequired,
    decimal? RequiredAmount,
    decimal? RequiredPercentage,
    DepositDto? Deposit
);
