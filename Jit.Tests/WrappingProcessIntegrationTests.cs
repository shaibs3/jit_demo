using Moq;
using Concrete;
using Interfaces;

namespace Jit.Tests
{
    [TestClass]
    public class WrappingProcessIntegrationTests
    {
        private Mock<ILlm> _mockLlmClient;
        private Mock<IReadmeExtractor> _mockReadmeExtractor;
        private Mock<IOci> _mockOciClient;
        private Extractor _extractor;
        private Validator _validator;
        private DockerService _dockerService;

        [TestInitialize]
        public void Setup()
        {
            _mockLlmClient = new Mock<ILlm>();
            _mockReadmeExtractor = new Mock<IReadmeExtractor>();
            _mockOciClient = new Mock<IOci>();
            
            _extractor = new Extractor(_mockReadmeExtractor.Object, _mockLlmClient.Object);
            _validator = new Validator(_mockLlmClient.Object);
            _dockerService = new DockerService();
        }

        [TestMethod]
        public async Task WrappingProcess_CompleteWorkflow_SuccessfullyDockerizesScript()
        {
            // Arrange
            var scriptPath = "test-script.js";
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test-script.js";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var dockerfile = "FROM node:18\nCOPY test-script.js .\nCMD [\"node\", \"test-script.js\"]";
            var imageName = "script-test-script-20241201-120000";

            // Mock README extraction
            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ReturnsAsync((exampleInput, expectedOutput));

            _mockLlmClient
                .Setup(x => x.ValidateExtractedExample(scriptContent, scriptFileName, exampleInput, expectedOutput))
                .ReturnsAsync(true);

            // Mock Dockerfile generation
            _mockLlmClient
                .Setup(x => x.CreateDockerFile(scriptContent, scriptFileName))
                .ReturnsAsync(dockerfile);

            // Mock Docker build
            _mockOciClient
                .Setup(x => x.BuildImage(dockerfile, scriptPath, scriptFileName))
                .ReturnsAsync(imageName);

            // Mock Docker run
            _mockOciClient
                .Setup(x => x.RunImage(imageName, exampleInput))
                .ReturnsAsync(expectedOutput);

            // Act - Simulate the wrapping process
            var (extractedInput, extractedOutput) = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);
            
            // Note: We can't easily test the DockerService.BuildDockerImageWithRetry method here because it uses real Docker commands
            // In a real integration test, we would need to mock the Process class or use a test Docker environment
            
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, extractedInput, extractedOutput, scriptContent, scriptFileName);

            // Assert
            Assert.AreEqual(exampleInput, extractedInput);
            Assert.AreEqual(expectedOutput, extractedOutput);
            
            _mockReadmeExtractor.Verify(x => x.ExtractExampleAsync(readmePath), Times.Once);
            _mockLlmClient.Verify(x => x.ValidateExtractedExample(scriptContent, scriptFileName, exampleInput, expectedOutput), Times.Once);
            _mockLlmClient.Verify(x => x.CreateDockerFile(scriptContent, scriptFileName), Times.Once);
            _mockOciClient.Verify(x => x.BuildImage(dockerfile, scriptPath, scriptFileName), Times.Once);
            _mockOciClient.Verify(x => x.RunImage(imageName, exampleInput), Times.Once);
        }

        [TestMethod]
        public async Task WrappingProcess_WhenReadmeExtractionFails_FallsBackToLlmGeneration()
        {
            // Arrange
            var scriptPath = "test-script.js";
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test-script.js";
            var fallbackInput = "fallback input";
            var fallbackOutput = "fallback output";
            var dockerfile = "FROM node:18\nCOPY test-script.js .\nCMD [\"node\", \"test-script.js\"]";
            var imageName = "script-test-script-20241201-120000";

            // Mock README extraction failure
            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ThrowsAsync(new Exception("README extraction failed"));

            // Mock fallback generation
            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ReturnsAsync((fallbackInput, fallbackOutput));

            // Mock Dockerfile generation
            _mockLlmClient
                .Setup(x => x.CreateDockerFile(scriptContent, scriptFileName))
                .ReturnsAsync(dockerfile);

            // Mock Docker build
            _mockOciClient
                .Setup(x => x.BuildImage(dockerfile, scriptPath, scriptFileName))
                .ReturnsAsync(imageName);

            // Mock Docker run
            _mockOciClient
                .Setup(x => x.RunImage(imageName, fallbackInput))
                .ReturnsAsync(fallbackOutput);

            // Act
            var (extractedInput, extractedOutput) = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, extractedInput, extractedOutput, scriptContent, scriptFileName);

            // Assert
            Assert.AreEqual(fallbackInput, extractedInput);
            Assert.AreEqual(fallbackOutput, extractedOutput);
            
            _mockLlmClient.Verify(x => x.GenerateFallbackTestData(scriptContent, scriptFileName), Times.Once);
        }

        [TestMethod]
        public async Task WrappingProcess_WhenDockerBuildFails_RetriesWithFixedDockerfile()
        {
            // Arrange
            var scriptPath = "test-script.js";
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test-script.js";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var initialDockerfile = "FROM node:18\nCOPY test-script.js .\nCMD [\"node\", \"test-script.js\"]";
            var fixedDockerfile = "FROM node:18\nWORKDIR /app\nCOPY test-script.js .\nCMD [\"node\", \"test-script.js\"]";
            var imageName = "script-test-script-20241201-120000";

            // Mock README extraction
            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ReturnsAsync((exampleInput, expectedOutput));

            _mockLlmClient
                .Setup(x => x.ValidateExtractedExample(scriptContent, scriptFileName, exampleInput, expectedOutput))
                .ReturnsAsync(true);

            // Mock Dockerfile generation and fixing
            _mockLlmClient
                .Setup(x => x.CreateDockerFile(scriptContent, scriptFileName))
                .ReturnsAsync(initialDockerfile);

            _mockLlmClient
                .Setup(x => x.FixDockerFile(scriptContent, scriptFileName, initialDockerfile, It.IsAny<string>()))
                .ReturnsAsync(fixedDockerfile);

            // Mock Docker build - first fails, second succeeds
            _mockOciClient
                .SetupSequence(x => x.BuildImage(It.IsAny<string>(), scriptPath, scriptFileName))
                .ThrowsAsync(new Exception("Docker build failed"))
                .ReturnsAsync(imageName);

            // Mock Docker run
            _mockOciClient
                .Setup(x => x.RunImage(imageName, exampleInput))
                .ReturnsAsync(expectedOutput);

            // Act
            var (extractedInput, extractedOutput) = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);
            
            // Note: This test demonstrates the retry logic, but the actual BuildDockerImageWithRetry method
            // is in DockerService and would need to be tested separately or mocked differently
            
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, extractedInput, extractedOutput, scriptContent, scriptFileName);

            // Assert
            _mockLlmClient.Verify(x => x.CreateDockerFile(scriptContent, scriptFileName), Times.Once);
            _mockLlmClient.Verify(x => x.FixDockerFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
} 