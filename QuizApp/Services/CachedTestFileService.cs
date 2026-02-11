using Microsoft.Extensions.Caching.Memory;
using QuizApp.Models;

namespace QuizApp.Services;

/// <summary>
/// Обёртка над ITestFileService с кэшированием вызовов SyncTopicsFromFiles.
/// Повторные вызовы в течение TTL пропускаются.
/// </summary>
public class CachedTestFileService : ITestFileService
{
    private const string CacheKey = "QuizApp:TopicsSync";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly ITestFileService _inner;
    private readonly IMemoryCache _cache;

    public CachedTestFileService(ITestFileService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public void SyncTopicsFromFiles(bool force = false)
    {
        if (!force && _cache.TryGetValue(CacheKey, out _))
            return;

        _inner.SyncTopicsFromFiles(force);
        _cache.Set(CacheKey, true, CacheDuration);
    }

    public IReadOnlyList<QuestionModel> LoadQuestionsForTopic(Topic topic)
        => _inner.LoadQuestionsForTopic(topic);
}
