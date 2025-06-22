using Moq;
using Concrete;
using Interfaces;

namespace Jit.Tests
{
    [TestClass]
    public class ValidatorTests
    {
        private Mock<ILlm> _mockLlmClient;
        private Mock<IOci> _mockOciClient;
        private Validator _validator;

        [TestInitialize]
        public void Setup()
        {
            _mockLlmClient = new Mock<ILlm>();
            _mockOciClient = new Mock<IOci>();
            _validator = new Validator(_mockLlmClient.Object);
        }

        [TestMethod]
        public void ValidateTestOutput_WhenOutputsMatch_ReturnsTrue()
        {
            // Arrange
            var actualOutput = "hello world";
            var expectedOutput = "hello world";

            // Act
            var result = _validator.ValidateTestOutput(actualOutput, expectedOutput);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateTestOutput_WhenOutputsMatchWithWhitespace_ReturnsTrue()
        {
            // Arrange
            var actualOutput = "  hello world  ";
            var expectedOutput = "hello world";

            // Act
            var result = _validator.ValidateTestOutput(actualOutput, expectedOutput);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateTestOutput_WhenOutputsDoNotMatch_ReturnsFalse()
        {
            // Arrange
            var actualOutput = "hello world";
            var expectedOutput = "goodbye world";

            // Act
            var result = _validator.ValidateTestOutput(actualOutput, expectedOutput);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RunTestAndValidateAsync_WhenTestPassesOnFirstTry_CompletesSuccessfully()
        {
            // Arrange
            var imageName = "test-image";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";

            _mockOciClient
                .Setup(x => x.RunImage(imageName, exampleInput))
                .ReturnsAsync(expectedOutput);

            // Act
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, exampleInput, expectedOutput, scriptContent, scriptFileName);

            // Assert
            _mockOciClient.Verify(x => x.RunImage(imageName, exampleInput), Times.Once);
            _mockLlmClient.Verify(x => x.GenerateTestDataFromScriptAnalysis(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task RunTestAndValidateAsync_WhenTestFailsFirstTimeButPassesSecondTime_CompletesSuccessfully()
        {
            // Arrange
            var imageName = "test-image";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";
            var actualOutput = "wrong output";
            var newInput = "new input";
            var newExpectedOutput = "new output";

            _mockOciClient
                .SetupSequence(x => x.RunImage(imageName, It.IsAny<string>()))
                .ReturnsAsync(actualOutput)  // First call returns wrong output
                .ReturnsAsync(newExpectedOutput); // Second call returns correct output

            _mockLlmClient
                .Setup(x => x.GenerateTestDataFromScriptAnalysis(scriptContent, scriptFileName, actualOutput, exampleInput, expectedOutput))
                .ReturnsAsync((newInput, newExpectedOutput));

            // Act
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, exampleInput, expectedOutput, scriptContent, scriptFileName);

            // Assert
            _mockOciClient.Verify(x => x.RunImage(imageName, It.IsAny<string>()), Times.Exactly(2));
            _mockLlmClient.Verify(x => x.GenerateTestDataFromScriptAnalysis(scriptContent, scriptFileName, actualOutput, exampleInput, expectedOutput), Times.Once);
        }

        [TestMethod]
        public async Task RunTestAndValidateAsync_WhenDockerRunFails_GeneratesNewTestDataAndRetries()
        {
            // Arrange
            var imageName = "test-image";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";
            var newInput = "new input";
            var newExpectedOutput = "new output";

            _mockOciClient
                .SetupSequence(x => x.RunImage(imageName, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Docker run failed")) // First call fails
                .ReturnsAsync(newExpectedOutput); // Second call succeeds

            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ReturnsAsync((newInput, newExpectedOutput));

            // Act
            await _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, exampleInput, expectedOutput, scriptContent, scriptFileName);

            // Assert
            _mockOciClient.Verify(x => x.RunImage(imageName, It.IsAny<string>()), Times.Exactly(2));
            _mockLlmClient.Verify(x => x.GenerateFallbackTestData(scriptContent, scriptFileName), Times.Once);
        }

        [TestMethod]
        public async Task RunTestAndValidateAsync_WhenAllRetriesExhausted_ThrowsException()
        {
            // Arrange
            var imageName = "test-image";
            var exampleInput = "test input";
            var expectedOutput = "test output";
            var scriptContent = "console.log('hello world');";
            var scriptFileName = "test.js";

            _mockOciClient
                .Setup(x => x.RunImage(imageName, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Docker run failed"));

            _mockLlmClient
                .Setup(x => x.GenerateFallbackTestData(scriptContent, scriptFileName))
                .ReturnsAsync(("new input", "new output"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _validator.RunTestAndValidateAsync(_mockOciClient.Object, imageName, exampleInput, expectedOutput, scriptContent, scriptFileName));

            // Verify it tried 3 times
            _mockOciClient.Verify(x => x.RunImage(imageName, It.IsAny<string>()), Times.Exactly(3));
        }
    }
} 