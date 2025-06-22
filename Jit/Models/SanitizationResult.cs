namespace Models
{
    public class SanitizationResult
    {
        public bool IsValid { get; set; }
        public string SanitizedInput { get; set; } = string.Empty;
        public string SanitizedExpectedOutput { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> DetectedThreats { get; set; } = new List<string>();
    }
} 