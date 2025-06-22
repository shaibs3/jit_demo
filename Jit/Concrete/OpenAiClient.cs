namespace Jit.Concrete
{
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;

    internal sealed class OpenAiClient : BaseLlmService
    {
        private readonly HttpClient m_httpClient;
        private readonly string m_model;

        internal OpenAiClient(string model = "gpt-3.5-turbo")
        {
            DotNetEnv.Env.Load();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            m_model = model;
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        protected override async Task<string> SendPromptToProvider(string prompt)
        {
            var requestBody = new
            {
                model = m_model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            StringContent content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await m_httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            
            // Parse the JSON and extract the message content
            using JsonDocument doc = JsonDocument.Parse(responseString);
            string? result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("No content received from OpenAI");

            return result;
        }
    }
}