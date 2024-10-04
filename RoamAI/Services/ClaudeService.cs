using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoamAI.Services
{
    public class ClaudeService
    {
        private readonly AmazonBedrockRuntimeClient _runtimeClient;

        public ClaudeService(IConfiguration configuration)
        {
            var accessKeyId = configuration["AWS:AccessKeyId"];
            var secretAccessKey = configuration["AWS:SecretAccessKey"];
            var sessionToken = configuration["AWS:SessionToken"];
            var region = configuration["AWS:Region"];

            var credentials = new SessionAWSCredentials(accessKeyId, secretAccessKey, sessionToken);
            var config = new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };

            _runtimeClient = new AmazonBedrockRuntimeClient(credentials, config);
        }

        public async Task<string> GetTravelRecommendations(string country, string city, string travelDate, string travelPreferences)
        {
            var systemPrompt = $"Ülke-şehir-gideceği tarih: {country}-{city}-{travelDate}, gezi türü yüzdesi: {travelPreferences}. " +
               "Belirtilen yüzdelere göre gezilecek toplamda 5 yer ve konumlarını kordinatlarıyla öner. Ayrıca, gittiği günün milli  bayram olup olmadığını belirt. Yanıtı liste formatında ver.";

            var input = new
            {
                system = systemPrompt,
                messages = new[]
                {
            new
            {
                role = "user",
                content = $"{country}-{city}-{travelDate}, {travelPreferences}"
            }
        },
                max_tokens = 10000,
                temperature = 1,
                top_p = 0.999,
                top_k = 250,
                stop_sequences = new string[] { },
                anthropic_version = "bedrock-2023-05-31"
            };

            var inputJson = JsonSerializer.Serialize(input);

            var request = new InvokeModelRequest
            {
                ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
                ContentType = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(inputJson))
            };

            var response = await _runtimeClient.InvokeModelAsync(request);

            using (var reader = new StreamReader(response.Body))
            {
                var responseBody = await reader.ReadToEndAsync();

                // JSON yanıtını deserialization işlemi yap
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                // content içindeki metni al
                if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var content in contentArray.EnumerateArray())
                    {
                        if (content.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                        {
                            return textElement.GetString(); // text alanını döndür
                        }
                    }
                }

                return "No content found";
            }
        }
    }
}