namespace TrustedGiving.Models
{
    public class Amuta
    {
        public int Id { get; set; }
        public long AmutaNumber { get; set; }
        public string NameHebrew { get; set; } = string.Empty;
        public string NameEnglish { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ActivityCategory { get; set; } = string.Empty;
        public string SecondaryActivity { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Goals { get; set; } = string.Empty;
        public double? AnnualRevenue { get; set; }
        public double? TotalExpenses { get; set; }
        public int? VolunteerCount { get; set; }
        public int? EmployeeCount { get; set; }
        public int? MemberCount { get; set; }
        public string ActivityAreas { get; set; } = string.Empty;
        public string RegistrationDate { get; set; } = string.Empty;
    }
}