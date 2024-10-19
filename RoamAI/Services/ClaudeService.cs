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

        private static Dictionary<string, string> locationCoordinates = new Dictionary<string, string>();

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

        public async Task<string> GetTravelRecommendations(string country, string city, string travelDate, int culturalPercentage, int modernPercentage, int foodPercentage)
        {
            // Prompt
            var systemPrompt = $"Ülke-şehir-gideceği tarih: {country}-{city}-{travelDate}, gezi türü yüzdesi: %Kültürel: {culturalPercentage}, %Modern: {modernPercentage}, %Yemek: {foodPercentage}. " +
            "Belirtilen yüzdelere göre gezilecek toplamda tam 5 yer ve konumlarını şu formatta öner: \n" +
            $"- Kültürel (%{culturalPercentage}):\n1. Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)\n2. Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)\n" +
            $"- Modern (%{modernPercentage}):\n3. Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)\n" +
            $"- Yemek Üzerine (%{foodPercentage}):\n4. Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)\n5. Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)\n" +
            "Son olarak, eğer gidilen tarihte milli bir bayram varsa belirt, yoksa şu mesajı ver: 'Not: {travelDate} tarihi, {country} bölgesinde herhangi bir milli bayrama denk gelmiyor.' Yanıtın her zaman bu formatta olmalı.";

            var input = new
            {
                system = systemPrompt,
                messages = new[]
                {
            new
            {
                role = "user",
                content = $"{country}-{city}-{travelDate}, %Kültürel: {culturalPercentage}, %Modern: {modernPercentage}, %Yemek: {foodPercentage}"
            }
        },
                max_tokens = 10000,
                temperature = 0.4,
                top_p = 0.8,
                top_k = 80,
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

                // JSON yanıtını parse et
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                // content içindeki metni al
                if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    int index = 0;

                    foreach (var content in contentArray.EnumerateArray())
                    {
                        if (content.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                        {
                            var fullText = textElement.GetString(); // Tüm yanıtı burada alıyoruz.

                            // Satırları ayırıyoruz
                            var lines = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                            // travelLocations array'ine kaydet
                            foreach (var line in lines)
                            {
                                // Lokasyon isimlerini ve koordinatlarını içeren satırları seçiyoruz
                                if (line.Contains("Koordinatlar:"))
                                {
                                    // a. Roma Köprüsü (Koordinatlar: 35.5667° N, 6.1833° E)
                                    // Burada yer ismini ve koordinatları ayıklıyoruz
                                    var location = line.Split(new[] { ".", "(", "Koordinatlar:" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

                                    var coordinates = line.Substring(line.IndexOf("Koordinatlar:") + "Koordinatlar:".Length)
                        .Trim() // Boşlukları temizle
                        .TrimEnd(')'); // Sondaki ")" karakterini temizle
                                       // Konum ve koordinatları birleştirip diziye kaydediyoruz
                                    locationCoordinates.Add(location, coordinates);
                                    index++;
                                }
                            }

                            // Diziyi yazdır
                            Console.WriteLine("Önerilen Seyahat Yerleri:");
                            foreach (var entry in locationCoordinates)
                            {
                                Console.WriteLine($"{entry.Key} {entry.Value}");
                            }

                            // Tüm yanıtı döndür (text alanını olduğu gibi döndür)
                            return fullText;
                        }
                    }
                }

                return "No content found";
            }
        }

        public async Task<string> GetCityInformation(string country, string city)
        {
            // Claude için şehir bilgisi istemek üzere bir prompt oluşturuyoruz.
            var systemPrompt = $"Ülke: {country}, Şehir: {city}. Bu şehir hakkında detaylı bir paragraf bilgi ver.";

            var input = new
            {
                system = systemPrompt,
                messages = new[]
                {
            new
            {
                role = "user",
                content = $"Şehir: {city} hakkında bilgi ver"
            }
        },
                max_tokens = 1000,
                temperature = 0.5,
                top_p = 0.8,
                top_k = 80,
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

                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                // Claude yanıtından şehir bilgisi paragrafını alıyoruz.
                if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var content in contentArray.EnumerateArray())
                    {
                        if (content.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                        {
                            var cityInformation = textElement.GetString();
                            return cityInformation;
                        }
                    }
                }

                return "Şehir hakkında bilgi bulunamadı.";
            }
        }

    }
}