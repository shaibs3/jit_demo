namespace DockerWrapperTool.Models;

public record ScriptAnalysis(
    string Name,
    string Language,
    string Content,
    List<string> Dependencies,
    string? Shebang,
    Dictionary<string, string> Metadata
);

public record ProcessResult(
    bool Success,
    string? DockerfilePath = null,
    string? ImageName = null,
    TestResult? TestResult = null,
    string? Error = null
);

public record TestResult(
    int Total,
    int Passed,
    List<TestCase> TestCases
);

public record TestCase(
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    bool Passed
);

public record DockerBuildResult(
    bool Success,
    string? ImageName = null,
    string? Error = null
); 