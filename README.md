# Jit AI Challenge - Intelligent Docker Script Wrapper

A sophisticated C# application that leverages generative AI to automatically containerize scripts, extract test data from README files, and validate the resulting Docker images. Built with clean architecture principles, comprehensive security validation, and provider-agnostic design.

## 🎯 Overview

This tool transforms any script into a production-ready Docker container by:
1. **Analyzing** script content and README documentation using AI
2. **Extracting** test cases from README files with confidence scoring
3. **Generating** optimized Dockerfiles tailored to the script's requirements
4. **Building** Docker images with intelligent error recovery
5. **Validating** functionality through automated testing
6. **Securing** all inputs against threats and prompt injection attacks

## 🏗️ Architecture

The project implements a clean, layered architecture with dependency injection:

```
JitDemo/
├── Jit/                          # Core application
│   ├── Program.cs                # Application entry point & orchestration
│   ├── Interfaces/               # Contract definitions
│   │   ├── ILlm.cs              # LLM service abstraction
│   │   ├── IOci.cs              # Container operations
│   │   ├── IReadmeExtractor.cs  # README parsing
│   │   ├── ITestDataExtractor.cs # Test data extraction
│   │   ├── IValidator.cs        # Validation logic
│   │   └── IInputSanitizer.cs   # Security validation
│   ├── Concrete/                 # Implementation layer
│   │   ├── BaseLlmService.cs    # Abstract LLM service (provider-agnostic)
│   │   ├── OpenAiClient.cs      # OpenAI implementation
│   │   ├── DockerService.cs     # Docker operations
│   │   ├── ReadmeExtractor.cs   # README parsing with constructor injection
│   │   ├── TestDataExtractor.cs # Test data extraction with fallbacks
│   │   ├── Validator.cs         # Multi-step validation
│   │   └── InputSanitizer.cs    # Comprehensive security validation
│   └── Models/                   # Data structures
│       └── SanitizationResult.cs # Security validation results
├── Jit.Tests/                    # Comprehensive test suite
├── Scripts/                      # Example implementations
│   ├── line_counter.sh          # Bash script example
│   ├── vowel_counter.js         # Node.js script example
│   └── word_reverser.py         # Python script example
└── README_*.md                   # Documentation for each script
```

## 🚀 Key Features

### 🤖 AI-Powered Intelligence
- **Smart Dockerfile Generation**: Context-aware Dockerfile creation using LLM
- **Intelligent Test Extraction**: Extracts test cases from README with confidence scoring
- **Adaptive Fallback Generation**: Creates test data when README examples are insufficient
- **Script Behavior Analysis**: Understands script functionality to generate accurate tests

### 🛡️ Enterprise-Grade Security
- **Comprehensive Input Validation**: Validates all inputs for security threats
- **Prompt Injection Detection**: Prevents LLM prompt injection attacks
- **Script Content Analysis**: Identifies potentially dangerous script content
- **Sanitization Pipeline**: Cleans inputs while preserving functionality
- **Security Recommendations**: Provides actionable security improvement suggestions

### 🏗️ Clean Architecture
- **Provider Agnostic**: Easy switching between LLM providers (OpenAI, Anthropic, etc.)
- **Dependency Injection**: Constructor-based DI for testability and flexibility
- **Interface Segregation**: Focused interfaces for single responsibility
- **Single Responsibility**: Each class has a clear, focused purpose
- **Extensible Design**: Easy to add new features and providers

### 🔄 Robust Error Handling
- **Multi-Layer Fallbacks**: Multiple recovery mechanisms at each step
- **Intelligent Retry Logic**: Smart retry with different strategies
- **Manual Override**: Interactive fallback when automation fails
- **Comprehensive Logging**: Detailed, emoji-rich output for debugging

## 🔧 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop (running)
- OpenAI API key

### Setup
```bash
# Clone and navigate
git clone <repository-url>
cd JitDemo

# Set environment variable
export OPENAI_API_KEY="your-api-key-here"

# Build the project
cd Jit
dotnet build
```

### Usage
```bash
# Basic usage
dotnet run <scriptPath> <readmePath>

# Examples
dotnet run ../line_counter.sh ../README_line_counter.md
dotnet run ../vowel_counter.js ../README_vowel_counter.md
dotnet run ../word_reverser.py ../README_word_reverser.md
```

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Test Coverage
- **ExtractorTests**: Test data extraction logic
- **ValidatorTests**: Validation processes
- **InputSanitizerTests**: Security validation
- **Integration Tests**: End-to-end workflow validation

## 🏗️ Design Patterns

### Dependency Injection
```csharp
// Clean constructor injection
var readmeExtractor = new ReadmeExtractor(llmClient, readmePath);
var testDataExtractor = new TestDataExtractor(readmeExtractor, llmClient, sanitizer);
var validator = new Validator(llmClient);
```

### Provider Agnostic Design
```csharp
// Base class contains all business logic
public abstract class BaseLlmService : ILlm
{
    // All prompts and business logic here
    protected abstract Task<string> SendPromptToProvider(string prompt);
}

// Easy to add new providers
public class AnthropicClient : BaseLlmService
{
    protected override async Task<string> SendPromptToProvider(string prompt)
    {
        // Only implement provider-specific API call
    }
}
```

### Interface Segregation
- `ILlm`: LLM operations (Dockerfile generation, test extraction)
- `IReadmeExtractor`: README parsing with constructor injection
- `ITestDataExtractor`: Test data extraction with fallbacks
- `IValidator`: Multi-step validation logic
- `IInputSanitizer`: Comprehensive security validation

## 🔒 Security Architecture

### Input Validation Pipeline
1. **Script Content Analysis**: Validates script for security threats
2. **Test Data Sanitization**: Ensures test data is safe for execution
3. **Prompt Injection Detection**: Blocks LLM prompt injection attempts
4. **Threat Classification**: Categorizes threats by severity
5. **Recommendation Engine**: Provides actionable security advice

### Security Features
- **Threat Detection**: Identifies dangerous patterns and content
- **Sanitization**: Cleans inputs while preserving functionality
- **Confidence Scoring**: Rates security validation confidence
- **Manual Override**: Interactive security validation when needed
- **Audit Trail**: Comprehensive logging of security decisions

## 📊 Rich Output

The application provides detailed, emoji-rich feedback:

```
✅ README extraction validated successfully
📋 Extracted test data:
   Input: 'hello world'
   Expected Output: 'Vowel Count: 3'
   Confidence: 95%
🐳 Building Docker image: jit-script-abc123
🧪 Running test: Input='hello world', Expected='Vowel Count: 3'
✅ Test passed! Actual output matches expected
⚠️ Security warning: Script contains potential threat
💡 Recommendation: Review script for command injection
```

## 🚀 Advanced Features

### Intelligent Caching
- **Test Data Cache**: Avoids regenerating identical test data
- **README Cache**: Caches README extraction results
- **Fallback Cache**: Caches generated fallback data by script hash
- **Performance Optimization**: Reduces API calls and improves speed

### Adaptive Validation
- **Multi-Step Validation**: Validates extracted data before use
- **Confidence Scoring**: AI provides confidence scores for extractions
- **Fallback Mechanisms**: Multiple strategies when validation fails
- **Script Analysis**: Understands actual script behavior for accurate tests

### Error Recovery
- **Docker Build Retry**: Up to 3 attempts with AI-generated fixes
- **Test Validation Retry**: Generates new test data if validation fails
- **Manual Intervention**: Interactive mode when automation fails
- **Graceful Degradation**: Continues with reduced functionality when possible

## 🤝 Contributing

### Development Guidelines
1. **Follow Architecture**: Maintain clean architecture principles
2. **Add Interfaces**: Create interfaces for new services
3. **Include Tests**: Add comprehensive unit tests
4. **Update Documentation**: Keep README and comments current
5. **Security First**: Validate all inputs and outputs

### Code Standards
- Use constructor injection for dependencies
- Implement focused interfaces
- Add comprehensive error handling
- Include security validation
- Write clear, documented code

## 🆘 Troubleshooting

### Common Issues

**Missing API Key**
```bash
export OPENAI_API_KEY="your-key-here"
```

**Docker Not Running**
```bash
# Start Docker Desktop
# Verify with: docker --version
```

**File Not Found**
```bash
# Verify paths are correct and files exist
ls -la <scriptPath> <readmePath>
```

**Security Warnings**
- Review detected threats in output
- Follow security recommendations
- Consider manual validation if needed

### Debug Mode
The application provides detailed logging:
- API call details and responses
- Sanitization results and decisions
- Validation steps and confidence scores
- Fallback attempts and reasons

## 🔮 Future Roadmap

### Planned Enhancements
- **Multi-Provider Support**: Anthropic, Gemini, local models
- **Enhanced Security**: Advanced threat detection and prevention
- **Performance Optimization**: Parallel processing and caching improvements
- **Web Interface**: Browser-based UI for easier interaction
- **CI/CD Integration**: GitHub Actions and GitLab CI support
- **Plugin System**: Extensible architecture for custom providers

### Architecture Evolution
- **Microservices**: Split into focused microservices
- **Event-Driven**: Implement event sourcing for better traceability
- **Configuration Management**: Centralized configuration system
- **Monitoring**: Comprehensive metrics and alerting

## 📝 License

This project is part of the Jit AI Challenge and is provided as-is for educational and demonstration purposes.

---

**Built with ❤️ using clean architecture principles, comprehensive security validation, and provider-agnostic design.**
