using System.Net.Http.Json;
using System.Text.Json;
using TrustedGiving.Models;

namespace TrustedGiving.Services
{
    public class AmutaService
    {
        private readonly HttpClient _http;
        private const string ResourceId = "be5b7935-3922-45d4-9638-08871b17ec95";
        private const string BaseUrl = "https://data.gov.il/api/3/action/datastore_search";

        public AmutaService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Amuta>> GetAmutotAsync(int limit = 20, int offset = 0, string search = "")
        {
            var url = $"{BaseUrl}?resource_id={ResourceId}&limit={limit}&offset={offset}";

            if (!string.IsNullOrWhiteSpace(search))
                url += $"&q={Uri.EscapeDataString(search)}";

            var response = await _http.GetFromJsonAsync<JsonElement>(url);
            var records = response.GetProperty("result").GetProperty("records");

            var list = new List<Amuta>();
            foreach (var r in records.EnumerateArray())
            {
                // Only show active orgs
                var status = r.GetProperty("סטטוס עמותה").GetString() ?? "";
                if (status != "רשומה") continue;

                list.Add(new Amuta
                {
                    Id = r.GetProperty("_id").GetInt32(),
                    AmutaNumber = r.GetProperty("מספר עמותה").GetInt64(),
                    NameHebrew = r.GetProperty("שם עמותה בעברית").GetString() ?? "",
                    NameEnglish = r.GetProperty("שם עמותה באנגלית").GetString() ?? "",
                    Status = status,
                    ActivityCategory = r.GetProperty("סיווג פעילות ענפי").GetString() ?? "",
                    SecondaryActivity = r.GetProperty("תחום פעילות משני").GetString() ?? "",
                    City = r.GetProperty("כתובת - ישוב").GetString() ?? "",
                    Street = r.GetProperty("כתובת - רחוב").GetString() ?? "",
                    Goals = r.GetProperty("מטרות עמותה").GetString() ?? "",
                    ActivityAreas = r.GetProperty("איזורי פעילות").GetString() ?? "",
                    RegistrationDate = r.GetProperty("תאריך רישום עמותה").GetString() ?? "",
                    AnnualRevenue = r.TryGetProperty("מחזור כספי (הכנסות)", out var rev) && rev.ValueKind == JsonValueKind.Number ? rev.GetDouble() : null,
                    TotalExpenses = r.TryGetProperty("סך הוצאות העמותה", out var exp) && exp.ValueKind == JsonValueKind.Number ? exp.GetDouble() : null,
                    VolunteerCount = r.TryGetProperty("כמות מתנדבים", out var vol) && vol.ValueKind == JsonValueKind.Number ? vol.GetInt32() : null,
                    EmployeeCount = r.TryGetProperty("כמות עובדים", out var emp) && emp.ValueKind == JsonValueKind.Number ? emp.GetInt32() : null,
                    MemberCount = r.TryGetProperty("מספר חברי עמותה", out var mem) && mem.ValueKind == JsonValueKind.Number ? mem.GetInt32() : null,
                });
            }
            return list;
        }
    }
}