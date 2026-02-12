using HomeCenter.Data;
using HomeCenter.Models;

namespace HomeCenter.Services;

public class TestHistoryService : ITestHistoryService
{
    private readonly ApplicationDbContext _db;

    public TestHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public void LogAdded(string fileName, string content)
    {
        _db.TestHistory.Add(new TestHistoryEntry
        {
            FileName = fileName,
            Action = TestHistoryActionType.Added,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void LogModified(string fileName, string content)
    {
        _db.TestHistory.Add(new TestHistoryEntry
        {
            FileName = fileName,
            Action = TestHistoryActionType.Modified,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void LogFileDeleted(string fileName, string? lastKnownContent)
    {
        _db.TestHistory.Add(new TestHistoryEntry
        {
            FileName = fileName,
            Action = TestHistoryActionType.FileDeleted,
            Content = lastKnownContent,
            Timestamp = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void LogDeletedFromDb(string fileName, string? lastKnownContent)
    {
        _db.TestHistory.Add(new TestHistoryEntry
        {
            FileName = fileName,
            Action = TestHistoryActionType.DeletedFromDb,
            Content = lastKnownContent,
            Timestamp = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void LogFolderDeleted(string folderPath, IEnumerable<string> deletedFileNames)
    {
        foreach (var fileName in deletedFileNames)
        {
            _db.TestHistory.Add(new TestHistoryEntry
            {
                FileName = fileName,
                FolderPath = folderPath,
                Action = TestHistoryActionType.FolderDeleted,
                Content = null,
                Timestamp = DateTime.UtcNow
            });
        }
        _db.SaveChanges();
    }
}
