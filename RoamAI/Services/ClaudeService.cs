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
            var systemPrompt = "Kullanıcı sana 'ülke-şehir-gideceği tarih', 'gezi türü örnek olarak (%30 modern, %50 tarihisel, %10 yemek üzerine)' bilgilerini verecek, sende ona bu yüzdelere göre gezebileceği 5 yer ve konumunu vereceksin. Mesela bu mantıkta 2 tane tarihsel, 2 modern, 1 yemek üzerine vermen daha mantıklı. Ayrıca gittiği gün milli veya dini bayram olup olmadığına dair bilgi vereceksin.\n\n" +
                               "Örnek input : 'Japonya-Tokyo-28.09.2024' , '%40 Kültürel , %40 Modern , %20 Yemek Üzerine'\n\n" +
                               "Örnek çıktı : \n\n" +
                               "1) Sensoji Tapınağı (Asakusa):\n" +
                               "Adres: 2 Chome-3-1 Asakusa, Taito City, Tokyo 111-0032\n" +
                               "Koordinatlar: 35.7145°N, 139.7967°E\n\n" +
                               "2) Ueno Park & Tokyo Ulusal Müzesi:\n" +
                               "Adres: 9 Chome-83 Uenokoen, Taito City, Tokyo 110-0007\n" +
                               "Koordinatlar: 35.7161°N, 139.7671°E\n\n" +
                               "3) Ginza Six (AVM):\n" +
                               "Adres: 6 Chome-10-1 Ginza, Chuo City, Tokyo 104-0061\n" +
                               "Koordinatlar: 35.6697°N, 139.7623°E\n" +
                               "4) Tokyo Big Sight (Festival Merkezi):\n" +
                               "Adres: 3 Chome-11-1 Ariake, Koto City, Tokyo 135-0063\n" +
                               "Koordinatlar: 35.6293°N, 139.7945°E\n" +
                               "5) Sukiyabashi Jiro (Restoran):\n" +
                               "Adres: B1F, Tsukamoto Sogyo Building, 4 Chome-2-15 Ginza, Chuo City, Tokyo 104-0061\n" +
                               "Koordinatlar: 35.6720°N, 139.7634°E\n\n" +
                               "28 Eylül de milli veya dini bayram bulunmamaktadır. Ekstra kalabalık yaşanması beklenmemekte.";

            var input = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user", // Kullanıcı rolü ilk mesajda olmalı
                        content = $"{country}-{city}-{travelDate}, {travelPreferences}"
                    },
                    new
                    {
                        role = "assistant", // Sistemin belirttiği talimatlar ikinci mesajda
                        content = systemPrompt
                    }
                },
                max_tokens = 10000, // Maksimum token sayısı
                temperature = 1, // Çeşitlilik parametresi
                top_p = 0.999, // Olasılık parametresi
                top_k = 250, // Seçeneklerin genişliği
                stop_sequences = new string[] { }, // Boş bırakın veya tamamen kaldırın
                anthropic_version = "bedrock-2023-05-31" // API versiyonu
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