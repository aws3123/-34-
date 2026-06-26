using ISO11820.App.Infrastructure.FileStorage;

namespace ISO11820.Tests.Persistence;

public sealed class CsvSampleWriterTests
{
    [Fact]
    public void BuildTestDirectory_Should_Combine_BaseDirectory_ProductId_And_TestId()
    {
        var writer = new CsvSampleWriter("TestData");

        var path = writer.BuildTestDirectory("P001", "T001");

        Assert.Contains("P001", path);
        Assert.Contains("T001", path);
    }
}
