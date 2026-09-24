using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CvManagement.Web.Services;

public class LayoutService
{
    private readonly ProtectedLocalStorage _storage;

    public bool IsDarkMode { get; set; } = false;
    public string Language { get; set; } = "en";

    public event Action? OnChange;

    public LayoutService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var dark = await _storage.GetAsync<bool>("darkMode");
            if (dark.Success)
                IsDarkMode = dark.Value;

            var lang = await _storage.GetAsync<string>("language");
            if (lang.Success && !string.IsNullOrEmpty(lang.Value))
                Language = lang.Value!;
        }
        catch
        {
            IsDarkMode = false;
            Language = "en";
        }
    }

    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _storage.SetAsync("darkMode", IsDarkMode);
        NotifyStateChanged();
    }

    public async Task SetLanguageAsync(string lang)
    {
        Language = lang;
        await _storage.SetAsync("language", lang);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}