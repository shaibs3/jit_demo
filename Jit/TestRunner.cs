using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Program
{
    public class TestRunner
    {
        public async Task<bool> RunTest(string imageName, string input, string expectedOutput)
        {
            // Run the docker image with the test input
            string dockerCommand = $"docker run {imageName} '{input}'";
            Console.WriteLine("Running test command:");
            Console.WriteLine(dockerCommand);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{dockerCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string actualOutput = await process.StandardOutput.ReadToEndAsync();
            string errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Compare outputs
            if (actualOutput.Trim() == expectedOutput)
            {
                Console.WriteLine("✅ Test passed! Output matches expected output.");
                return true;
            }
            else
            {
                Console.WriteLine("❌ Test failed.");
                Console.WriteLine($"Expected:\n{expectedOutput}");
                Console.WriteLine($"Actual:\n{actualOutput}");
                if (!string.IsNullOrWhiteSpace(errorOutput))
                    Console.WriteLine($"Error Output:\n{errorOutput}");
                return false;
            }
        }
    }
}