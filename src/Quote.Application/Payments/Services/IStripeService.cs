using Quote.Domain.Entities;
using Quote.Shared.DTOs;

namespace Quote.Application.Payments.Services;

public interface IStripeService
{
    // Connected Accounts (Stripe Connect)
    Task<ConnectedAccountResponse> CreateConnectedAccountAsync(Guid tradieId, CreateConnectedAccountRequest request, CancellationToken cancellationToken = default);
    Task<OnboardingLinkResponse> GetOnboardingLinkAsync(Guid tradieId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default);
    Task<AccountStatusResponse?> GetAccountStatusAsync(Guid tradieId, CancellationToken cancellationToken = default);
    Task RefreshAccountStatusAsync(Guid tradieId, CancellationToken cancellationToken = default);

    // Payment Intents
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid customerId, CreatePaymentIntentRequest request, CancellationToken cancellationToken = default);
    Task<ConfirmPaymentResponse> ConfirmPaymentAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetTransactionByPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    // Deposits and Milestone Payments
    Task<DepositResponse> ProcessDepositAsync(Guid customerId, ProcessDepositRequest request, CancellationToken cancellationToken = default);
    Task<MilestonePaymentResponse> ProcessMilestonePaymentAsync(Guid customerId, ProcessMilestonePaymentRequest request, CancellationToken cancellationToken = default);

    // Payment History
    Task<PaymentHistoryResponse> GetPaymentHistoryAsync(Guid userId, bool isTradie, int page, int pageSize, CancellationToken cancellationToken = default);

    // Payouts
    Task<PayoutListResponse> GetPayoutsAsync(Guid tradieId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RequestPayoutResponse> RequestPayoutAsync(Guid tradieId, RequestPayoutRequest request, CancellationToken cancellationToken = default);
    Task<BalanceResponse> GetBalanceAsync(Guid tradieId, CancellationToken cancellationToken = default);

    // Refunds
    Task<RefundResponse> RefundPaymentAsync(Guid tradieId, RefundRequest request, CancellationToken cancellationToken = default);

    // Webhooks
    Task HandleWebhookEventAsync(string json, string signature, CancellationToken cancellationToken = default);
}
