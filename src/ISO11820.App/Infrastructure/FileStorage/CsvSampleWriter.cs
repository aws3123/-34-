namespace ISO11820.App.Infrastructure.FileStorage;

public sealed class CsvSampleWriter
{
    private readonly string _baseDirectory;

    public CsvSampleWriter(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public string BuildTestDirectory(string productId, string testId)
    {
        return Path.Combine(_baseDirectory, productId, testId);
    }
}
