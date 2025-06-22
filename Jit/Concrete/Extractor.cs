using Interfaces;

namespace Concrete
{
    public class Extractor : IExtractor
    {
        private readonly IReadmeExtractor _readmeExtractor;
        private readonly ILlm _llmClient;

        public Extractor(IReadmeExtractor readmeExtractor, ILlm llmClient)
        {
            _readmeExtractor = readmeExtractor;
            _llmClient = llmClient;
        }

        public async Task<(string exampleInput, string expectedOutput)> ExtractTestDataAsync(
            string readmePath, 
            string scriptContent, 
            string scriptFileName)
        {
            try
            {
                // First attempt: extract from README
                var (exampleInput, expectedOutput) = await _readmeExtractor.ExtractExampleAsync(readmePath);

                Console.WriteLine("Example input extracted from README: " + exampleInput);
                Console.WriteLine("Expected output extracted from README: " + expectedOutput);

                // Validate the extracted data
                bool isValid = await _llmClient.ValidateExtractedExample(scriptContent, scriptFileName, exampleInput, expectedOutput);

                if (isValid)
                {
                    Console.WriteLine("✅ README extraction validated successfully");
                    return (exampleInput, expectedOutput);
                }
                else
                {
                    Console.WriteLine("❌ README extraction validation failed, trying fallback generation...");
                    return await GenerateFallbackTestDataAsync(scriptContent, scriptFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ README extraction failed: {ex.Message}");
                Console.WriteLine("🔄 Attempting fallback test data generation...");
                return await GenerateFallbackTestDataAsync(scriptContent, scriptFileName);
            }
        }

        private async Task<(string exampleInput, string expectedOutput)> GenerateFallbackTestDataAsync(
            string scriptContent, 
            string scriptFileName)
        {
            try
            {
                var (exampleInput, expectedOutput) = await _llmClient.GenerateFallbackTestData(scriptContent, scriptFileName);

                Console.WriteLine("✅ Fallback test data generated successfully");
                return (exampleInput, expectedOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fallback test data generation failed: {ex.Message}");
                Console.WriteLine("🔧 Would you like to enter test data manually? (y/n): ");
                string? response = Console.ReadLine()?.ToLower();

                if (response == "y" || response == "yes")
                {
                    return GetManualTestData();
                }
                else
                {
                    throw new InvalidOperationException("Failed to generate test data and manual entry was declined", ex);
                }
            }
        }

        private (string exampleInput, string expectedOutput) GetManualTestData()
        {
            Console.WriteLine("🔧 Manual test data entry mode:");
            Console.Write("Enter example input: ");
            string exampleInput = Console.ReadLine() ?? "";

            Console.Write("Enter expected output: ");
            string expectedOutput = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(exampleInput) || string.IsNullOrWhiteSpace(expectedOutput))
            {
                throw new InvalidOperationException("Manual test data cannot be empty");
            }

            Console.WriteLine("✅ Manual test data accepted");
            return (exampleInput, expectedOutput);
        }
    }
} 