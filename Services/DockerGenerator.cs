using DockerWrapperTool.Models;
using DockerWrapperTool.Interfaces;
using Microsoft.Extensions.Logging;

namespace DockerWrapperTool.Services;

public class DockerGenerator
{
    private readonly ILLMService _llmService;
    private readonly ILogger _logger;

    public DockerGenerator(ILLMService llmService, ILogger logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<string> GenerateDockerfileAsync(ScriptAnalysis scriptAnalysis, string? readmeContent)
    {
        try
        {
            _logger.LogInformation("Generating Dockerfile for {Language} script: {Name} using {Provider}", 
                scriptAnalysis.Language, scriptAnalysis.Name, _llmService.ProviderName);

            var scriptFileName = $"{scriptAnalysis.Name}{GetFileExtension(scriptAnalysis.Language)}";
            
            var dockerfileContent = await _llmService.GenerateDockerfileAsync(
                scriptAnalysis.Content,
                scriptFileName,
                scriptAnalysis.Language,
                scriptAnalysis.Dependencies,
                readmeContent
            );

            // Validate the generated Dockerfile
            if (string.IsNullOrWhiteSpace(dockerfileContent))
            {
                throw new Exception("Generated Dockerfile is empty");
            }

            // Basic validation - ensure it contains essential Dockerfile keywords
            var requiredKeywords = new[] { "FROM", "COPY", "CMD", "ENTRYPOINT" };
            var hasRequiredKeywords = requiredKeywords.Any(keyword => 
                dockerfileContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (!hasRequiredKeywords)
            {
                _logger.LogWarning("Generated Dockerfile may be invalid - missing essential keywords");
            }

            _logger.LogInformation("Successfully generated Dockerfile using {Provider} ({Length} characters, {Tokens} tokens)", 
                _llmService.ProviderName, dockerfileContent.Length, _llmService.TokensUsed);

            return dockerfileContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Dockerfile");
            throw;
        }
    }

    private string GetFileExtension(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "bash" => ".sh",
            "python" => ".py",
            "javascript" => ".js",
            "ruby" => ".rb",
            "perl" => ".pl",
            "php" => ".php",
            "go" => ".go",
            "rust" => ".rs",
            "java" => ".java",
            "csharp" => ".cs",
            _ => ""
        };
    }
} 