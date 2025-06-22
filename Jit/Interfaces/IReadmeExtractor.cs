namespace Interfaces
{
    public interface IReadmeExtractor
    {
        Task<(string exampleInput, string expectedOutput)> ExtractExampleAsync(string readmePath);
    }
}