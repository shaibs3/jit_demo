using Interfaces;
using Concrete;

namespace Program
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await RunDockerizationProcess(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            }
        }

        static async Task RunDockerizationProcess(string[] args)
        {
            // Validate and parse arguments
            var (scriptPath, readmePath) = ValidateAndParseArguments(args);

            // Validate files exist
            ValidateFilesExist(scriptPath, readmePath);

            // Read script content
            var (scriptContent, scriptFileName) = await ReadScriptFile(scriptPath);

            // Initialize services
            var (llmClient, readmeExtractor, dockerService) = InitializeServices();

            // Extract test data from README
            var (exampleInput, expectedOutput) = await ExtractTestData(readmeExtractor, readmePath, llmClient, scriptContent, scriptFileName);

            // Build Docker image with retry logic
            string imageName = await dockerService.BuildDockerImageWithRetry(llmClient, scriptContent, scriptFileName, scriptPath);

            // Run test and validate output
            await RunTestAndValidate(dockerService, imageName, exampleInput, expectedOutput, llmClient, scriptContent, scriptFileName);
        }

        static (string scriptPath, string readmePath) ValidateAndParseArguments(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet run <scriptPath> <readmePath>");
                throw new ArgumentException("Invalid number of arguments");
            }

            return (args[0], args[1]);
        }

        static void ValidateFilesExist(string scriptPath, string readmePath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Script file not found: {scriptPath}");
            }

            if (!File.Exists(readmePath))
            {
                throw new FileNotFoundException($"README file not found: {readmePath}");
            }
        }

        static async Task<(string scriptContent, string scriptFileName)> ReadScriptFile(string scriptPath)
        {
            string scriptContent = await File.ReadAllTextAsync(scriptPath);
            string scriptFileName = Path.GetFileName(scriptPath);
            return (scriptContent, scriptFileName);
        }

        static (OpenAiClient openAiClient, ReadmeExtractor readmeExtractor, DockerService dockerService) InitializeServices()
        {
            var openAiClient = new OpenAiClient();
            var readmeExtractor = new ReadmeExtractor(openAiClient);
            var dockerService = new DockerService();

            return (openAiClient, readmeExtractor, dockerService);
        }

        static async Task<(string exampleInput, string expectedOutput)> ExtractTestData(IReadmeExtractor readmeExtractor, string readmePath, ILlm llmClient, string scriptContent, string scriptFileName)
        {
            try
            {
                // First attempt: extract from README
                var (exampleInput, expectedOutput) = await readmeExtractor.ExtractExampleAsync(readmePath);

                Console.WriteLine("Example input extracted from README: " + exampleInput);
                Console.WriteLine("Expected output extracted from README: " + expectedOutput);

                // Validate the extracted data
                bool isValid = await llmClient.ValidateExtractedExample(scriptContent, scriptFileName, exampleInput, expectedOutput);

                if (isValid)
                {
                    Console.WriteLine("✅ README extraction validated successfully");
                    return (exampleInput, expectedOutput);
                }
                else
                {
                    Console.WriteLine("❌ README extraction validation failed, trying fallback generation...");
                    return await GenerateFallbackTestData(llmClient, scriptContent, scriptFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ README extraction failed: {ex.Message}");
                Console.WriteLine("🔄 Attempting fallback test data generation...");
                return await GenerateFallbackTestData(llmClient, scriptContent, scriptFileName);
            }
        }

        static async Task<(string exampleInput, string expectedOutput)> GenerateFallbackTestData(ILlm llmClient, string scriptContent, string scriptFileName)
        {
            try
            {
                var (exampleInput, expectedOutput) = await llmClient.GenerateFallbackTestData(scriptContent, scriptFileName);

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

        static (string exampleInput, string expectedOutput) GetManualTestData()
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

        static async Task RunTestAndValidate(IOci ociClient, string imageName, string exampleInput, string expectedOutput, ILlm llmClient, string scriptContent, string scriptFileName)
        {
            int maxTestRetries = 3;
            int currentTestRetry = 0;

            while (currentTestRetry < maxTestRetries)
            {
                try
                {
                    string actualOutput = await RunDockerContainer(ociClient, imageName, exampleInput);

                    if (ValidateTestOutput(actualOutput, expectedOutput))
                    {
                        Console.WriteLine("✅ Test passed! Output matches expected output.");
                        return; // Success, exit the retry loop
                    }
                    else
                    {
                        currentTestRetry++;
                        Console.WriteLine($"❌ Test failed (attempt {currentTestRetry}/{maxTestRetries})");

                        if (currentTestRetry >= maxTestRetries)
                        {
                            Console.WriteLine("❌ All test attempts failed. Final validation:");
                            ValidateTestOutput(actualOutput, expectedOutput);
                            return;
                        }

                        // Generate new test data based on the script and actual output
                        Console.WriteLine("🔄 Generating new test data based on script analysis...");
                        var (newExampleInput, newExpectedOutput) = await llmClient.GenerateTestDataFromScriptAnalysis(
                            scriptContent, scriptFileName, actualOutput, exampleInput, expectedOutput);

                        // Update test data for next iteration
                        exampleInput = newExampleInput;
                        expectedOutput = newExpectedOutput;

                        Console.WriteLine($"🔄 Retrying with new test data:");
                        Console.WriteLine($"   New Input: '{exampleInput}'");
                        Console.WriteLine($"   New Expected Output: '{expectedOutput}'");
                    }
                }
                catch (Exception ex)
                {
                    currentTestRetry++;
                    Console.WriteLine($"❌ Test execution failed (attempt {currentTestRetry}/{maxTestRetries}): {ex.Message}");

                    if (currentTestRetry >= maxTestRetries)
                    {
                        throw new InvalidOperationException($"Failed to run and validate test after {maxTestRetries} attempts", ex);
                    }

                    // Try to generate new test data even if execution failed
                    Console.WriteLine("🔄 Generating new test data due to execution failure...");
                    var (newExampleInput, newExpectedOutput) = await llmClient.GenerateFallbackTestData(scriptContent, scriptFileName);
                    exampleInput = newExampleInput;
                    expectedOutput = newExpectedOutput;
                }
            }
        }

        static async Task<string> RunDockerContainer(IOci ociClient, string imageName, string exampleInput)
        {
            try
            {
                Console.WriteLine("running Docker image... " + imageName + " input:  " + exampleInput);
                string actualOutput = await ociClient.RunImage(imageName, exampleInput);
                Console.WriteLine("Actual output from container:");
                Console.WriteLine(actualOutput);
                return actualOutput;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run Docker image: {ex.Message}", ex);
            }
        }

        static bool ValidateTestOutput(string actualOutput, string expectedOutput)
        {
            if (actualOutput.Trim() == expectedOutput.Trim())
            {
                return true;
            }
            else
            {
                Console.WriteLine($"Expected:\n{expectedOutput}");
                Console.WriteLine($"Actual:\n{actualOutput}");
                return false;
            }
        }
    }
}