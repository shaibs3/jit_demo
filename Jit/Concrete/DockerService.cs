using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Interfaces;

namespace Concrete
{
    using System.Text;
    public class DockerService : IOci
    {
        public async Task<string> BuildImage(string dockerfileContent, string scriptPath, string scriptFileName)
        {
            try
            {
                // Create a temporary directory for the build context
                var tempDir = Path.Combine(Path.GetTempPath(), $"docker-build-{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                // Write the Dockerfile to the temp directory
                var dockerfilePath = Path.Combine(tempDir, "Dockerfile");
                await File.WriteAllTextAsync(dockerfilePath, dockerfileContent);

                // Copy the script to the temp directory
                var scriptDestPath = Path.Combine(tempDir, scriptFileName);
                File.Copy(scriptPath, scriptDestPath);

                // Generate image name
                var imageName = $"script-{Path.GetFileNameWithoutExtension(scriptFileName)}-{DateTime.Now:yyyyMMdd-HHmmss}";

                Console.WriteLine($"Building Docker image: {imageName}");

                // Build the Docker image
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"build -t {imageName} {tempDir}",
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

                // Clean up temp directory
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not clean up temp directory: {ex.Message}");
                }

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"✅ Docker image built successfully: {imageName}");
                    Console.WriteLine("Build output:");
                    Console.WriteLine(output);
                    return imageName;
                }
                else
                {
                    Console.WriteLine($"❌ Docker build failed:");
                    Console.WriteLine("Error output:");
                    Console.WriteLine(error);
                    throw new InvalidOperationException($"Docker build failed with exit code {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error building Docker image: {ex.Message}");
                throw;
            }
        }
        
        public async Task<string> RunImage(string imageName, string input)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --rm -i {imageName} \"{input}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            process.Start();

            // Write input to stdin
            if (!string.IsNullOrEmpty(input))
            {
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync();
            }
            process.StandardInput.Close();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Docker run failed: {errorBuilder}");
            }

            return outputBuilder.ToString().Trim();
        }
    }
}