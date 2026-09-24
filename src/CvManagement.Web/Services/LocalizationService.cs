using CvManagement.Web.Resources;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace CvManagement.Web.Services;

public class LocalizationService
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ProtectedLocalStorage _storage;

    public string CurrentLanguage { get; private set; } = "en";

    public event Action? OnLanguageChanged;

    public LocalizationService(
        IStringLocalizer<SharedResource> localizer,
        ProtectedLocalStorage storage)
    {
        _localizer = localizer;
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var result = await _storage.GetAsync<string>("language");
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                CurrentLanguage = result.Value;
            }
        }
        catch { CurrentLanguage = "en"; }

        ApplyCulture(CurrentLanguage);
    }

    public async Task SetLanguageAsync(string lang)
    {
        CurrentLanguage = lang;
        await _storage.SetAsync("language", lang);
        ApplyCulture(lang);
        OnLanguageChanged?.Invoke();
    }

    public string this[string key] => _localizer[key].Value;
    public string Get(string key) => _localizer[key].Value;

    private void ApplyCulture(string lang)
    {
        var culture = new CultureInfo(lang);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}