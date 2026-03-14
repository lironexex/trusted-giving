using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using TrustedGiving.Services;

namespace trusted_giving
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddHttpClient<AmutaService>();
            builder.Services.AddScoped<AmutaService>();
            builder.Services.AddMudServices();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<LanguageService>();

            return builder.Build();
        }
    }
}
