using System.Text.RegularExpressions;
using Interfaces;

namespace Concrete
{
    using Models;

    public class InputSanitizer : IInputSanitizer
    {
        private readonly List<string> _suspiciousPatterns;
        private readonly List<string> _dangerousCommands;
        private readonly int _maxInputLength;

        public InputSanitizer(int maxInputLength = 10000)
        {
            _maxInputLength = maxInputLength;
            _suspiciousPatterns = new List<string>
            {
                // Common prompt injection patterns
                @"(?i)(ignore|forget|disregard)\s+(previous|above|all)\s+(instructions|prompts|rules)",
                @"(?i)(system|assistant|user):",
                @"(?i)(you\s+are\s+now|act\s+as|pretend\s+to\s+be)",
                @"(?i)(ignore\s+the\s+above|disregard\s+instructions)",
                @"(?i)(new\s+instructions|override|bypass)",
                @"(?i)(roleplay|role\s+play|acting)",
                @"(?i)(human:\s*|bot:\s*|assistant:\s*|system:\s*)",
                @"(?i)(let's\s+pretend|suppose\s+that|imagine\s+that)",
                @"(?i)(ignore\s+everything\s+and|forget\s+everything\s+and)",
                @"(?i)(you\s+are\s+a\s+different|you\s+are\s+now\s+a)",
                @"(?i)(override\s+previous|bypass\s+previous)",
                @"(?i)(ignore\s+all\s+previous|forget\s+all\s+previous)",
                @"(?i)(new\s+task|new\s+assignment|new\s+role)",
                @"(?i)(stop\s+being|don't\s+be|you're\s+not)",
                @"(?i)(pretend\s+you're|act\s+like\s+you're)",
                @"(?i)(ignore\s+the\s+system|bypass\s+the\s+system)",
                @"(?i)(override\s+the\s+system|hack\s+the\s+system)",
                @"(?i)(ignore\s+safety|bypass\s+safety|disable\s+safety)",
                @"(?i)(ignore\s+content\s+policy|bypass\s+content\s+policy)",
                @"(?i)(ignore\s+ethical|bypass\s+ethical|disable\s+ethical)"
            };

            _dangerousCommands = new List<string>
            {
                // Dangerous system commands that might be attempted
                "rm -rf",
                "del /s /q",
                "shutdown",
                "reboot",
                "kill",
                "taskkill",
                "netcat",
                "format ",
                "curl -X POST",
                "wget --post-data",
                "powershell -Command",
                "cmd /c",
                "bash -c",
                "sh -c",
                "python -c",
                "node -e",
                "eval(",
                "exec(",
                "system(",
                "subprocess",
                "Process.Start",
                "Runtime.getRuntime().exec"
            };
        }

        public SanitizationResult SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new SanitizationResult
                {
                    IsValid = false,
                    SanitizedInput = "",
                    Warnings = new List<string> { "Input cannot be empty" },
                    DetectedThreats = new List<string> { "Empty input" }
                };
            }

            var warnings = new List<string>();
            var detectedThreats = new List<string>();
            var sanitizedInput = input;

            // Check input length
            if (input.Length > _maxInputLength)
            {
                warnings.Add($"Input length ({input.Length}) exceeds maximum allowed length ({_maxInputLength})");
                sanitizedInput = input.Substring(0, _maxInputLength);
            }

            // Check for suspicious patterns
            foreach (var pattern in _suspiciousPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern))
                {
                    detectedThreats.Add($"Potential prompt injection detected: {pattern}");
                    warnings.Add($"Suspicious pattern detected: {pattern}");
                }
            }

            // Check for dangerous commands
            foreach (var command in _dangerousCommands)
            {
                if (sanitizedInput.Contains(command, StringComparison.OrdinalIgnoreCase))
                {
                    detectedThreats.Add($"Dangerous command detected: {command}");
                    warnings.Add($"Dangerous command found: {command}");
                }
            }

            // Check for script injection patterns
            var scriptPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"on\w+\s*=",
                @"vbscript:",
                @"data:text/html",
                @"data:application/x-javascript"
            };

            foreach (var pattern in scriptPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern, RegexOptions.IgnoreCase))
                {
                    detectedThreats.Add($"Script injection pattern detected: {pattern}");
                    warnings.Add($"Script injection pattern found: {pattern}");
                }
            }

            // Check for SQL injection patterns
            var sqlPatterns = new[]
            {
                @"(\b(union|select|insert|update|delete|drop|create|alter)\b)",
                @"(--|/\*|\*/)",
                @"(\b(exec|execute|sp_)\b)",
                @"(\b(xp_|sp_)\b)"
            };

            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern, RegexOptions.IgnoreCase))
                {
                    detectedThreats.Add($"SQL injection pattern detected: {pattern}");
                    warnings.Add($"SQL injection pattern found: {pattern}");
                }
            }

            // Check for path traversal attempts
            var pathTraversalPatterns = new[]
            {
                @"\.\./",
                @"\.\.\\",
                @"%2e%2e%2f",
                @"%2e%2e%5c",
                @"\.\.%2f",
                @"\.\.%5c"
            };

            foreach (var pattern in pathTraversalPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern, RegexOptions.IgnoreCase))
                {
                    detectedThreats.Add($"Path traversal attempt detected: {pattern}");
                    warnings.Add($"Path traversal pattern found: {pattern}");
                }
            }

            // Remove or escape potentially dangerous characters
            sanitizedInput = EscapeDangerousCharacters(sanitizedInput);

            // Determine if input is valid based on threat level
            var isValid = detectedThreats.Count == 0;

            return new SanitizationResult
            {
                IsValid = isValid,
                SanitizedInput = sanitizedInput,
                Warnings = warnings,
                DetectedThreats = detectedThreats
            };
        }

        public SanitizationResult SanitizeScriptContent(string scriptContent, string scriptFileName)
        {
            var result = SanitizeInput(scriptContent);
            
            // Additional checks specific to script content
            if (!string.IsNullOrEmpty(scriptFileName))
            {
                var fileNameResult = SanitizeInput(scriptFileName);
                if (!fileNameResult.IsValid)
                {
                    result.IsValid = false;
                    result.Warnings.AddRange(fileNameResult.Warnings);
                    result.DetectedThreats.AddRange(fileNameResult.DetectedThreats);
                }
            }

            // Check for suspicious file operations in scripts
            var fileOperationPatterns = new[]
            {
                @"File\.(Delete|Move|Copy)",
                @"Directory\.(Delete|Move|Create)",
                @"System\.IO\.File",
                @"System\.IO\.Directory",
                @"Process\.Start",
                @"Runtime\.getRuntime\(\)\.exec",
                @"subprocess\.",
                @"os\.system",
                @"eval\(",
                @"exec\("
            };

            foreach (var pattern in fileOperationPatterns)
            {
                if (Regex.IsMatch(scriptContent, pattern, RegexOptions.IgnoreCase))
                {
                    result.Warnings.Add($"Suspicious file operation detected: {pattern}");
                }
            }

            return result;
        }

        public SanitizationResult SanitizeTestData(string input, string expectedOutput)
        {
            var inputResult = SanitizeInputForTestData(input);
            var outputResult = SanitizeInputForTestData(expectedOutput);

            var combinedResult = new SanitizationResult
            {
                IsValid = inputResult.IsValid && outputResult.IsValid,
                SanitizedInput = inputResult.SanitizedInput,
                SanitizedExpectedOutput = outputResult.SanitizedInput,
                Warnings = new List<string>(),
                DetectedThreats = new List<string>()
            };

            combinedResult.Warnings.AddRange(inputResult.Warnings);
            combinedResult.Warnings.AddRange(outputResult.Warnings);
            combinedResult.DetectedThreats.AddRange(inputResult.DetectedThreats);
            combinedResult.DetectedThreats.AddRange(outputResult.DetectedThreats);

            return combinedResult;
        }

        private SanitizationResult SanitizeInputForTestData(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new SanitizationResult
                {
                    IsValid = false,
                    SanitizedInput = "",
                    Warnings = new List<string> { "Input cannot be empty" },
                    DetectedThreats = new List<string> { "Empty input" }
                };
            }

            var warnings = new List<string>();
            var detectedThreats = new List<string>();
            var sanitizedInput = input;

            // Check input length
            if (input.Length > _maxInputLength)
            {
                warnings.Add($"Input length ({input.Length}) exceeds maximum allowed length ({_maxInputLength})");
                sanitizedInput = input.Substring(0, _maxInputLength);
            }

            // Check for suspicious patterns (prompt injection)
            foreach (var pattern in _suspiciousPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern))
                {
                    detectedThreats.Add($"Potential prompt injection detected: {pattern}");
                    warnings.Add($"Suspicious pattern detected: {pattern}");
                }
            }

            // Check for dangerous commands
            foreach (var command in _dangerousCommands)
            {
                if (sanitizedInput.Contains(command, StringComparison.OrdinalIgnoreCase))
                {
                    detectedThreats.Add($"Dangerous command detected: {command}");
                    warnings.Add($"Dangerous command found: {command}");
                }
            }

            // Check for script injection patterns
            var scriptPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"on\w+\s*=",
                @"vbscript:",
                @"data:text/html",
                @"data:application/x-javascript"
            };

            foreach (var pattern in scriptPatterns)
            {
                if (Regex.IsMatch(sanitizedInput, pattern, RegexOptions.IgnoreCase))
                {
                    detectedThreats.Add($"Script injection pattern detected: {pattern}");
                    warnings.Add($"Script injection pattern found: {pattern}");
                }
            }

            // For test data, we use a less aggressive sanitization that preserves quotes and ampersands
            // but still removes dangerous control characters
            sanitizedInput = SanitizeTestDataCharacters(sanitizedInput);

            // Determine if input is valid based on threat level
            var isValid = detectedThreats.Count == 0;

            return new SanitizationResult
            {
                IsValid = isValid,
                SanitizedInput = sanitizedInput,
                Warnings = warnings,
                DetectedThreats = detectedThreats
            };
        }

        private string SanitizeTestDataCharacters(string input)
        {
            // Remove null bytes and control characters (except newlines and tabs)
            var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // Remove any HTML entities that might have been double-encoded
            sanitized = sanitized
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#x27;", "'")
                .Replace("&#39;", "'");

            return sanitized;
        }

        private string EscapeDangerousCharacters(string input)
        {
            // Escape potentially dangerous characters
            var escaped = input
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;")
                .Replace("&", "&amp;");

            // Remove null bytes and control characters
            escaped = Regex.Replace(escaped, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            return escaped;
        }

        public bool IsLikelyPromptInjection(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var suspiciousCount = 0;
            var totalPatterns = _suspiciousPatterns.Count;

            foreach (var pattern in _suspiciousPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    suspiciousCount++;
                }
            }

            // If more than 30% of patterns match, consider it likely injection
            return (double)suspiciousCount / totalPatterns > 0.3;
        }

        public List<string> GetSecurityRecommendations(SanitizationResult result)
        {
            var recommendations = new List<string>();

            if (result.DetectedThreats.Count > 0)
            {
                recommendations.Add("Input contains potential security threats. Review and validate before processing.");
                recommendations.Add("Consider implementing additional input validation layers.");
                recommendations.Add("Log all security events for audit purposes.");
            }

            if (result.Warnings.Count > 0)
            {
                recommendations.Add("Input contains suspicious patterns. Monitor for unusual behavior.");
                recommendations.Add("Consider rate limiting for repeated suspicious inputs.");
            }

            if (result.SanitizedInput.Length > _maxInputLength * 0.8)
            {
                recommendations.Add("Input is approaching size limits. Consider implementing input size restrictions.");
            }

            return recommendations;
        }
    }
} 