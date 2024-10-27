using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using RoamAI.Models;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoamAI.Services
{
    public class ClaudeService
    {
        private readonly AmazonBedrockRuntimeClient _runtimeClient;

        public static Dictionary<string, string> locationCoordinates = new Dictionary<string, string>();

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

        public async Task<ClaudeResponseModel> GetTravelRecommendations(string country, string city, DateTime StartDate,DateTime EndDate, int culturalPercentage, int EntertainmantPercentage, int foodPercentage)
        {

            ClaudeResponseModel cresponse = new ClaudeResponseModel();
            // Prompt
           var systemPrompt = $"Ülke-şehir-gideceği tarih: {country}-{city}-{StartDate}-{EndDate}, gezi türü yüzdesi: %Kültürel: {culturalPercentage}, %Eğlence: {EntertainmantPercentage}, %Yemek: {foodPercentage}. " +
    "Her gün için önerilecek toplam yer sayısını sen belirle ve bu sayıyı yüzdelere göre kültürel, eğlence ve yemek kategorilerine böl. Önerdiğin yer sayısı her kategori için yüzdeye uygun olmalı. " +
    "Her gün farklı yerler öner ve bu yerler benzersiz olmalıdır. Önerilerin her biri 'Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)' formatında olmalıdır. " +
    "Her günün önerilerini şu şekilde yüzdelere göre dağıt:\n" +
    "1. Gün (Giriş Tarihi: {StartDate}):\n" +
    $"- Kültürel (%{culturalPercentage}): Kültürel yer önerilerini günün toplam öneri sayısının %{culturalPercentage} kadarı olacak şekilde yap.\n" +
    $"- Modern (%{EntertainmantPercentage}): Eğlence yer önerilerini günün toplam öneri sayısının %{EntertainmantPercentage} kadarı olacak şekilde yap.\n" +
    $"- Yemek (%{foodPercentage}): Yemek yer önerilerini günün toplam öneri sayısının %{foodPercentage} kadarı olacak şekilde yap.\n" +
    "Her gün için öneri yaparken, her kategoride benzersiz yerler öner ve aynı yeri başka bir günde tekrar etme. Her öneri, belirtilen formatta ('Yer İsmi (Koordinatlar: xx.xxxx°N, xx.xxxx°E)') olmalıdır. " +
   "Günleri kalacağı gün kadar devam ettir. Günlerin tamamını detaylı olarak listele, kısaltma ya da '...' ifadesi kullanma. Kullanıcının belirttiği gün sayısı kadar günü ayrı ayrı detaylandır." +
    "Belirtilen tarih aralığında ki tüm günleri tek tek kontrol et belirtilen ülkede herhangi resmi tatil günü varsa döndür. Mesela : verdiğin liste de ki günler 28 ekim , 29 ekim 30 ekim burada 29 ekim türkiyede resmi tatildir ona göre 29 ekim var dikkatli olunuz gibi mesaj döndürmen lazım Yoksa en sonda ayrı bir yerde şu mesajı ver: 'Gideceğiniz tarih herhangi bir milli bayrama denk gelmiyor.' Yanıtın her zaman bu formatta olmalı.";

            var input = new
            {
                system = systemPrompt,
                messages = new[]
                {
            new
            {
                role = "user",
                content = $"{country}-{city}-{StartDate}-{EndDate}, %Kültürel: {culturalPercentage}, %Modern: {EntertainmantPercentage}, %Yemek: {foodPercentage}"
            }
        },
                max_tokens = 10000,
                temperature = 0.1,
                top_p = 0.95,
                top_k = 5,
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
                                    var lineWithoutNumbering = line.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' ');
                                    var locationEndIndex = lineWithoutNumbering.IndexOf("(Koordinatlar:");
                                    var location = lineWithoutNumbering.Substring(0, locationEndIndex).Replace("-", "").Trim();

                                    var coordinatesStartIndex = lineWithoutNumbering.IndexOf("Koordinatlar:") + "Koordinatlar:".Length;
                                    var coordinates = lineWithoutNumbering.Substring(coordinatesStartIndex).Trim().TrimEnd(')');
                                    // Konum ve koordinatları birleştirip diziye kaydediyoruz
                                    if (!locationCoordinates.ContainsKey(location))
                                    {
                                        locationCoordinates.Add(location, coordinates);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Warning: Location {location} with coordinates {coordinates} already exists.");
                                    }
                                    index++;
                                }
                            }

                            Dictionary<string, string> LocationsAndCoordinates = new Dictionary<string, string>();
                            
                            
                            
                            foreach (var entry in locationCoordinates)
                            {
                                LocationsAndCoordinates.Add(entry.Key, entry.Value);    
                            }



                            cresponse.LocationCoordinates = LocationsAndCoordinates;
                            cresponse.text = fullText;
                            
                            return cresponse;
                        }
                    }
                }

                return cresponse;

                
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