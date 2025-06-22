using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Concrete
{
    public class OpenAiClient : ILlm
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly Dictionary<string, (string exampleInput, string expectedOutput)> _testDataCache;

        public OpenAiClient(string? model = null)
        {
            DotNetEnv.Env.Load();
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            _model = model ?? "gpt-3.5-turbo"; // Default to gpt-3.5-turbo if not specified
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _testDataCache = new Dictionary<string, (string exampleInput, string expectedOutput)>();
        }

        public async Task<string> CreateDockerFile(string scriptContent, string scriptFileName)
        {
            var prompt = $@"
You are an expert DevOps engineer.
Given the following script file named `{scriptFileName}`, generate a complete Dockerfile that will run this script.
Ensure the Dockerfile installs all necessary dependencies for the script to work, sets the correct ENTRYPOINT (not CMD), and is as minimal as possible.

IMPORTANT: Use ENTRYPOINT instead of CMD to ensure the script is the main executable.

Here is the script content:

{scriptContent}

Please output only the Dockerfile content without any markdown formatting or code blocks.
Use ENTRYPOINT to run the script.
";

            return await SendPromptToOpenAI(prompt);
        }

        public async Task<string> FixDockerFile(string scriptContent, string scriptFileName, string originalDockerfile, string buildError)
        {
            var prompt = $@"
You are an expert DevOps engineer. The Dockerfile you generated has a build error and needs to be fixed.

Script file: {scriptFileName}
Script content:
{scriptContent}

Original Dockerfile (with error):
{originalDockerfile}

Docker build error:
{buildError}

Please fix the Dockerfile and output only the corrected Dockerfile content without any markdown formatting or code blocks.
IMPORTANT: Use ENTRYPOINT instead of CMD to ensure the script is the main executable.
Focus on the specific error mentioned in the build output.
";

            return await SendPromptToOpenAI(prompt);
        }

        public async Task<(string exampleInput, string expectedOutput)> ExtractExampleFromReadme(string readmePath)
        {
            // Check cache first
            if (_testDataCache.ContainsKey(readmePath))
            {
                Console.WriteLine("📋 Using cached test data from README");
                return _testDataCache[readmePath];
            }

            var readmeContent = await File.ReadAllTextAsync(readmePath);
            
            var prompt = $@"
You are an expert at analyzing README files and extracting test examples.

Please analyze this README content and extract:
1. An example input that can be used to test the script
2. The expected output for that input
3. A confidence score (0-100) indicating how certain you are about the extraction

README content:
{readmeContent}

Please respond with only a JSON object in this exact format:
{{
  ""exampleInput"": ""the input string"",
  ""expectedOutput"": ""the expected output string"",
  ""confidence"": 85,
  ""reasoning"": ""brief explanation of why this input/output was chosen""
}}

Do not include any explanations or markdown formatting outside the JSON.
";

            var response = await SendPromptToOpenAI(prompt);
            
            // Parse the JSON response
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string? exampleInput = root.GetProperty("exampleInput").GetString();
            string? expectedOutput = root.GetProperty("expectedOutput").GetString();
            int confidence = root.GetProperty("confidence").GetInt32();
            string? reasoning = root.GetProperty("reasoning").GetString();
            
            if (string.IsNullOrEmpty(exampleInput) || string.IsNullOrEmpty(expectedOutput))
                throw new InvalidOperationException("Failed to extract valid example input and output from README");
            
            // Log the extraction details for debugging
            Console.WriteLine($"📋 Extracted test data:");
            Console.WriteLine($"   Input: '{exampleInput}'");
            Console.WriteLine($"   Expected Output: '{expectedOutput}'");
            Console.WriteLine($"   Confidence: {confidence}%");
            Console.WriteLine($"   Reasoning: {reasoning}");
            
            // Warn if confidence is low
            if (confidence < 70)
            {
                Console.WriteLine($"⚠️  Warning: Low confidence ({confidence}%) in extracted test data. Manual verification recommended.");
            }
            
            // Cache the result
            var result = (exampleInput, expectedOutput);
            _testDataCache[readmePath] = result;
                
            return result;
        }

        public async Task<bool> ValidateExtractedExample(string scriptContent, string scriptFileName, string exampleInput, string expectedOutput)
        {
            var prompt = $@"
You are an expert at validating test examples for scripts.

Given this script content:
{scriptContent}

And these extracted test values:
- Input: ""{exampleInput}""
- Expected Output: ""{expectedOutput}""

Please analyze if this input/output pair makes sense for testing this script.
Consider:
1. Does the input format match what the script expects?
2. Is the expected output reasonable given the input and script logic?
3. Are there any obvious issues with the test case?

Respond with only a JSON object:
{{
  ""isValid"": true/false,
  ""confidence"": 0-100,
  ""issues"": [""list of any issues found""],
  ""suggestions"": [""list of suggestions for improvement""]
}}
";

            var response = await SendPromptToOpenAI(prompt);
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            bool isValid = root.GetProperty("isValid").GetBoolean();
            int confidence = root.GetProperty("confidence").GetInt32();
            
            var issues = root.GetProperty("issues").EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
                
            var suggestions = root.GetProperty("suggestions").EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
            
            Console.WriteLine($"🔍 Validation results:");
            Console.WriteLine($"   Valid: {isValid}");
            Console.WriteLine($"   Confidence: {confidence}%");
            
            if (issues.Length > 0)
            {
                Console.WriteLine($"   Issues: {string.Join(", ", issues)}");
            }
            
            if (suggestions.Length > 0)
            {
                Console.WriteLine($"   Suggestions: {string.Join(", ", suggestions)}");
            }
            
            return isValid && confidence >= 70;
        }

        public async Task<(string exampleInput, string expectedOutput)> GenerateFallbackTestData(string scriptContent, string scriptFileName)
        {
            // Create a cache key based on script content hash
            string cacheKey = $"fallback_{GetHash(scriptContent)}";
            
            // Check cache first
            if (_testDataCache.ContainsKey(cacheKey))
            {
                Console.WriteLine("📋 Using cached fallback test data");
                return _testDataCache[cacheKey];
            }

            var prompt = $@"
You are an expert at creating test cases for scripts.

Given this script content:
{scriptContent}

Please generate a simple, reliable test case that can be used to verify the script works correctly.
The test should be:
1. Simple and easy to understand
2. Representative of typical usage
3. Have a predictable, verifiable output

Respond with only a JSON object:
{{
  ""exampleInput"": ""the input string"",
  ""expectedOutput"": ""the expected output string"",
  ""explanation"": ""brief explanation of why this test case was chosen""
}}
";

            var response = await SendPromptToOpenAI(prompt);
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string? exampleInput = root.GetProperty("exampleInput").GetString();
            string? expectedOutput = root.GetProperty("expectedOutput").GetString();
            string? explanation = root.GetProperty("explanation").GetString();
            
            if (string.IsNullOrEmpty(exampleInput) || string.IsNullOrEmpty(expectedOutput))
                throw new InvalidOperationException("Failed to generate fallback test data");
            
            Console.WriteLine($"🔄 Generated fallback test data:");
            Console.WriteLine($"   Input: '{exampleInput}'");
            Console.WriteLine($"   Expected Output: '{expectedOutput}'");
            Console.WriteLine($"   Explanation: {explanation}");
            
            // Cache the result
            var result = (exampleInput, expectedOutput);
            _testDataCache[cacheKey] = result;
            
            return result;
        }

        private string GetHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<string> GetTestCommandAndExpectedOutput(string prompt)
        {
            return await SendPromptToOpenAI(prompt);
        }

        public async Task<string> SendPromptToOpenAI(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            
            // Parse the JSON and extract the message content
            using var doc = JsonDocument.Parse(responseString);
            string? result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("No content received from OpenAI");

            return result;
        }

        private string CleanDockerfileContent(string content)
        {
            // Remove markdown code blocks
            content = Regex.Replace(content, @"^```(?:dockerfile|Dockerfile)?\s*", "", RegexOptions.Multiline);
            content = Regex.Replace(content, @"\s*```$", "", RegexOptions.Multiline);
            
            // Remove any leading/trailing whitespace
            content = content.Trim();
            
            // Remove any explanatory text that might be before or after the Dockerfile
            var lines = content.Split('\n');
            var dockerfileLines = new List<string>();
            bool inDockerfile = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Start collecting Dockerfile lines when we see a valid Dockerfile instruction
                if (!inDockerfile && IsDockerfileInstruction(trimmedLine))
                {
                    inDockerfile = true;
                }
                
                if (inDockerfile)
                {
                    // Stop if we hit another markdown block or explanatory text
                    if (trimmedLine.StartsWith("```") || 
                        trimmedLine.StartsWith("Here's") || 
                        trimmedLine.StartsWith("The Dockerfile") ||
                        trimmedLine.StartsWith("This Dockerfile"))
                    {
                        break;
                    }
                    
                    dockerfileLines.Add(line);
                }
            }
            
            return string.Join("\n", dockerfileLines).Trim();
        }

        private bool IsDockerfileInstruction(string line)
        {
            var instructions = new[] { "FROM", "RUN", "COPY", "ADD", "CMD", "ENTRYPOINT", "EXPOSE", "ENV", "ARG", "WORKDIR", "USER", "VOLUME", "LABEL" };
            return instructions.Any(instruction => line.StartsWith(instruction, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<(string exampleInput, string expectedOutput)> GenerateTestDataFromScriptAnalysis(
            string scriptContent, string scriptFileName, string actualOutput, string previousInput, string previousExpectedOutput)
        {
            var prompt = $@"
You are an expert at analyzing script behavior and generating accurate test cases.

Given this script:
{scriptContent}

The previous test failed:
- Input: ""{previousInput}""
- Expected Output: ""{previousExpectedOutput}""
- Actual Output: ""{actualOutput}""

Please analyze the script and the actual output to understand what the script really does, then generate a new, accurate test case.

Consider:
1. What does the script actually do based on the actual output?
2. What input would produce a predictable, verifiable output?
3. How can we create a simple test that will definitely work?

Respond with only a JSON object:
{{
  ""exampleInput"": ""the new input string"",
  ""expectedOutput"": ""the new expected output string"",
  ""analysis"": ""brief explanation of what the script actually does and why this test case should work""
}}
";

            var response = await SendPromptToOpenAI(prompt);
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string? exampleInput = root.GetProperty("exampleInput").GetString();
            string? expectedOutput = root.GetProperty("expectedOutput").GetString();
            string? analysis = root.GetProperty("analysis").GetString();
            
            if (string.IsNullOrEmpty(exampleInput) || string.IsNullOrEmpty(expectedOutput))
                throw new InvalidOperationException("Failed to generate new test data from script analysis");
            
            Console.WriteLine($"📊 Script analysis: {analysis}");
            
            return (exampleInput, expectedOutput);
        }
    }
}