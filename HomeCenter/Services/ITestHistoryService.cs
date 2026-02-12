namespace HomeCenter.Services;

public interface ITestHistoryService
{
    void LogAdded(string fileName, string content);
    void LogModified(string fileName, string content);
    void LogFileDeleted(string fileName, string? lastKnownContent);
    void LogDeletedFromDb(string fileName, string? lastKnownContent);
    void LogFolderDeleted(string folderPath, IEnumerable<string> deletedFileNames);
}
