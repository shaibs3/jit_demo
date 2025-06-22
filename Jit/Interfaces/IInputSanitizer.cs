namespace Interfaces
{
    using Models;

    public interface IInputSanitizer
    {
        SanitizationResult SanitizeScriptContent(string scriptContent, string scriptFileName);
        SanitizationResult SanitizeTestData(string input, string expectedOutput);
        bool IsLikelyPromptInjection(string input);
        List<string> GetSecurityRecommendations(SanitizationResult result);
    }
} 