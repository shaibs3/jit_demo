using Interfaces;

namespace Concrete
{
    public class ReadmeExtractor : IReadmeExtractor
    {
        private readonly ILlm _llmClient;

        public ReadmeExtractor(ILlm llmClient)
        {
            _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        }

        public async Task<(string exampleInput, string expectedOutput)> ExtractExampleAsync(string readmePath)
        {
            if (string.IsNullOrEmpty(readmePath))
                throw new ArgumentException("Readme path cannot be null or empty", nameof(readmePath));

            if (!File.Exists(readmePath))
                throw new FileNotFoundException($"README file not found: {readmePath}");
            
            // Use the LLM client to extract example input and expected output
            (string exampleInput, string expectedOutput) result = await _llmClient.ExtractExampleFromReadme(readmePath);

            return result;
        }
    }
}