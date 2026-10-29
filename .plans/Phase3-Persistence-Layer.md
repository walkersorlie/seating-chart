# Blazor WASM Seating Chart: Phase 3 - Persistence Layer

## Weeks 2-3 Deliverable: localStorage + AppState Service Working

### Task 3.1: JavaScript Storage Wrapper

Create `src/SeatingChart.Client/wwwroot/js/storage.js`:
```javascript
// storage.js (ES module)
export function get(key)        { return localStorage.getItem(key); }
export function set(key, value) { localStorage.setItem(key, value); }
export function remove(key)     { localStorage.removeItem(key); }
```

Create `src/SeatingChart.Client/wwwroot/js/download.js`:
```javascript
window.downloadText = (filename, text, mime) => {
  const blob = new Blob([text], { type: mime });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
  URL.revokeObjectURL(a.href);
};
```

Add `<script src="../../js/download.js"></script>` to `index.html` before the Blazor bootstrap script.

### Task 3.2: Create IStorageService Interface

Create folder `src/SeatingChart.Client/Services/`:

- **IStorageService.cs**:
```csharp
public interface IStorageService
{
    Task<T?> LoadAsync<T>(string key);
    Task SaveAsync<T>(string key, T value);
}
```

### Task 3.3: Implement LocalStorageService with JS Interop

- **LocalStorageService.cs** in `src/SeatingChart.Client/Services/`:
```csharp
public class LocalStorageService(IJSRuntime js) : IStorageService
{
    private IJSObjectReference? _module;
    private async ValueTask<IJSObjectReference> Mod() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/storage.js");

    public async Task<T?> LoadAsync<T>(string key)
    {
        var json = await (await Mod()).InvokeAsync<string?>("get", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, AppJsonContext.Options);
    }

    public async Task SaveAsync<T>(string key, T value) =>
        await (await Mod()).InvokeVoidAsync("set", key,
            JsonSerializer.Serialize(value, AppJsonContext.Options));
}
```

### Task 3.4: Create JSON Context for Source Generation

- **AppJsonContext.cs** in `src/SeatingChart.Core/Serialization/`:
```csharp
[JsonSerializable(typeof(AppData))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext { }
```

### Task 3.5: Implement AppStateService with Debouncing

- **AppStateService.cs** in `src/SeatingChart.Client/Services/`:
```csharp
public class AppStateService(IStorageService storage)
{
    private const string Key = "seatingchart.v1";
    public AppData Data { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? OnChange;

    private readonly System.Threading.Timer _debounceTimer;

    public async Task InitAsync()
    {
        Data = await storage.LoadAsync<AppData>(Key) ?? Seed();
        Data.Layouts = Data.Layouts.Count > 0 ? Data.Layouts : LayoutPresets.All();
        Loaded = true;
        Notify(false);
    }

    // Methods like AddConflict, RemoveConflict, Assign, Unassign
    // Each calls Notify() to trigger debounce save

    private void Notify(bool save = true) { /* ... */ }
    private async Task SaveDebouncedAsync() { /* ... */ }
}
```

### Task 3.6: Wire up Dependency Injection in Program.cs

Update `src/SeatingChart.Client/Program.cs`:
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<AppStateService>();
// Add other services here

// In OnStarting: 
await state.InitAsync();
```

### Task 3.7: Implement Backup/Restore Functionality

- **BackupService.cs** in `src/SeatingChart.Client/Services/`:
```csharp
public class BackupService(AppStateService state, IJSRuntime js)
{
    public async Task<byte[]> ExportJsonAsync() =>
        Encoding.UTF8.GetBytes(await (await js.InvokeAsync<object>("downloadText", 
            "seating-backup.json", JsonConvert.SerializeObject(state.Data), "application/json"));
    
    public async Task ImportJsonAsync(byte[] data) { /* ... */ }
    public async Task<int> SchemaVersionCheckAsync() { /* ... */ }
}
```

---

## Phase 3 Completion Criteria

Persistence layer must support:
1. App data loads from localStorage on startup (or seeder default)
2. User mutations persist to localStorage with ~300ms debounce
3. Export/Import JSON backup buttons functional
4. Migration hooks in place for future schema changes
5. Corrupt data handled gracefully (fallback to empty or warning)
