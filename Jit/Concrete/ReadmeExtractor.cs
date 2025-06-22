using Interfaces;

namespace Concrete
{
    public class ReadmeExtractor : IReadmeExtractor
    {
        private readonly ILlm _llmClient;
        private readonly string _readmePath;

        public ReadmeExtractor(ILlm llmClient, string readmePath)
        {
            _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
            _readmePath = readmePath ?? throw new ArgumentNullException(nameof(readmePath));
            if (!File.Exists(_readmePath))
                throw new FileNotFoundException($"README file not found: {_readmePath}");
        }

        public async Task<(string exampleInput, string expectedOutput)> ExtractReadmeTestDataAsync()
        {
            // Use the LLM client to extract example input and expected output
            (string exampleInput, string expectedOutput) result = await _llmClient.ExtractExampleFromReadme(_readmePath);
            return result;
        }
    }
}