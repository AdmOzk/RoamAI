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
            var input = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user", // Kullanıcı rolü
                        content = $"Provide travel recommendations for {country}, {city} on {travelDate} with preferences: {travelPreferences}."
                    }
                },
                max_tokens = 300, // Maksimum token sayısı
                temperature = 0.1, // Çeşitlilik parametresi
                top_p = 0.9, // Olasılık parametresi
                top_k = 7, // Seçeneklerin genişliği
                stop_sequences = new[] { "Human:" }, // Durdurma dizisi
                anthropic_version = "bedrock-2023-05-31" // API versiyonu, AWS dökümantasyonundan kontrol edin
            };

            var inputJson = JsonSerializer.Serialize(input);

            var request = new InvokeModelRequest
            {
                ModelId = "anthropic.claude-3-sonnet-20240229-v1:0", // Doğru model ID'si
                ContentType = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(inputJson))
            };

            var response = await _runtimeClient.InvokeModelAsync(request);

            using (var reader = new StreamReader(response.Body))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}