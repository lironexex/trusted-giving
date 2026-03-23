// Services/AmutaService.cs
// Fetches and filters Amutot (Israeli non-profits) from the Israeli government open data API.
// Applies two trust filters before returning results:
//   1. The organization filed a financial report within the last 3 years
//   2. The organization has a valid ניהול תקין (proper management) approval for the current year

using System.Net.Http.Json;
using System.Text.Json;
using TrustedGiving.Models;

namespace TrustedGiving.Services
{
    public class AmutaService
    {
        // Main amutot dataset from data.gov.il (Registrar of Amutot)
        private const string AmutotResourceId = "be5b7935-3922-45d4-9638-08871b17ec95";

        // ניהול תקין dataset — one row per amuta per year, indicates proper management approval
        private const string NihalTakinResourceId = "cb12ac14-7429-4268-bc03-460f48157858";

        private const string BaseUrl = "https://data.gov.il/api/3/action/datastore_search";

        // Organizations that haven't reported within this many years are filtered out
        private const int ReportingWindowYears = 3;

        private readonly HttpClient _http;

        public AmutaService(HttpClient http)
        {
            _http = http;
        }

        // Returns a filtered list of active, trusted amutot matching the search query and optional category.
        // Filters out: inactive orgs, orgs with stale reports, orgs without valid ניהול תקין.
        public async Task<List<Amuta>> GetAmutotAsync(int limit = 20, int offset = 0, string search = "", string category = "")
        {
            var url = $"{BaseUrl}?resource_id={AmutotResourceId}&limit={limit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&q={Uri.EscapeDataString(search)}";

            // Apply category filter via the API filters parameter when a category is selected
            if (!string.IsNullOrWhiteSpace(category))
            {
                var filters = Uri.EscapeDataString($"{{\"סיווג פעילות ענפי\":\"{category}\"}}");
                url += $"&filters={filters}";
            }

            var response = await _http.GetFromJsonAsync<JsonElement>(url);
            var records = response.GetProperty("result").GetProperty("records");

            int currentYear = DateTime.Now.Year;
            var candidates = new List<(Amuta amuta, long amutaNumber)>();

            foreach (var r in records.EnumerateArray())
            {
                // Skip non-active organizations
                var status = r.GetProperty("סטטוס עמותה").GetString() ?? "";
                if (status != "רשומה") continue;

                // Filter 1: must have filed a financial report within the last 3 years
                var lastReportYear = r.TryGetProperty("שנת דיווח דוח כספי אחרון", out var yr) && yr.ValueKind == JsonValueKind.Number
                    ? yr.GetInt32() : (int?)null;
                if (!lastReportYear.HasValue || currentYear - lastReportYear.Value > ReportingWindowYears)
                    continue;

                var amutaNumber = r.GetProperty("מספר עמותה").GetInt64();

                candidates.Add((new Amuta
                {
                    Id = r.GetProperty("_id").GetInt32(),
                    AmutaNumber = amutaNumber,
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
                    LastFinancialReportYear = lastReportYear,
                    AnnualRevenue = r.TryGetProperty("מחזור כספי (הכנסות)", out var rev) && rev.ValueKind == JsonValueKind.Number ? rev.GetDouble() : null,
                    TotalExpenses = r.TryGetProperty("סך הוצאות העמותה", out var exp) && exp.ValueKind == JsonValueKind.Number ? exp.GetDouble() : null,
                    VolunteerCount = r.TryGetProperty("כמות מתנדבים", out var vol) && vol.ValueKind == JsonValueKind.Number ? vol.GetInt32() : null,
                    EmployeeCount = r.TryGetProperty("כמות עובדים", out var emp) && emp.ValueKind == JsonValueKind.Number ? emp.GetInt32() : null,
                    MemberCount = r.TryGetProperty("מספר חברי עמותה", out var mem) && mem.ValueKind == JsonValueKind.Number ? mem.GetInt32() : null,
                }, amutaNumber));
            }

            if (!candidates.Any()) return new List<Amuta>();

            // Filter 2: cross-reference with ניהול תקין dataset
            var validAmutaNumbers = await GetAmutotWithValidNihalTakinAsync(
                candidates.Select(c => c.amutaNumber).ToList(),
                currentYear
            );

            return candidates
                .Where(c => validAmutaNumbers.Contains(c.amutaNumber))
                .Select(c => c.amuta)
                .ToList();
        }

        // Returns the total count of amutot that pass both trust filters.
        // Used to display an accurate stat on the frontend rather than the raw API total.
        public async Task<int> GetTrustedCountAsync()
        {
            int currentYear = DateTime.Now.Year;
            int offset = 0;
            int batchSize = 1000;
            int trustedCount = 0;

            while (true)
            {
                var url = $"{BaseUrl}?resource_id={AmutotResourceId}&limit={batchSize}&offset={offset}";
                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                var result = response.GetProperty("result");
                var records = result.GetProperty("records");

                var batch = records.EnumerateArray().ToList();
                if (!batch.Any()) break;

                // Apply filter 1: active + reported in last 3 years
                var candidates = batch
                    .Where(r => r.GetProperty("סטטוס עמותה").GetString() == "רשומה")
                    .Where(r => r.TryGetProperty("שנת דיווח דוח כספי אחרון", out var yr) &&
                                yr.ValueKind == JsonValueKind.Number &&
                                currentYear - yr.GetInt32() <= ReportingWindowYears)
                    .Select(r => r.GetProperty("מספר עמותה").GetInt64())
                    .ToList();

                // Apply filter 2: ניהול תקין
                var valid = await GetAmutotWithValidNihalTakinAsync(candidates, currentYear);
                trustedCount += valid.Count;

                offset += batchSize;
            }

            return trustedCount;
        }

        // Returns all distinct non-empty סיווג פעילות ענפי values found in the dataset.
        // Fetches a large batch (no search/filter) so the list reflects the full category universe.
        public async Task<List<string>> GetCategoriesAsync()
        {
            var url = $"{BaseUrl}?resource_id={AmutotResourceId}&limit=1000&offset=0";
            var response = await _http.GetFromJsonAsync<JsonElement>(url);
            var records = response.GetProperty("result").GetProperty("records");

            return records
                .EnumerateArray()
                .Select(r => r.GetProperty("סיווג פעילות ענפי").GetString() ?? "")
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        // Queries the ניהול תקין dataset in parallel for each amuta number.
        // Returns the set of amuta numbers that have a signed approval (נחתם אישור) for the current year or later.
        private async Task<HashSet<long>> GetAmutotWithValidNihalTakinAsync(List<long> amutaNumbers, int currentYear)
        {
            var result = new HashSet<long>();

            var tasks = amutaNumbers.Select(async number =>
            {
                // מספר עמותה is stored as text in this dataset, so the filter value must be a string
                var filters = Uri.EscapeDataString($"{{\"מספר עמותה\":\"{number}\"}}");
                var url = $"{BaseUrl}?resource_id={NihalTakinResourceId}&filters={filters}";

                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                var records = response.GetProperty("result").GetProperty("records");

                // Valid if any row for this amuta has approval for the current year or beyond
                var hasValidApproval = records.EnumerateArray().Any(r =>
                    r.TryGetProperty("שנת האישור", out var yr) &&
                    yr.ValueKind == JsonValueKind.Number &&
                    yr.GetInt32() >= currentYear &&
                    r.GetProperty("האם יש אישור").GetString() == "נחתם אישור"
                );

                if (hasValidApproval)
                    lock (result) { result.Add(number); }
            });

            await Task.WhenAll(tasks);
            return result;
        }
    }
}
