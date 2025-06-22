namespace Interfaces
{
    public interface ITestDataExtractor
    {
        Task<(string exampleInput, string expectedOutput)> ExtractTestDataAsync(
            string scriptContent, 
            string scriptFileName);
    }
} 