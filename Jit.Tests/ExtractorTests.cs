using Moq;
using Concrete;
using Interfaces;

namespace Jit.Tests
{
    [TestClass]
    public class ExtractorTests
    {
        private Mock<IReadmeExtractor> _mockReadmeExtractor;
        private Mock<ILlm> _mockLlmClient;
        private Extractor _extractor;
        private Mock<IInputSanitizer> _mockSanitizer;

        [TestInitialize]
        public void Setup()
        {
            _mockReadmeExtractor = new Mock<IReadmeExtractor>();
            _mockLlmClient = new Mock<ILlm>();
            _mockSanitizer = new Mock<IInputSanitizer>();
            _extractor = new Extractor(_mockReadmeExtractor.Object, _mockLlmClient.Object, _mockSanitizer.Object);
        }

        [TestMethod]
        public async Task ExtractTestDataAsync_WhenReadmeExtractionSucceedsAndValidationPasses_ReturnsExtractedData()
        {
            // Arrange
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";
            var expectedInput = "test input";
            var expectedOutput = "test output";

            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ReturnsAsync((expectedInput, expectedOutput));

            _mockLlmClient
                .Setup(x => x.ValidateExtractedExample(scriptContent, scriptFileName, expectedInput, expectedOutput))
                .ReturnsAsync(true);

            // Act
            var result = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);

            // Assert
            Assert.AreEqual(expectedInput, result.exampleInput);
            Assert.AreEqual(expectedOutput, result.expectedOutput);
            _mockReadmeExtractor.Verify(x => x.ExtractExampleAsync(readmePath), Times.Once);
            _mockLlmClient.Verify(x => x.ValidateExtractedExample(scriptContent, scriptFileName, expectedInput, expectedOutput), Times.Once);
        }

        [TestMethod]
        public async Task ExtractTestDataAsync_WhenReadmeExtractionSucceedsButValidationFails_FallsBackToLlmGeneration()
        {
            // Arrange
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";
            var readmeInput = "readme input";
            var readmeOutput = "readme output";
            var fallbackInput = "fallback input";
            var fallbackOutput = "fallback output";

            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ReturnsAsync((readmeInput, readmeOutput));

            _mockLlmClient
                .Setup(x => x.ValidateExtractedExample(scriptContent, scriptFileName, readmeInput, readmeOutput))
                .ReturnsAsync(false);

            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ReturnsAsync((fallbackInput, fallbackOutput));

            // Act
            var result = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);

            // Assert
            Assert.AreEqual(fallbackInput, result.exampleInput);
            Assert.AreEqual(fallbackOutput, result.expectedOutput);
            _mockLlmClient.Verify(x => x.GenerateFallbackTestData(scriptContent, scriptFileName), Times.Once);
        }

        [TestMethod]
        public async Task ExtractTestDataAsync_WhenReadmeExtractionFails_FallsBackToLlmGeneration()
        {
            // Arrange
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";
            var fallbackInput = "fallback input";
            var fallbackOutput = "fallback output";

            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ThrowsAsync(new Exception("README extraction failed"));

            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ReturnsAsync((fallbackInput, fallbackOutput));

            // Act
            var result = await _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName);

            // Assert
            Assert.AreEqual(fallbackInput, result.exampleInput);
            Assert.AreEqual(fallbackOutput, result.expectedOutput);
            _mockLlmClient.Verify(x => x.GenerateFallbackTestData(scriptContent, scriptFileName), Times.Once);
        }

        [TestMethod]
        public async Task ExtractTestDataAsync_WhenBothReadmeAndLlmFail_ThrowsException()
        {
            // Arrange
            var readmePath = "test-readme.md";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";

            _mockReadmeExtractor
                .Setup(x => x.ExtractExampleAsync(readmePath))
                .ThrowsAsync(new Exception("README extraction failed"));

            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ThrowsAsync(new Exception("LLM generation failed"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _extractor.ExtractTestDataAsync(readmePath, scriptContent, scriptFileName));
        }
    }
}