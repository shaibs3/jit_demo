using Interfaces;

namespace Concrete
{
    public class Validator : IValidator
    {
        private readonly ILlm _llmClient;

        public Validator(ILlm llmClient)
        {
            _llmClient = llmClient;
        }

        public async Task RunTestAndValidateAsync(
            IOci ociClient, 
            string imageName, 
            string exampleInput, 
            string expectedOutput, 
            string scriptContent, 
            string scriptFileName)
        {
            int maxTestRetries = 3;
            int currentTestRetry = 0;

            while (currentTestRetry < maxTestRetries)
            {
                try
                {
                    string actualOutput = await RunDockerContainerAsync(ociClient, imageName, exampleInput);

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
                        var (newExampleInput, newExpectedOutput) = await _llmClient.GenerateTestDataFromScriptAnalysis(
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
                    var (newExampleInput, newExpectedOutput) = await _llmClient.GenerateFallbackTestData(scriptContent, scriptFileName);
                    exampleInput = newExampleInput;
                    expectedOutput = newExpectedOutput;
                }
            }
        }

        private async Task<string> RunDockerContainerAsync(IOci ociClient, string imageName, string exampleInput)
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

        public bool ValidateTestOutput(string actualOutput, string expectedOutput)
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