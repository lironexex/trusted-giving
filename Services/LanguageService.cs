namespace TrustedGiving.Services
{
    public class LanguageService
    {
        public bool IsHebrew { get; private set; } = true;
        public string Direction => IsHebrew ? "rtl" : "ltr";
        public event Action? OnChange;

        public void Toggle()
        {
            IsHebrew = !IsHebrew;
            OnChange?.Invoke();
        }

        public string Get(string hebrew, string english)
            => IsHebrew ? hebrew : english;
    }
}