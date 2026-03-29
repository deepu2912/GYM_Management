namespace GymManagement.Api.Entities;

public class TrainerSalary
{
    public Guid Id { get; set; }
    public Guid TrainerUserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidOn { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
