using ClosedXML.Excel;
using GymManagement.Api.Data;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("financial")]
    public IActionResult GetFinancialReport(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? membershipType = null,
        [FromQuery] string? status = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildFinancialReport(
            period.Value.FromDate,
            period.Value.ToDate,
            page,
            pageSize,
            search,
            membershipType,
            status);
        return Ok(data);
    }

    [HttpGet("financial/export/excel")]
    public IActionResult ExportFinancialExcel(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search = null,
        [FromQuery] string? membershipType = null,
        [FromQuery] string? status = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildFinancialReport(
            period.Value.FromDate,
            period.Value.ToDate,
            1,
            100000,
            search,
            membershipType,
            status);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Financial Report");

        sheet.Cell(1, 1).Value = "From Date";
        sheet.Cell(1, 2).Value = data.FromDate.ToString("yyyy-MM-dd");
        sheet.Cell(2, 1).Value = "To Date";
        sheet.Cell(2, 2).Value = data.ToDate.ToString("yyyy-MM-dd");
        sheet.Cell(3, 1).Value = "Total Records";
        sheet.Cell(3, 2).Value = data.TotalMemberships;
        sheet.Cell(4, 1).Value = "Total Billing";
        sheet.Cell(4, 2).Value = data.TotalBilling;
        sheet.Cell(5, 1).Value = "Total Collected";
        sheet.Cell(5, 2).Value = data.TotalCollected;
        sheet.Cell(6, 1).Value = "Total Due";
        sheet.Cell(6, 2).Value = data.TotalDue;

        var startRow = 8;
        var headers = new[]
        {
            "Member Name", "Email", "Plan", "Membership Type", "Created On", "Start Date", "End Date",
            "Plan Amount", "Discount", "Net Amount", "Collected", "Due", "Status"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(startRow, i + 1).Value = headers[i];
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            var row = startRow + i + 1;
            var item = data.Items[i];
            sheet.Cell(row, 1).Value = item.MemberName;
            sheet.Cell(row, 2).Value = item.MemberEmail;
            sheet.Cell(row, 3).Value = item.PlanName;
            sheet.Cell(row, 4).Value = item.MembershipType;
            sheet.Cell(row, 5).Value = item.CreatedOn.ToString("yyyy-MM-dd");
            sheet.Cell(row, 6).Value = item.StartDate.ToString("yyyy-MM-dd");
            sheet.Cell(row, 7).Value = item.EndDate.ToString("yyyy-MM-dd");
            sheet.Cell(row, 8).Value = item.PlanAmount;
            sheet.Cell(row, 9).Value = item.Discount;
            sheet.Cell(row, 10).Value = item.NetAmount;
            sheet.Cell(row, 11).Value = item.CollectedAmount;
            sheet.Cell(row, 12).Value = item.DueAmount;
            sheet.Cell(row, 13).Value = item.Status;
        }

        sheet.Range(startRow, 1, startRow, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"financial-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("financial/export/pdf")]
    public IActionResult ExportFinancialPdf(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search = null,
        [FromQuery] string? membershipType = null,
        [FromQuery] string? status = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildFinancialReport(
            period.Value.FromDate,
            period.Value.ToDate,
            1,
            100000,
            search,
            membershipType,
            status);
        var pdf = BuildFinancialPdf(data);
        var fileName = $"financial-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet("attendance")]
    public IActionResult GetAttendanceReport([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildAttendanceReport(period.Value.FromDate, period.Value.ToDate);
        return Ok(data);
    }

    [HttpGet("attendance/export/excel")]
    public IActionResult ExportAttendanceExcel([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildAttendanceReport(period.Value.FromDate, period.Value.ToDate);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Report");

        sheet.Cell(1, 1).Value = "From Date";
        sheet.Cell(1, 2).Value = data.FromDate.ToString("yyyy-MM-dd");
        sheet.Cell(2, 1).Value = "To Date";
        sheet.Cell(2, 2).Value = data.ToDate.ToString("yyyy-MM-dd");
        sheet.Cell(3, 1).Value = "Total Members";
        sheet.Cell(3, 2).Value = data.TotalMembers;
        sheet.Cell(4, 1).Value = "Total Marked Days";
        sheet.Cell(4, 2).Value = data.TotalMarkedDays;
        sheet.Cell(5, 1).Value = "Total Present";
        sheet.Cell(5, 2).Value = data.TotalPresentDays;
        sheet.Cell(6, 1).Value = "Total Absent";
        sheet.Cell(6, 2).Value = data.TotalAbsentDays;

        var startRow = 8;
        var headers = new[] { "Member Name", "Email", "Marked Days", "Present Days", "Absent Days", "Attendance %" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(startRow, i + 1).Value = headers[i];
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            var row = startRow + i + 1;
            var item = data.Items[i];
            sheet.Cell(row, 1).Value = item.MemberName;
            sheet.Cell(row, 2).Value = item.MemberEmail;
            sheet.Cell(row, 3).Value = item.TotalMarkedDays;
            sheet.Cell(row, 4).Value = item.PresentDays;
            sheet.Cell(row, 5).Value = item.AbsentDays;
            sheet.Cell(row, 6).Value = item.AttendancePercentage;
        }

        sheet.Range(startRow, 1, startRow, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"attendance-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("attendance/export/pdf")]
    public IActionResult ExportAttendancePdf([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildAttendanceReport(period.Value.FromDate, period.Value.ToDate);
        var pdf = BuildAttendancePdf(data);
        var fileName = $"attendance-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet("payment-dues")]
    public IActionResult GetPaymentDuesReport([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentDuesReport(fromDate, toDate);
        return Ok(data);
    }

    [HttpGet("payment-dues/export/excel")]
    public IActionResult ExportPaymentDuesExcel([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentDuesReport(fromDate, toDate);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Payment Dues");

        sheet.Cell(1, 1).Value = "From Date";
        sheet.Cell(1, 2).Value = data.FromDate?.ToString("yyyy-MM-dd") ?? "All";
        sheet.Cell(2, 1).Value = "To Date";
        sheet.Cell(2, 2).Value = data.ToDate?.ToString("yyyy-MM-dd") ?? "All";
        sheet.Cell(3, 1).Value = "Total Members with Due";
        sheet.Cell(3, 2).Value = data.TotalMembersWithDue;
        sheet.Cell(4, 1).Value = "Total Charges";
        sheet.Cell(4, 2).Value = data.TotalCharges;
        sheet.Cell(5, 1).Value = "Total Collected";
        sheet.Cell(5, 2).Value = data.TotalCollected;
        sheet.Cell(6, 1).Value = "Total Due";
        sheet.Cell(6, 2).Value = data.TotalDue;

        var startRow = 8;
        var headers = new[] { "Member Name", "Email", "Total Charges", "Total Collected", "Due Amount" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(startRow, i + 1).Value = headers[i];
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            var row = startRow + i + 1;
            var item = data.Items[i];
            sheet.Cell(row, 1).Value = item.MemberName;
            sheet.Cell(row, 2).Value = item.MemberEmail;
            sheet.Cell(row, 3).Value = item.TotalCharges;
            sheet.Cell(row, 4).Value = item.TotalCollected;
            sheet.Cell(row, 5).Value = item.DueAmount;
        }

        sheet.Range(startRow, 1, startRow, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payment-dues-report-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
    }

    [HttpGet("payment-dues/export/pdf")]
    public IActionResult ExportPaymentDuesPdf([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentDuesReport(fromDate, toDate);
        var pdf = BuildPaymentDuesPdf(data);
        return File(pdf, "application/pdf", $"payment-dues-report-{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
    }

    [HttpGet("payment-collections")]
    public IActionResult GetPaymentCollectionsReport(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? paymentMode = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentCollectionsReport(period.Value.FromDate, period.Value.ToDate, page, pageSize, search, paymentMode);
        return Ok(data);
    }

    [HttpGet("payment-collections/export/excel")]
    public IActionResult ExportPaymentCollectionsExcel(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search = null,
        [FromQuery] string? paymentMode = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentCollectionsReport(period.Value.FromDate, period.Value.ToDate, 1, 100000, search, paymentMode);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Payment Collections");

        sheet.Cell(1, 1).Value = "From Date";
        sheet.Cell(1, 2).Value = data.FromDate.ToString("yyyy-MM-dd");
        sheet.Cell(2, 1).Value = "To Date";
        sheet.Cell(2, 2).Value = data.ToDate.ToString("yyyy-MM-dd");
        sheet.Cell(3, 1).Value = "Total Receipts";
        sheet.Cell(3, 2).Value = data.TotalReceipts;
        sheet.Cell(4, 1).Value = "Total Collection";
        sheet.Cell(4, 2).Value = data.TotalCollectionAmount;

        var startRow = 6;
        var headers = new[] { "Paid On", "Receipt No", "Invoice No", "Member Name", "Member Email", "Payment Mode", "Amount" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(startRow, i + 1).Value = headers[i];
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            var row = startRow + i + 1;
            var item = data.Items[i];
            sheet.Cell(row, 1).Value = item.PaidOn.ToString("yyyy-MM-dd");
            sheet.Cell(row, 2).Value = item.ReceiptNumber;
            sheet.Cell(row, 3).Value = item.InvoiceNumber;
            sheet.Cell(row, 4).Value = item.MemberName;
            sheet.Cell(row, 5).Value = item.MemberEmail;
            sheet.Cell(row, 6).Value = item.PaymentMode;
            sheet.Cell(row, 7).Value = item.Amount;
        }

        sheet.Range(startRow, 1, startRow, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payment-collections-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.xlsx");
    }

    [HttpGet("payment-collections/export/pdf")]
    public IActionResult ExportPaymentCollectionsPdf(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search = null,
        [FromQuery] string? paymentMode = null)
    {
        var period = ResolvePeriod(fromDate, toDate);
        if (period is null)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var data = BuildPaymentCollectionsReport(period.Value.FromDate, period.Value.ToDate, 1, 100000, search, paymentMode);
        var pdf = BuildPaymentCollectionsPdf(data);
        return File(pdf, "application/pdf", $"payment-collections-report-{data.FromDate:yyyyMMdd}-{data.ToDate:yyyyMMdd}.pdf");
    }

    private (DateOnly FromDate, DateOnly ToDate)? ResolvePeriod(DateOnly? fromDate, DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var resolvedTo = toDate ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (resolvedFrom > resolvedTo)
        {
            return null;
        }

        return (resolvedFrom, resolvedTo);
    }

    private FinancialReportResponse BuildFinancialReport(
        DateOnly fromDate,
        DateOnly toDate,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? membershipType = null,
        string? status = null)
    {
        var createdFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var createdToExclusive = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedPage = page < 1 ? 1 : page;
        var resolvedPageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var records = _context.MemberMemberships
            .AsNoTracking()
            .Select(mm => new
            {
                mm.Id,
                mm.CreatedOn,
                mm.StartDate,
                mm.EndDate,
                mm.PlanPriceAtEnrollment,
                mm.Discount,
                CreatedInRange = mm.CreatedOn >= createdFrom && mm.CreatedOn < createdToExclusive,
                MemberName = mm.Member != null ? mm.Member.Name : "N/A",
                MemberEmail = mm.Member != null ? mm.Member.Email : "N/A",
                PlanName = mm.MembershipPlan != null ? mm.MembershipPlan.PlanName : "N/A",
                MembershipType = mm.MembershipPlan != null ? mm.MembershipPlan.MembershipType.ToString() : "N/A",
                CollectedAmountInPeriod = _context.Payments
                    .Where(p =>
                        p.MemberMembershipId == mm.Id &&
                        p.PaidOn >= createdFrom &&
                        p.PaidOn < createdToExclusive)
                    .Sum(p => (decimal?)p.Amount) ?? 0m,
                CollectedAmountLifetime = _context.Payments
                    .Where(p => p.MemberMembershipId == mm.Id)
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToList()
            .Where(x => x.CreatedInRange || x.CollectedAmountInPeriod > 0m)
            .Select(x =>
            {
                var netAmount = Math.Max(x.PlanPriceAtEnrollment - x.Discount, 0m);
                var due = Math.Max(netAmount - x.CollectedAmountLifetime, 0m);
                var computedStatus = x.EndDate < today ? "Completed" : "Active";
                return new FinancialReportItem
                {
                    MembershipId = x.Id,
                    MemberName = x.MemberName,
                    MemberEmail = x.MemberEmail,
                    PlanName = x.PlanName,
                    MembershipType = x.MembershipType,
                    CreatedOn = x.CreatedOn,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    PlanAmount = x.PlanPriceAtEnrollment,
                    Discount = x.Discount,
                    NetAmount = netAmount,
                    CollectedAmount = x.CollectedAmountInPeriod,
                    DueAmount = due,
                    Status = computedStatus
                };
            })
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                x.MemberName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                x.MemberEmail.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                x.PlanName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x =>
                string.IsNullOrWhiteSpace(membershipType) ||
                x.MembershipType.Equals(membershipType.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x =>
                string.IsNullOrWhiteSpace(status) ||
                x.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.DueAmount)
            .ThenByDescending(x => x.CreatedOn)
            .ToList();

        var totalCount = records.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);
        if (totalPages > 0 && resolvedPage > totalPages)
        {
            resolvedPage = totalPages;
        }

        var paged = records
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .ToList();

        return new FinancialReportResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Search = search?.Trim(),
            MembershipType = membershipType?.Trim(),
            Status = status?.Trim(),
            TotalMemberships = totalCount,
            TotalBilling = records.Sum(x => x.NetAmount),
            TotalCollected = records.Sum(x => x.CollectedAmount),
            TotalDue = records.Sum(x => x.DueAmount),
            Items = paged
        };
    }

    private AttendanceReportResponse BuildAttendanceReport(DateOnly fromDate, DateOnly toDate)
    {
        var grouped = _context.MemberAttendances
            .AsNoTracking()
            .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate)
            .GroupBy(a => a.MemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                TotalMarkedDays = g.Count(),
                PresentDays = g.Count(x => x.Status == AttendanceStatus.Present),
                AbsentDays = g.Count(x => x.Status == AttendanceStatus.Absent)
            })
            .ToList();

        var memberIds = grouped.Select(x => x.MemberId).ToList();
        var members = _context.Members
            .AsNoTracking()
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.Email })
            .ToDictionary(m => m.Id, m => m);

        var items = grouped
            .Select(x =>
            {
                var member = members.TryGetValue(x.MemberId, out var value) ? value : null;
                var percentage = x.TotalMarkedDays == 0 ? 0m : (decimal)x.PresentDays * 100 / x.TotalMarkedDays;
                return new AttendanceReportItem
                {
                    MemberId = x.MemberId,
                    MemberName = member?.Name ?? "N/A",
                    MemberEmail = member?.Email ?? "N/A",
                    TotalMarkedDays = x.TotalMarkedDays,
                    PresentDays = x.PresentDays,
                    AbsentDays = x.AbsentDays,
                    AttendancePercentage = Math.Round(percentage, 2)
                };
            })
            .OrderByDescending(x => x.AttendancePercentage)
            .ThenBy(x => x.MemberName)
            .ToList();

        return new AttendanceReportResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalMembers = items.Count,
            TotalMarkedDays = items.Sum(x => x.TotalMarkedDays),
            TotalPresentDays = items.Sum(x => x.PresentDays),
            TotalAbsentDays = items.Sum(x => x.AbsentDays),
            Items = items
        };
    }

    private PaymentDuesReportResponse BuildPaymentDuesReport(DateOnly? fromDate, DateOnly? toDate)
    {
        var chargesFrom = fromDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var chargesToExclusive = toDate?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var records = _context.Members
            .AsNoTracking()
            .Select(member => new
            {
                MemberId = member.Id,
                MemberName = member.Name,
                MemberEmail = member.Email,
                TotalCharges = _context.MemberMemberships
                    .Where(mm =>
                        mm.MemberId == member.Id &&
                        (!chargesFrom.HasValue || mm.CreatedOn >= chargesFrom.Value) &&
                        (!chargesToExclusive.HasValue || mm.CreatedOn < chargesToExclusive.Value))
                    .Select(mm => (decimal?)(mm.PlanPriceAtEnrollment - mm.Discount))
                    .Sum() ?? 0m,
                TotalCollected = _context.Payments
                    .Where(p =>
                        p.MemberId == member.Id &&
                        (!chargesFrom.HasValue || p.PaidOn >= chargesFrom.Value) &&
                        (!chargesToExclusive.HasValue || p.PaidOn < chargesToExclusive.Value))
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToList()
            .Select(x => new PaymentDueItem
            {
                MemberId = x.MemberId,
                MemberName = x.MemberName,
                MemberEmail = x.MemberEmail,
                TotalCharges = x.TotalCharges,
                TotalCollected = x.TotalCollected,
                DueAmount = x.TotalCharges - x.TotalCollected
            })
            .Where(x => x.DueAmount > 0m)
            .OrderByDescending(x => x.DueAmount)
            .ThenBy(x => x.MemberName)
            .ToList();

        return new PaymentDuesReportResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalMembersWithDue = records.Count,
            TotalCharges = records.Sum(x => x.TotalCharges),
            TotalCollected = records.Sum(x => x.TotalCollected),
            TotalDue = records.Sum(x => x.DueAmount),
            Items = records
        };
    }

    private PaymentCollectionsReportResponse BuildPaymentCollectionsReport(
        DateOnly fromDate,
        DateOnly toDate,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? paymentMode = null)
    {
        var paidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var paidToExclusive = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var resolvedPage = page < 1 ? 1 : page;
        var resolvedPageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var records = _context.Payments
            .AsNoTracking()
            .Where(p => p.PaidOn >= paidFrom && p.PaidOn < paidToExclusive)
            .Select(p => new PaymentCollectionItem
            {
                PaymentId = p.Id,
                PaidOn = p.PaidOn,
                ReceiptNumber = p.ReceiptNumber,
                InvoiceNumber = p.InvoiceNumber,
                MemberId = p.MemberId,
                MemberName = _context.Members.Where(m => m.Id == p.MemberId).Select(m => m.Name).FirstOrDefault() ?? "N/A",
                MemberEmail = _context.Members.Where(m => m.Id == p.MemberId).Select(m => m.Email).FirstOrDefault() ?? "N/A",
                PaymentMode = p.PaymentMode.ToString(),
                Amount = p.Amount
            })
            .ToList()
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                x.MemberName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                x.MemberEmail.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                x.ReceiptNumber.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                x.InvoiceNumber.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x =>
                string.IsNullOrWhiteSpace(paymentMode) ||
                x.PaymentMode.Equals(paymentMode.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PaidOn)
            .ToList();

        var totalCount = records.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);
        if (totalPages > 0 && resolvedPage > totalPages)
        {
            resolvedPage = totalPages;
        }

        var paged = records
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .ToList();

        return new PaymentCollectionsReportResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Search = search?.Trim(),
            PaymentMode = paymentMode?.Trim(),
            TotalReceipts = totalCount,
            TotalCollectionAmount = records.Sum(x => x.Amount),
            Items = paged
        };
    }

    private static byte[] BuildFinancialPdf(FinancialReportResponse data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text("Financial Report").SemiBold().FontSize(16);
                    header.Item().Text($"Period: {data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}");
                    header.Item().Text($"Memberships: {data.TotalMemberships}  Billing: INR {data.TotalBilling:N2}  Collected: INR {data.TotalCollected:N2}  Due: INR {data.TotalDue:N2}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                    });

                    static IContainer Head(IContainer c) => c.Background("#E2E8F0").Padding(4).BorderBottom(1).BorderColor("#CBD5E1");
                    static IContainer Body(IContainer c) => c.Padding(4).BorderBottom(1).BorderColor("#E2E8F0");

                    var headers = new[] { "Member", "Plan", "Type", "Created", "Start", "End", "Net", "Collected", "Due", "Status", "Email" };
                    foreach (var header in headers)
                    {
                        table.Cell().Element(Head).Text(header).SemiBold();
                    }

                    foreach (var row in data.Items)
                    {
                        table.Cell().Element(Body).Text(row.MemberName);
                        table.Cell().Element(Body).Text(row.PlanName);
                        table.Cell().Element(Body).Text(row.MembershipType);
                        table.Cell().Element(Body).Text(row.CreatedOn.ToString("yyyy-MM-dd"));
                        table.Cell().Element(Body).Text(row.StartDate.ToString("yyyy-MM-dd"));
                        table.Cell().Element(Body).Text(row.EndDate.ToString("yyyy-MM-dd"));
                        table.Cell().Element(Body).Text($"INR {row.NetAmount:N2}");
                        table.Cell().Element(Body).Text($"INR {row.CollectedAmount:N2}");
                        table.Cell().Element(Body).Text($"INR {row.DueAmount:N2}");
                        table.Cell().Element(Body).Text(row.Status);
                        table.Cell().Element(Body).Text(row.MemberEmail);
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    private static byte[] BuildAttendancePdf(AttendanceReportResponse data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Text("Attendance Report").SemiBold().FontSize(16);
                    header.Item().Text($"Period: {data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}");
                    header.Item().Text($"Members: {data.TotalMembers}  Marked: {data.TotalMarkedDays}  Present: {data.TotalPresentDays}  Absent: {data.TotalAbsentDays}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.4f);
                        columns.RelativeColumn(2.6f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    static IContainer Head(IContainer c) => c.Background("#E2E8F0").Padding(6).BorderBottom(1).BorderColor("#CBD5E1");
                    static IContainer Body(IContainer c) => c.Padding(6).BorderBottom(1).BorderColor("#E2E8F0");

                    var headers = new[] { "Member", "Email", "Marked", "Present", "Absent", "Attendance %" };
                    foreach (var header in headers)
                    {
                        table.Cell().Element(Head).Text(header).SemiBold();
                    }

                    foreach (var row in data.Items)
                    {
                        table.Cell().Element(Body).Text(row.MemberName);
                        table.Cell().Element(Body).Text(row.MemberEmail);
                        table.Cell().Element(Body).Text(row.TotalMarkedDays.ToString());
                        table.Cell().Element(Body).Text(row.PresentDays.ToString());
                        table.Cell().Element(Body).Text(row.AbsentDays.ToString());
                        table.Cell().Element(Body).Text($"{row.AttendancePercentage:N2}%");
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    private static byte[] BuildPaymentDuesPdf(PaymentDuesReportResponse data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Column(header =>
                {
                    header.Item().Text("Payment Dues Report").SemiBold().FontSize(16);
                    if (data.FromDate.HasValue || data.ToDate.HasValue)
                    {
                        header.Item().Text($"Period: {data.FromDate?.ToString("yyyy-MM-dd") ?? "All"} to {data.ToDate?.ToString("yyyy-MM-dd") ?? "All"}");
                    }
                    header.Item().Text($"Members: {data.TotalMembersWithDue}  Charges: INR {data.TotalCharges:N2}  Collected: INR {data.TotalCollected:N2}  Due: INR {data.TotalDue:N2}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    static IContainer Head(IContainer c) => c.Background("#E2E8F0").Padding(6).BorderBottom(1).BorderColor("#CBD5E1");
                    static IContainer Body(IContainer c) => c.Padding(6).BorderBottom(1).BorderColor("#E2E8F0");

                    foreach (var header in new[] { "Member", "Email", "Charges", "Collected", "Due" })
                    {
                        table.Cell().Element(Head).Text(header).SemiBold();
                    }

                    foreach (var row in data.Items)
                    {
                        table.Cell().Element(Body).Text(row.MemberName);
                        table.Cell().Element(Body).Text(row.MemberEmail);
                        table.Cell().Element(Body).Text($"INR {row.TotalCharges:N2}");
                        table.Cell().Element(Body).Text($"INR {row.TotalCollected:N2}");
                        table.Cell().Element(Body).Text($"INR {row.DueAmount:N2}");
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    private static byte[] BuildPaymentCollectionsPdf(PaymentCollectionsReportResponse data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Column(header =>
                {
                    header.Item().Text("Payment Collections Report").SemiBold().FontSize(16);
                    header.Item().Text($"Period: {data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd}");
                    header.Item().Text($"Receipts: {data.TotalReceipts}  Total Collection: INR {data.TotalCollectionAmount:N2}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    static IContainer Head(IContainer c) => c.Background("#E2E8F0").Padding(5).BorderBottom(1).BorderColor("#CBD5E1");
                    static IContainer Body(IContainer c) => c.Padding(5).BorderBottom(1).BorderColor("#E2E8F0");

                    foreach (var header in new[] { "Paid On", "Receipt No", "Invoice No", "Member", "Email", "Mode", "Amount" })
                    {
                        table.Cell().Element(Head).Text(header).SemiBold();
                    }

                    foreach (var row in data.Items)
                    {
                        table.Cell().Element(Body).Text(row.PaidOn.ToString("yyyy-MM-dd"));
                        table.Cell().Element(Body).Text(row.ReceiptNumber);
                        table.Cell().Element(Body).Text(row.InvoiceNumber);
                        table.Cell().Element(Body).Text(row.MemberName);
                        table.Cell().Element(Body).Text(row.MemberEmail);
                        table.Cell().Element(Body).Text(row.PaymentMode);
                        table.Cell().Element(Body).Text($"INR {row.Amount:N2}");
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    public class FinancialReportResponse
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public string? MembershipType { get; set; }
        public string? Status { get; set; }
        public int TotalMemberships { get; set; }
        public decimal TotalBilling { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalDue { get; set; }
        public List<FinancialReportItem> Items { get; set; } = new();
    }

    public class FinancialReportItem
    {
        public Guid MembershipId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string MembershipType { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal PlanAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AttendanceReportResponse
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int TotalMembers { get; set; }
        public int TotalMarkedDays { get; set; }
        public int TotalPresentDays { get; set; }
        public int TotalAbsentDays { get; set; }
        public List<AttendanceReportItem> Items { get; set; } = new();
    }

    public class AttendanceReportItem
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public int TotalMarkedDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    public class PaymentDuesReportResponse
    {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int TotalMembersWithDue { get; set; }
        public decimal TotalCharges { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalDue { get; set; }
        public List<PaymentDueItem> Items { get; set; } = new();
    }

    public class PaymentDueItem
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public decimal TotalCharges { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal DueAmount { get; set; }
    }

    public class PaymentCollectionsReportResponse
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public string? PaymentMode { get; set; }
        public int TotalReceipts { get; set; }
        public decimal TotalCollectionAmount { get; set; }
        public List<PaymentCollectionItem> Items { get; set; } = new();
    }

    public class PaymentCollectionItem
    {
        public Guid PaymentId { get; set; }
        public DateTime PaidOn { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
