# Jit AI Challenge - Docker Script Wrapper

A C# tool that uses generative AI (OpenAI) to automatically wrap existing scripts in Docker containers, build and test the images, and verify their correctness. The tool is designed to be generic and work with any scripting language while optimizing API usage within a $5 budget.

## 🎯 Overview

This tool takes a script file and its corresponding README, analyzes them using AI, generates an appropriate Dockerfile, builds the Docker image, and validates that it works correctly by running test cases.

## 🏗️ Architecture

The project follows a clean architecture pattern with clear separation of concerns:

```
Jit-ai-challenge/
├── Jit/                          # Main C# application
│   ├── Program.cs                # Main orchestrator
│   ├── Interfaces/               # Interface definitions
│   │   ├── ILlm.cs              # LLM service interface
│   │   ├── IOci.cs              # Container operations interface
│   │   └── IReadmeExtractor.cs  # README extraction interface
│   ├── Concrete/                 # Concrete implementations
│   │   ├── OpenAiClient.cs      # OpenAI API client
│   │   ├── DockerService.cs     # Docker operations
│   │   └── ReadmeExtractor.cs   # README parsing
│   └── Jit.csproj               # Project file
├── Scripts/                      # Example scripts for testing
│   ├── line_counter.sh          # Bash script example
│   ├── vowel_counter.js         # Node.js script example
│   └── word_reverser.py         # Python script example
├── README_*.md                   # README files for each script
└── README.md                     # This file
```

## 🚀 Features

- **Multi-language Support**: Works with any scripting language (Bash, Python, Node.js, etc.)
- **AI-Powered Analysis**: Uses OpenAI to analyze scripts and generate appropriate Dockerfiles
- **Intelligent Test Data Extraction**: Extracts test examples from README files or generates fallback test data
- **Validation & Retry Logic**: Validates test data and retries with new data if tests fail
- **Robust Error Handling**: Comprehensive error handling with fallback mechanisms
- **API Usage Optimization**: Caching and efficient prompts to stay within budget
- **LLM Vendor Agnostic**: Interface-based design allows easy switching between LLM providers

## 📋 Prerequisites

- .NET 9.0 SDK
- Docker Desktop (running)
- OpenAI API key

## ⚙️ Setup

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd Jit-ai-challenge
   ```

2. **Set up environment variables**:
   Create a `.env` file in the project root:
   ```bash
   OPENAI_API_KEY=your_openai_api_key_here
   ```

3. **Build the project**:
   ```bash
   cd Jit
   dotnet build
   ```

## 🎮 Usage

### Basic Usage

```bash
cd Jit
dotnet run <script_path> <readme_path>
```

### Examples

**Bash Script (Line Counter)**:
```bash
dotnet run ../line_counter.sh ../README_line_counter.md
```

**Node.js Script (Vowel Counter)**:
```bash
dotnet run ../vowel_counter.js ../README_vowel_counter.md
```

**Python Script (Word Reverser)**:
```bash
dotnet run ../word_reverser.py ../README_word_reverser.md
```

## 🔧 How It Works

### 1. Script Analysis
- Reads the script file and analyzes its language and dependencies
- Extracts test examples from the README file
- Validates the extracted test data using AI

### 2. Dockerfile Generation
- Uses AI to generate an appropriate Dockerfile for the script
- Installs necessary dependencies and runtime
- Sets up the correct ENTRYPOINT for script execution

### 3. Container Building
- Builds the Docker image with retry logic
- Handles build errors by asking AI to fix the Dockerfile
- Provides detailed feedback on the build process

### 4. Testing & Validation
- Runs the container with extracted test data
- Validates that actual output matches expected output
- If validation fails, generates new test data and retries
- Supports up to 3 retry attempts with different test data

## 🧪 Example Scripts

The repository includes three example scripts to demonstrate the tool's capabilities:

### 1. Line Counter (Bash)
- **Script**: `line_counter.sh`
- **Function**: Counts lines in input text
- **Example**: `"Hello world\nThis is a test."` → `"Line Count: 2"`

### 2. Vowel Counter (Node.js)
- **Script**: `vowel_counter.js`
- **Function**: Counts vowels in input text
- **Example**: `"Hello world"` → `"Vowel Count: 3"`

### 3. Word Reverser (Python)
- **Script**: `word_reverser.py`
- **Function**: Reverses word order in input text
- **Example**: `"Hello world"` → `"world Hello"`

## 🔄 Process Flow

```
1. Parse Arguments → Validate Files → Read Script Content
2. Initialize Services (LLM, README Extractor, Docker)
3. Extract Test Data from README (with validation)
4. Generate Dockerfile using AI
5. Build Docker Image (with retry logic)
6. Run Test and Validate Output
7. If validation fails → Generate new test data → Retry
8. Success! Script is Dockerized and tested
```

## 🛡️ Error Handling & Fallbacks

The tool implements multiple layers of error handling:

1. **README Extraction Fallback**: If README parsing fails, generates test data from script analysis
2. **Test Data Validation**: AI validates extracted test data before use
3. **Docker Build Retry**: Up to 3 attempts with AI-generated fixes
4. **Test Validation Retry**: If test fails, generates new test data and retries
5. **Manual Override**: Option to manually enter test data if all automatic methods fail

## 💰 API Usage Optimization

- **Caching**: Test data is cached to avoid regenerating the same data
- **Efficient Prompts**: Optimized prompts to minimize token usage
- **Confidence Scoring**: AI provides confidence scores to avoid low-quality extractions
- **Budget Monitoring**: Designed to stay within $5 API budget

## 🔧 Configuration

### Environment Variables
- `OPENAI_API_KEY`: Your OpenAI API key (required)

### Model Configuration
The tool uses GPT-3.5-turbo by default, but you can configure a different model:

```csharp
var openAiClient = new OpenAiClient("gpt-4"); // or any other model
```

## 🧪 Testing

The tool includes built-in testing capabilities:

- **Automatic Test Data Generation**: Creates test cases when README examples are insufficient
- **Validation Retry Logic**: Automatically retries with new test data if validation fails
- **Script Analysis**: AI analyzes script behavior to generate accurate test cases

## 🚀 Advanced Features

### LLM Vendor Agnostic Design
The tool uses interfaces (`ILlm`, `IOci`, `IReadmeExtractor`) making it easy to:
- Switch between different LLM providers (OpenAI, Anthropic, etc.)
- Mock services for testing
- Implement different container runtimes

### Intelligent Test Data Generation
- Analyzes script behavior to understand what it actually does
- Generates test cases based on actual script output
- Provides explanations for why test cases were chosen

### Robust Validation
- Multi-step validation process
- Confidence scoring for extracted data
- Fallback mechanisms at every step

## 📝 Output

The tool provides detailed output including:

- ✅ Success messages for each step
- 🔄 Retry attempts and reasons
- 📋 Extracted test data with confidence scores
- 🐳 Docker build progress and image names
- 🧪 Test results and validation status
- ❌ Detailed error messages and fallback attempts

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is part of the Jit AI Challenge.

---

**Note**: This tool is designed to work within a $5 API budget constraint. The caching and optimization features help minimize API calls while maintaining functionality.