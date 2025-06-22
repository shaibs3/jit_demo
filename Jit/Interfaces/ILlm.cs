namespace Interfaces
{
    public interface ILlm
    {
        Task<string> GetTestCommandAndExpectedOutput(string prompt);

        Task<string> CreateDockerFile(string scriptContent, string scriptFileName);

        Task<string> FixDockerFile(string scriptContent, string scriptFileName, string originalDockerfile, string buildError);

        Task<(string exampleInput, string expectedOutput)> ExtractExampleFromReadme(string readmePath);

        Task<bool> ValidateExtractedExample(string scriptContent, string scriptFileName, string exampleInput, string expectedOutput);

        Task<(string exampleInput, string expectedOutput)> GenerateFallbackTestData(string scriptContent, string scriptFileName);

        Task<(string exampleInput, string expectedOutput)> GenerateTestDataFromScriptAnalysis(
            string scriptContent, string scriptFileName, string actualOutput, string previousInput, string previousExpectedOutput);
    }
}