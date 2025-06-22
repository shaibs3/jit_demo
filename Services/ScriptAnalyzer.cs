using DockerWrapperTool.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DockerWrapperTool.Services;

public class ScriptAnalyzer
{
    private readonly ILogger _logger;

    public ScriptAnalyzer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ScriptAnalysis> AnalyzeAsync(string scriptPath)
    {
        var content = await File.ReadAllTextAsync(scriptPath);
        var fileName = Path.GetFileName(scriptPath);
        var extension = Path.GetExtension(scriptPath).ToLowerInvariant();

        var language = DetectLanguage(extension, content);
        var dependencies = DetectDependencies(language, content);
        var shebang = ExtractShebang(content);
        var metadata = ExtractMetadata(content, language);

        return new ScriptAnalysis(
            Name: Path.GetFileNameWithoutExtension(scriptPath),
            Language: language,
            Content: content,
            Dependencies: dependencies,
            Shebang: shebang,
            Metadata: metadata
        );
    }

    private string DetectLanguage(string extension, string content)
    {
        return extension switch
        {
            ".sh" or ".bash" => "bash",
            ".py" or ".python" => "python",
            ".js" or ".javascript" => "javascript",
            ".rb" or ".ruby" => "ruby",
            ".pl" or ".perl" => "perl",
            ".php" => "php",
            ".go" => "go",
            ".rs" => "rust",
            ".java" => "java",
            ".cs" => "csharp",
            _ => DetectLanguageFromShebang(content) ?? "unknown"
        };
    }

    private string? DetectLanguageFromShebang(string content)
    {
        var firstLine = content.Split('\n').FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(firstLine) || !firstLine.StartsWith("#!"))
            return null;

        var shebang = firstLine.ToLowerInvariant();
        return shebang switch
        {
            var s when s.Contains("python") => "python",
            var s when s.Contains("node") => "javascript",
            var s when s.Contains("bash") => "bash",
            var s when s.Contains("sh") => "bash",
            var s when s.Contains("ruby") => "ruby",
            var s when s.Contains("perl") => "perl",
            var s when s.Contains("php") => "php",
            _ => "unknown"
        };
    }

    private List<string> DetectDependencies(string language, string content)
    {
        var dependencies = new List<string>();

        switch (language.ToLowerInvariant())
        {
            case "python":
                dependencies.AddRange(DetectPythonDependencies(content));
                break;
            case "javascript":
                dependencies.AddRange(DetectJavaScriptDependencies(content));
                break;
            case "bash":
                dependencies.AddRange(DetectBashDependencies(content));
                break;
        }

        return dependencies.Distinct().ToList();
    }

    private List<string> DetectPythonDependencies(string content)
    {
        var dependencies = new List<string>();
        var importPattern = @"^(?:from\s+(\w+)|import\s+(\w+))";
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("import ") || trimmedLine.StartsWith("from "))
            {
                var match = Regex.Match(trimmedLine, importPattern);
                if (match.Success)
                {
                    var module = match.Groups[1].Value ?? match.Groups[2].Value;
                    if (!string.IsNullOrEmpty(module) && !module.StartsWith("."))
                    {
                        dependencies.Add(module);
                    }
                }
            }
        }

        return dependencies;
    }

    private List<string> DetectJavaScriptDependencies(string content)
    {
        var dependencies = new List<string>();
        var requirePattern = @"require\(['""]([^'""]+)['""]\)";
        var importPattern = @"import\s+(?:\{[^}]*\}|\w+)\s+from\s+['""]([^'""]+)['""]";

        var requireMatches = Regex.Matches(content, requirePattern);
        var importMatches = Regex.Matches(content, importPattern);

        foreach (Match match in requireMatches)
        {
            dependencies.Add(match.Groups[1].Value);
        }

        foreach (Match match in importMatches)
        {
            dependencies.Add(match.Groups[1].Value);
        }

        return dependencies;
    }

    private List<string> DetectBashDependencies(string content)
    {
        var dependencies = new List<string>();
        var commandPattern = @"\b(awk|sed|grep|wc|sort|uniq|head|tail|cat|echo|printf|curl|wget|git|docker|kubectl)\b";

        var matches = Regex.Matches(content, commandPattern);
        foreach (Match match in matches)
        {
            dependencies.Add(match.Value);
        }

        return dependencies;
    }

    private string? ExtractShebang(string content)
    {
        var firstLine = content.Split('\n').FirstOrDefault()?.Trim();
        return firstLine?.StartsWith("#!") == true ? firstLine : null;
    }

    private Dictionary<string, string> ExtractMetadata(string content, string language)
    {
        var metadata = new Dictionary<string, string>();

        // Extract version info if present
        var versionPattern = @"version\s*[=:]\s*['""]?([^'""\s]+)['""]?";
        var versionMatch = Regex.Match(content, versionPattern, RegexOptions.IgnoreCase);
        if (versionMatch.Success)
        {
            metadata["version"] = versionMatch.Groups[1].Value;
        }

        // Extract author info if present
        var authorPattern = @"author\s*[=:]\s*['""]?([^'""\n]+)['""]?";
        var authorMatch = Regex.Match(content, authorPattern, RegexOptions.IgnoreCase);
        if (authorMatch.Success)
        {
            metadata["author"] = authorMatch.Groups[1].Value;
        }

        return metadata;
    }
} 