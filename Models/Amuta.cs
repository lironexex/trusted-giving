// Models/Amuta.cs
// Represents a single Amuta (Israeli non-profit organization) with data sourced
// from the Israeli government open data API (data.gov.il).

namespace TrustedGiving.Models
{
    public class Amuta
    {
        // --- Identity ---
        public int Id { get; set; }
        public long AmutaNumber { get; set; }
        public string NameHebrew { get; set; } = string.Empty;
        public string NameEnglish { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RegistrationDate { get; set; } = string.Empty;

        // --- Classification ---
        public string ActivityCategory { get; set; } = string.Empty;
        public string SecondaryActivity { get; set; } = string.Empty;
        public string ActivityAreas { get; set; } = string.Empty;

        // --- Location ---
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;

        // --- Description ---
        public string Goals { get; set; } = string.Empty;

        // --- Financials ---
        public double? AnnualRevenue { get; set; }
        public double? TotalExpenses { get; set; }

        // --- Reporting & Trust ---
        // The most recent year a financial report was filed; used to derive דיווח בשלוש שנים האחרונות
        public int? LastFinancialReportYear { get; set; }

        // --- People ---
        public int? VolunteerCount { get; set; }
        public int? EmployeeCount { get; set; }
        public int? MemberCount { get; set; }
    }
}