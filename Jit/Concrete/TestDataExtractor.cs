using Interfaces;

namespace Concrete
{
    using System.Security;

    public class TestDataExtractor : ITestDataExtractor
    {
        private readonly IReadmeExtractor _readmeExtractor;
        private readonly ILlm _llmClient;
        private readonly IInputSanitizer _sanitizer;

        public TestDataExtractor(IReadmeExtractor readmeExtractor, ILlm llmClient, IInputSanitizer sanitizer)
        {
            _readmeExtractor = readmeExtractor;
            _llmClient = llmClient;
            _sanitizer = sanitizer;
        }

        public async Task<(string exampleInput, string expectedOutput)> ExtractTestDataAsync(
            string scriptContent, 
            string scriptFileName)
        {
            // Sanitize script content first
            var scriptSanitization = _sanitizer.SanitizeScriptContent(scriptContent, scriptFileName);
            if (!scriptSanitization.IsValid)
            {
                Console.WriteLine("⚠️ Security warning: Script content contains potential threats:");
                foreach (var threat in scriptSanitization.DetectedThreats)
                {
                    Console.WriteLine($"   - {threat}");
                }
                foreach (var warning in scriptSanitization.Warnings)
                {
                    Console.WriteLine($"   - {warning}");
                }
                
                var recommendations = _sanitizer.GetSecurityRecommendations(scriptSanitization);
                foreach (var recommendation in recommendations)
                {
                    Console.WriteLine($"   💡 {recommendation}");
                }
                
                throw new SecurityException("Script content failed security validation");
            }

            // Use sanitized script content
            var sanitizedScriptContent = scriptSanitization.SanitizedInput;

            try
            {
                // First attempt: extract from README (no path needed)
                var (exampleInput, expectedOutput) = await _readmeExtractor.ExtractReadmeTestDataAsync();

                // Sanitize extracted test data
                var testDataSanitization = _sanitizer.SanitizeTestData(exampleInput, expectedOutput);
                if (!testDataSanitization.IsValid)
                {
                    Console.WriteLine("⚠️ Security warning: Extracted test data contains potential threats:");
                    foreach (var threat in testDataSanitization.DetectedThreats)
                    {
                        Console.WriteLine($"   - {threat}");
                    }
                    
                    // Check if it's likely a prompt injection attempt
                    if (_sanitizer.IsLikelyPromptInjection(exampleInput) || _sanitizer.IsLikelyPromptInjection(expectedOutput))
                    {
                        Console.WriteLine("🚨 High probability of prompt injection attempt detected!");
                        throw new SecurityException("Prompt injection attempt detected in test data");
                    }
                    
                    // For warnings, continue but log them
                    foreach (var warning in testDataSanitization.Warnings)
                    {
                        Console.WriteLine($"   - {warning}");
                    }
                }

                // Use sanitized test data
                exampleInput = testDataSanitization.SanitizedInput;
                expectedOutput = testDataSanitization.SanitizedExpectedOutput;

                Console.WriteLine("Example input extracted from README: " + exampleInput);
                Console.WriteLine("Expected output extracted from README: " + expectedOutput);

                // Validate the extracted data
                bool isValid = await _llmClient.ValidateExtractedExample(sanitizedScriptContent, scriptFileName, exampleInput, expectedOutput);

                if (isValid)
                {
                    Console.WriteLine("✅ README extraction validated successfully");
                    return (exampleInput, expectedOutput);
                }
                else
                {
                    Console.WriteLine("❌ README extraction validation failed, trying fallback generation...");
                    return await GenerateFallbackTestDataAsync(sanitizedScriptContent, scriptFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ README extraction failed: {ex.Message}");
                Console.WriteLine("🔄 Attempting fallback test data generation...");
                return await GenerateFallbackTestDataAsync(sanitizedScriptContent, scriptFileName);
            }
        }

        private async Task<(string exampleInput, string expectedOutput)> GenerateFallbackTestDataAsync(
            string scriptContent, 
            string scriptFileName)
        {
            try
            {
                var (exampleInput, expectedOutput) = await _llmClient.GenerateFallbackTestData(scriptContent, scriptFileName);

                // Sanitize fallback test data
                var testDataSanitization = _sanitizer.SanitizeTestData(exampleInput, expectedOutput);
                if (!testDataSanitization.IsValid)
                {
                    Console.WriteLine("⚠️ Security warning: Fallback test data contains potential threats:");
                    foreach (var threat in testDataSanitization.DetectedThreats)
                    {
                        Console.WriteLine($"   - {threat}");
                    }
                    
                    if (_sanitizer.IsLikelyPromptInjection(exampleInput) || _sanitizer.IsLikelyPromptInjection(expectedOutput))
                    {
                        Console.WriteLine("🚨 High probability of prompt injection attempt detected in fallback data!");
                        throw new SecurityException("Prompt injection attempt detected in fallback test data");
                    }
                }

                // Use sanitized data
                exampleInput = testDataSanitization.SanitizedInput;
                expectedOutput = testDataSanitization.SanitizedExpectedOutput;

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

            // Sanitize manual input
            var testDataSanitization = _sanitizer.SanitizeTestData(exampleInput, expectedOutput);
            if (!testDataSanitization.IsValid)
            {
                Console.WriteLine("⚠️ Security warning: Manual test data contains potential threats:");
                foreach (var threat in testDataSanitization.DetectedThreats)
                {
                    Console.WriteLine($"   - {threat}");
                }
                
                if (_sanitizer.IsLikelyPromptInjection(exampleInput) || _sanitizer.IsLikelyPromptInjection(expectedOutput))
                {
                    Console.WriteLine("🚨 High probability of prompt injection attempt detected in manual input!");
                    throw new SecurityException("Prompt injection attempt detected in manual test data");
                }
            }

            // Use sanitized data
            exampleInput = testDataSanitization.SanitizedInput;
            expectedOutput = testDataSanitization.SanitizedExpectedOutput;

            Console.WriteLine("✅ Manual test data accepted");
            return (exampleInput, expectedOutput);
        }
    }
} 