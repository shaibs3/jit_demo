namespace Interfaces
{
    public interface IExtractor
    {
        Task<(string exampleInput, string expectedOutput)> ExtractTestDataAsync(
            string readmePath, 
            string scriptContent, 
            string scriptFileName);
    }
} 