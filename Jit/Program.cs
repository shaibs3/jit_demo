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
            var (llmClient, _, dockerService, extractor, validator, sanitizer) = InitializeServices();

            // Extract test data from README
            var (exampleInput, expectedOutput) = await extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);

            // Build Docker image with retry logic
            string imageName = await dockerService.BuildDockerImageWithRetry(llmClient, scriptContent, scriptFileName, scriptPath);

            // Run test and validate output
            await validator.RunTestAndValidateAsync(dockerService, imageName, exampleInput, expectedOutput, scriptContent, scriptFileName);
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

        static (OpenAiClient openAiClient, ReadmeExtractor readmeExtractor, DockerService dockerService, Extractor extractor, Validator validator, InputSanitizer sanitizer) InitializeServices()
        {
            OpenAiClient openAiClient = new OpenAiClient();
            ReadmeExtractor readmeExtractor = new ReadmeExtractor(openAiClient);
            DockerService dockerService = new DockerService();
            InputSanitizer sanitizer = new InputSanitizer();
            Extractor extractor = new Extractor(readmeExtractor, openAiClient, sanitizer);
            Validator validator = new Validator(openAiClient);

            return (openAiClient, readmeExtractor, dockerService, extractor, validator, sanitizer);
        }
    }
}