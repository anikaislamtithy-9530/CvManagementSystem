using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CvManagement.Web.Services;

public class AutoSaveService : IDisposable
{
    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<AutoSaveService> _logger;

    private Timer? _timer;
    private Func<Task>? _saveCallback;
    private bool _isSaving;
    private bool _isDirty;
    private DateTime _lastSaved = DateTime.UtcNow;

    public event Action? OnSaveStarted;
    public event Action? OnSaveCompleted;
    public event Action<string>? OnSaveFailed;

    public bool IsDirty => _isDirty;
    public DateTime LastSaved => _lastSaved;

    public AutoSaveService(
        ProtectedLocalStorage storage,
        ILogger<AutoSaveService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public void Start(Func<Task> saveCallback, int intervalSeconds = 5)
    {
        Stop();
        _saveCallback = saveCallback;
        _timer = new Timer(
            async _ => await TickAsync(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds));

        _logger.LogInformation("AutoSave started with {Interval}s interval", intervalSeconds);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void MarkDirty() => _isDirty = true;

    public async Task ForceSaveAsync()
    {
        if (_isSaving || _saveCallback == null) return;
        await TickAsync();
    }

    private async Task TickAsync()
    {
        if (_isSaving || !_isDirty || _saveCallback == null) return;

        _isSaving = true;
        try
        {
            OnSaveStarted?.Invoke();
            await _saveCallback.Invoke();
            _isDirty = false;
            _lastSaved = DateTime.UtcNow;
            OnSaveCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoSave failed");
            OnSaveFailed?.Invoke(ex.Message);
        }
        finally
        {
            _isSaving = false;
        }
    }

    public void Dispose() => Stop();
}