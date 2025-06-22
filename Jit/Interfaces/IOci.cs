namespace Interfaces;

public interface IOci
{
    public Task<string> RunImage(string imageName, string input);

    Task<string> BuildDockerImageWithRetry(ILlm llmClient, string scriptContent, string scriptFileName, string scriptPath);
}