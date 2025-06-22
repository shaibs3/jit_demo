namespace DockerWrapperTool.Interfaces;

/// <summary>
/// Interface for LLM services to make the system vendor-agnostic
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Sends a prompt to the LLM and returns the response
    /// </summary>
    /// <param name="prompt">The prompt to send to the LLM</param>
    /// <param name="maxTokens">Maximum tokens for the response</param>
    /// <param name="temperature">Temperature for response generation (0.0 to 1.0)</param>
    /// <returns>The LLM response</returns>
    Task<string> SendPromptAsync(string prompt, int maxTokens = 1000, double temperature = 0.1);
    
    /// <summary>
    /// Generates a Dockerfile for a script and handles response parsing internally
    /// </summary>
    /// <param name="scriptContent">The content of the script</param>
    /// <param name="scriptFileName">The name of the script file</param>
    /// <param name="language">The programming language of the script</param>
    /// <param name="dependencies">List of dependencies</param>
    /// <param name="readmeContent">Optional README content for context</param>
    /// <returns>The generated Dockerfile content</returns>
    Task<string> GenerateDockerfileAsync(string scriptContent, string scriptFileName, string language, List<string> dependencies, string? readmeContent = null);
    
    /// <summary>
    /// Gets the name of the LLM service provider
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Gets the current token usage for cost tracking
    /// </summary>
    long TokensUsed { get; }
    
    /// <summary>
    /// Resets the token usage counter
    /// </summary>
    void ResetTokenUsage();
} 