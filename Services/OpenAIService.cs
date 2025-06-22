using OpenAI;
using Microsoft.Extensions.Logging;
using DockerWrapperTool.Interfaces;
using System.Text.Json;

namespace DockerWrapperTool.Services;

public class OpenAIService : ILLMService
{
    private readonly OpenAIClient _client;
    private readonly ILogger _logger;
    private long _tokensUsed;

    public OpenAIService(string apiKey, ILogger logger)
    {
        _client = new OpenAIClient(apiKey);
        _logger = logger;
        _tokensUsed = 0;
    }

    public string ProviderName => "OpenAI";

    public long TokensUsed => _tokensUsed;

    public void ResetTokenUsage()
    {
        _tokensUsed = 0;
    }

    public async Task<string> SendPromptAsync(string prompt, int maxTokens = 1000, double temperature = 0.1)
    {
        try
        {
            _logger.LogDebug("Sending prompt to OpenAI (length: {Length} chars)", prompt.Length);

            var response = await _client.GetChatCompletionsAsync(new ChatCompletionsRequest
            {
                Model = "gpt-4",
                Messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, "You are an expert Docker engineer. Generate only valid Dockerfile content without any explanations or markdown formatting."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                MaxTokens = maxTokens,
                Temperature = temperature
            });

            var content = response.Choices[0].Message.Content;
            
            // Track token usage (approximate)
            _tokensUsed += response.Usage?.TotalTokens ?? 0;
            
            _logger.LogDebug("Received response from OpenAI (length: {Length} chars, tokens: {Tokens})", 
                content?.Length ?? 0, response.Usage?.TotalTokens ?? 0);
            
            return content ?? throw new Exception("No content received from OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prompt to OpenAI");
            throw;
        }
    }

    public async Task<string> GenerateDockerfileAsync(string scriptContent, string scriptFileName, string language, List<string> dependencies, string? readmeContent = null)
    {
        try
        {
            var prompt = BuildDockerfilePrompt(scriptContent, scriptFileName, language, dependencies, readmeContent);
            var rawResponse = await SendPromptAsync(prompt);
            
            // Parse the JSON response and extract the Dockerfile content
            var dockerfileContent = ParseDockerfileFromResponse(rawResponse);
            
            _logger.LogInformation("Successfully generated Dockerfile for {FileName} using {Provider} ({Length} characters, {Tokens} tokens)", 
                scriptFileName, ProviderName, dockerfileContent.Length, TokensUsed);
            
            return dockerfileContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Dockerfile for {FileName}", scriptFileName);
            throw;
        }
    }

    private string ParseDockerfileFromResponse(string rawResponse)
    {
        try
        {
            // Parse the JSON and extract the message content
            using var doc = JsonDocument.Parse(rawResponse);
            var dockerfile = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(dockerfile))
            {
                throw new Exception("Dockerfile content is empty in the response");
            }

            return dockerfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Dockerfile from response");
            throw new Exception($"Failed to parse Dockerfile from response: {ex.Message}");
        }
    }

    private string BuildDockerfilePrompt(string scriptContent, string scriptFileName, string language, List<string> dependencies, string? readmeContent)
    {
        var prompt = $"""
You are an expert DevOps engineer.
Given the following script file named `{scriptFileName}`, generate a complete Dockerfile that will run this script.
Ensure the Dockerfile installs all necessary dependencies for the script to work, sets the correct entrypoint, and is as minimal as possible.

SCRIPT CONTENT:
{scriptContent}

LANGUAGE: {language}
DEPENDENCIES: {string.Join(", ", dependencies)}

""";

        if (!string.IsNullOrEmpty(readmeContent))
        {
            prompt += $"""
README CONTENT:
{readmeContent}

""";
        }

        prompt += """
REQUIREMENTS:
1. Use the most appropriate base image for the language
2. Install only necessary dependencies
3. Copy the script to the container
4. Set up proper entrypoint/command to run the script
5. Make the script executable if needed
6. Use multi-stage builds if beneficial for size optimization
7. Follow Docker best practices
8. Ensure the script can receive command line arguments

Please output only the Dockerfile content without any explanations or markdown formatting.
""";

        return prompt;
    }
} 