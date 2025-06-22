namespace Interfaces
{
    public interface IValidator
    {
        Task RunTestAndValidateAsync(
            IOci ociClient, 
            string imageName, 
            string exampleInput, 
            string expectedOutput, 
            string scriptContent, 
            string scriptFileName);
    }
} 