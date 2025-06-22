# Jit AI Challenge - Docker Script Wrapper

A C# tool that uses generative AI (OpenAI) to automatically wrap existing scripts in Docker containers, build and test the images, and verify their correctness. The tool is designed to be generic and work with any scripting language while optimizing API usage within a $5 budget.


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


### Environment Variables
- `OPENAI_API_KEY`: Your OpenAI API key (required)

### Model Configuration
The tool uses GPT-3.5-turbo by default, but you can configure a different model:

## 📝 Output

The tool provides detailed output including:

- ✅ Success messages for each step
- 🔄 Retry attempts and reasons
- 📋 Extracted test data with confidence scores
- 🐳 Docker build progress and image names
- 🧪 Test results and validation status
- ❌ Detailed error messages and fallback attempts