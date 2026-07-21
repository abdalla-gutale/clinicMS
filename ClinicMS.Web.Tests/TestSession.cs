using Microsoft.AspNetCore.Http;

namespace ClinicMS.Web.Tests;

/// <summary>Minimal in-memory ISession so RequirePermissionFilter's session reads/writes work in
/// tests without a real session middleware pipeline behind DefaultHttpContext.</summary>
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _store.Clear();
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
