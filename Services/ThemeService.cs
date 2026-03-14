namespace TrustedGiving.Services
{
    public class ThemeService
    {
        public bool IsDark { get; private set; } = true;
        public string Theme => IsDark ? "dark" : "light";
        public event Action? OnChange;

        public void Toggle()
        {
            IsDark = !IsDark;
            OnChange?.Invoke();
        }
    }
}