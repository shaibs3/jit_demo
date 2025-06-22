namespace Interfaces;

public interface IOci
{
    public Task<string> BuildImage(string dockerfileContent, string scriptPath, string scriptFileName);

    public Task<string> RunImage(string imageName, string input);

}