using System.Net;
using System.Net.Mail;
using GymManagement.Api.Configuration;
using Microsoft.Extensions.Options;

namespace GymManagement.Api.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPaymentInvoiceAsync(
        string toEmail,
        string memberName,
        string invoiceNumber,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping invoice email for {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Please configure Smtp settings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Payment Invoice - {invoiceNumber}",
            Body = $"""
                Hi {memberName},

                Your payment has been recorded successfully.
                Please find attached invoice {invoiceNumber}.

                Regards,
                {_settings.FromName}
                """,
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(toEmail));
        message.Attachments.Add(new Attachment(
            new MemoryStream(pdfBytes),
            $"{invoiceNumber}.pdf",
            "application/pdf"));

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message).WaitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SendPaymentReminderAsync(
        string toEmail,
        string memberName,
        string planName,
        decimal dueAmount,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping reminder email for {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Please configure Smtp settings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Payment Reminder - {planName}",
            Body = $"""
                Hi {memberName},

                This is a reminder for your pending membership payment.
                Plan: {planName}
                Due Amount: INR {dueAmount:N2}
                Notes: {notes ?? "N/A"}

                Please pay at the earliest.

                Regards,
                {_settings.FromName}
                """,
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(toEmail));

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message).WaitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SendPaymentReceiptAsync(
        string toEmail,
        string memberName,
        string receiptNumber,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping receipt email for {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Please configure Smtp settings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Payment Receipt - {receiptNumber}",
            Body = $"""
                Hi {memberName},

                Your payment has been received successfully.
                Please find attached receipt {receiptNumber}.

                Regards,
                {_settings.FromName}
                """,
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(toEmail));
        message.Attachments.Add(new Attachment(
            new MemoryStream(pdfBytes),
            $"{receiptNumber}.pdf",
            "application/pdf"));

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message).WaitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SendGymOnboardingAsync(
        string toEmail,
        string adminName,
        string gymName,
        string loginEmail,
        string temporaryPassword,
        string portalUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping onboarding email for {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Please configure Smtp settings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Welcome to ManageMyGym - {gymName}",
            IsBodyHtml = true,
            Body = $"""
                <p>Hi {WebUtility.HtmlEncode(adminName)},</p>
                <p>Welcome to <strong>ManageMyGym</strong>. Your gym account has been successfully created.</p>
                <p><strong>Gym:</strong> {WebUtility.HtmlEncode(gymName)}</p>
                <p><strong>Login details:</strong></p>
                <ul>
                    <li>Email: <strong>{WebUtility.HtmlEncode(loginEmail)}</strong></li>
                    <li>Temporary Password: <strong>{WebUtility.HtmlEncode(temporaryPassword)}</strong></li>
                </ul>
                <p>Portal: <a href="{WebUtility.HtmlEncode(portalUrl)}">{WebUtility.HtmlEncode(portalUrl)}</a></p>
                <p>Please sign in and change your password immediately for security.</p>
                <p>We are excited to have you onboard and wish your gym continued growth and success.</p>
                <p>Regards,<br/>{WebUtility.HtmlEncode(_settings.FromName)}</p>
                """
        };

        message.To.Add(new MailAddress(toEmail));

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message).WaitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SendGymSubscriptionInvoiceAsync(
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
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP email is disabled. Skipping subscription invoice email for {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Please configure Smtp settings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Gym Subscription Invoice - {invoiceNumber}",
            IsBodyHtml = true,
            Body = $"""
                <p>Hi {WebUtility.HtmlEncode(adminName)},</p>
                <p>Your gym subscription payment has been recorded successfully.</p>
                <p><strong>Gym:</strong> {WebUtility.HtmlEncode(gymName)}</p>
                <p><strong>Invoice No:</strong> {WebUtility.HtmlEncode(invoiceNumber)}</p>
                <p><strong>Plan:</strong> {WebUtility.HtmlEncode(planCode)}</p>
                <p><strong>Amount:</strong> INR {amount:N2}</p>
                <p><strong>Paid On:</strong> {paidOn:dd MMM yyyy}</p>
                <p><strong>Payment Mode:</strong> {WebUtility.HtmlEncode(paymentMode)}</p>
                <p><strong>Transaction Ref:</strong> {WebUtility.HtmlEncode(transactionReference ?? "N/A")}</p>
                <p><strong>Valid Till:</strong> {(validTill.HasValue ? validTill.Value.ToString("dd MMM yyyy") : "N/A")}</p>
                <p>Regards,<br/>{WebUtility.HtmlEncode(_settings.FromName)}</p>
                """
        };

        message.To.Add(new MailAddress(toEmail));
        message.Attachments.Add(new Attachment(
            new MemoryStream(pdfBytes),
            $"{invoiceNumber}.pdf",
            "application/pdf"));

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message).WaitAsync(cancellationToken);
        return true;
    }
}
