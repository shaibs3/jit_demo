using DockerWrapperTool.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DockerWrapperTool.Services;

public class ContainerTester
{
    private readonly ILogger _logger;

    public ContainerTester(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<DockerBuildResult> BuildContainerAsync(string dockerfilePath, string scriptPath)
    {
        try
        {
            var imageName = $"script-{Path.GetFileNameWithoutExtension(scriptPath)}-{DateTime.Now:yyyyMMdd-HHmmss}";
            var scriptDir = Path.GetDirectoryName(scriptPath) ?? ".";
            var scriptName = Path.GetFileName(scriptPath);

            _logger.LogInformation("Building Docker image: {ImageName}", imageName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"build -f {dockerfilePath} -t {imageName} {scriptDir}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Docker build completed successfully");
                return new DockerBuildResult(true, imageName);
            }
            else
            {
                _logger.LogError("Docker build failed: {Error}", error);
                return new DockerBuildResult(false, Error: error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building Docker container");
            return new DockerBuildResult(false, Error: ex.Message);
        }
    }

    public async Task<TestResult> TestContainerAsync(string imageName, ScriptAnalysis scriptAnalysis, string readmeContent)
    {
        try
        {
            var testCases = ExtractTestCases(readmeContent, scriptAnalysis.Language);
            var passedTests = 0;
            var testResults = new List<TestCase>();

            _logger.LogInformation("Running {Count} test cases", testCases.Count);

            foreach (var testCase in testCases)
            {
                var result = await RunTestCaseAsync(imageName, testCase);
                testResults.Add(result);
                
                if (result.Passed)
                {
                    passedTests++;
                    _logger.LogInformation("✅ Test passed: {Input} → {Output}", 
                        testCase.Input, result.ActualOutput);
                }
                else
                {
                    _logger.LogWarning("❌ Test failed: {Input} → Expected: {Expected}, Got: {Actual}", 
                        testCase.Input, testCase.ExpectedOutput, result.ActualOutput);
                }
            }

            return new TestResult(testCases.Count, passedTests, testResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing container");
            return new TestResult(0, 0, new List<TestCase>());
        }
    }

    private async Task<TestCase> RunTestCaseAsync(string imageName, TestCase testCase)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"run --rm {imageName} {testCase.Input}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var actualOutput = output.Trim();
            var passed = actualOutput.Equals(testCase.ExpectedOutput, StringComparison.OrdinalIgnoreCase);

            return new TestCase(
                testCase.Input,
                testCase.ExpectedOutput,
                actualOutput,
                passed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running test case: {Input}", testCase.Input);
            return new TestCase(
                testCase.Input,
                testCase.ExpectedOutput,
                $"Error: {ex.Message}",
                false
            );
        }
    }

    private List<TestCase> ExtractTestCases(string readmeContent, string language)
    {
        var testCases = new List<TestCase>();

        // Look for common patterns in README files
        var patterns = new[]
        {
            // Pattern: "Example: command input → expected output"
            @"(?:Example|Usage|Test):\s*([^\n]+)\s*→\s*([^\n]+)",
            // Pattern: "Input: xyz, Output: abc"
            @"Input:\s*([^\n]+)[,\s]+Output:\s*([^\n]+)",
            // Pattern: command line examples
            @".*`([^`]+)`.*→.*`([^`]+)`",
            // Pattern: Usage examples
            @"Usage:.*`([^`]+)`.*\n.*([^\n]+)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(readmeContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var input = match.Groups[1].Value.Trim();
                var expectedOutput = match.Groups[2].Value.Trim();

                // Clean up the input/output
                input = CleanInput(input);
                expectedOutput = CleanOutput(expectedOutput);

                if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(expectedOutput))
                {
                    testCases.Add(new TestCase(input, expectedOutput, "", false));
                }
            }
        }

        // If no patterns found, try to extract from usage examples
        if (!testCases.Any())
        {
            testCases.AddRange(ExtractFromUsageExamples(readmeContent, language));
        }

        return testCases;
    }

    private string CleanInput(string input)
    {
        // Remove quotes and extra whitespace
        input = input.Trim('"', '\'', '`');
        input = input.Trim();
        
        // Handle escaped characters
        input = input.Replace("\\n", "\n");
        input = input.Replace("\\t", "\t");
        
        return input;
    }

    private string CleanOutput(string output)
    {
        // Remove quotes and extra whitespace
        output = output.Trim('"', '\'', '`');
        output = output.Trim();
        
        // Handle escaped characters
        output = output.Replace("\\n", "\n");
        output = output.Replace("\\t", "\t");
        
        return output;
    }

    private List<TestCase> ExtractFromUsageExamples(string readmeContent, string language)
    {
        var testCases = new List<TestCase>();

        // Language-specific default test cases
        switch (language.ToLowerInvariant())
        {
            case "bash":
                testCases.Add(new TestCase("hello\nworld", "Line Count: 2", "", false));
                break;
            case "javascript":
                testCases.Add(new TestCase("hello world", "Vowel Count: 3", "", false));
                break;
            case "python":
                testCases.Add(new TestCase("hello world", "world hello", "", false));
                break;
        }

        return testCases;
    }
} 