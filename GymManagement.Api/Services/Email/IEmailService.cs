namespace GymManagement.Api.Services.Email;

public interface IEmailService
{
    Task<bool> SendPaymentInvoiceAsync(
        string toEmail,
        string memberName,
        string invoiceNumber,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);

    Task<bool> SendPaymentReminderAsync(
        string toEmail,
        string memberName,
        string planName,
        decimal dueAmount,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<bool> SendPaymentReceiptAsync(
        string toEmail,
        string memberName,
        string receiptNumber,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);

    Task<bool> SendGymOnboardingAsync(
        string toEmail,
        string adminName,
        string gymName,
        string loginEmail,
        string temporaryPassword,
        string portalUrl,
        CancellationToken cancellationToken = default);

    Task<bool> SendGymSubscriptionInvoiceAsync(
        string toEmail,
        string adminName,
        string gymName,
        string invoiceNumber,
        string planCode,
        decimal amount,
        DateTime paidOn,
        string paymentMode,
        string? transactionReference,
        DateTime? validTill,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);
}
