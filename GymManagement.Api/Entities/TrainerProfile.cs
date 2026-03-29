namespace GymManagement.Api.Entities;

public class TrainerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public DateTime JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
}
