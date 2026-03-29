using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManagement.Api.Services.Invoice;

public class GymSubscriptionInvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string GymEmail { get; set; } = string.Empty;
    public string GymPhone { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime? ValidTill { get; set; }

    public string IssuerBusinessName { get; set; } = "ManageMyGym";
    public string? IssuerEmail { get; set; }
    public string? IssuerPhone { get; set; }
    public string? IssuerAddress { get; set; }
    public string? IssuerGstNumber { get; set; }
    public string? IssuerAuthorizedSignatory { get; set; }
    public string? TermsAndConditions { get; set; }
}

public static class GymSubscriptionInvoicePdfBuilder
{
    public static byte[] Build(GymSubscriptionInvoiceData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(data.IssuerBusinessName).FontSize(16).Bold();
                        if (!string.IsNullOrWhiteSpace(data.IssuerAddress))
                        {
                            col.Item().Text(data.IssuerAddress);
                        }
                        if (!string.IsNullOrWhiteSpace(data.IssuerEmail) || !string.IsNullOrWhiteSpace(data.IssuerPhone))
                        {
                            col.Item().Text($"Email: {data.IssuerEmail ?? "-"} | Phone: {data.IssuerPhone ?? "-"}");
                        }
                        if (!string.IsNullOrWhiteSpace(data.IssuerGstNumber))
                        {
                            col.Item().Text($"GSTIN: {data.IssuerGstNumber}");
                        }
                    });

                    row.ConstantItem(220).AlignRight().Column(col =>
                    {
                        col.Item().Text("SUBSCRIPTION INVOICE").SemiBold();
                        col.Item().Text($"Invoice No: {data.InvoiceNumber}");
                        col.Item().Text($"Invoice Date: {data.InvoiceDate:dd-MMM-yyyy}");
                    });
                });

                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(billTo =>
                    {
                        billTo.Item().Text("Bill To").Bold();
                        billTo.Item().Text(data.GymName);
                        billTo.Item().Text($"Email: {data.GymEmail}");
                        billTo.Item().Text($"Phone: {data.GymPhone}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        static IContainer HeaderCell(IContainer container) =>
                            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4);

                        static IContainer BodyCell(IContainer container) =>
                            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(4);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Plan").Bold();
                            header.Cell().Element(HeaderCell).Text("Code").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount").Bold();
                        });

                        table.Cell().Element(BodyCell).Text(data.PlanName);
                        table.Cell().Element(BodyCell).Text(data.PlanCode);
                        table.Cell().Element(BodyCell).AlignRight().Text($"INR {data.Amount:N2}");
                    });

                    col.Item().AlignRight().Width(220).Column(totals =>
                    {
                        totals.Item().Row(r => { r.RelativeItem().Text("Payment Mode"); r.RelativeItem().AlignRight().Text(data.PaymentMode); });
                        totals.Item().Row(r => { r.RelativeItem().Text("Transaction Ref"); r.RelativeItem().AlignRight().Text(data.TransactionReference ?? "N/A"); });
                        totals.Item().Row(r => { r.RelativeItem().Text("Valid Till"); r.RelativeItem().AlignRight().Text(data.ValidTill?.ToString("dd-MMM-yyyy") ?? "N/A"); });
                        totals.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total").Bold();
                            r.RelativeItem().AlignRight().Text($"INR {data.Amount:N2}").Bold();
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(data.TermsAndConditions))
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(tc =>
                        {
                            tc.Item().Text("Terms & Conditions").Bold();
                            foreach (var line in data.TermsAndConditions.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            {
                                tc.Item().Text(line.Trim());
                            }
                        });
                    }
                });

                page.Footer().AlignRight().Text($"Authorized Signatory: {data.IssuerAuthorizedSignatory ?? "N/A"}");
            });
        }).GeneratePdf();
    }
}
