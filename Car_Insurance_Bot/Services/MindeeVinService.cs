using Mindee;
using Mindee.Input;
using Mindee.Http;
using Mindee.Product.Generated;
using Mindee.Product.Fr.IdCard;

namespace Car_Insurance_Bot.Services
{
    public class MindeeService 
    {
        private readonly MindeeClient _mindeeClient;

        public MindeeService(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "Mindee API key is missing.");  
            }

            _mindeeClient = new MindeeClient(apiKey);
        }
        public async Task<string> ProcessDocumentAsync(string filePath)
        {
            var inputSource = new LocalInputSource(filePath);

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path is missing.");

            CustomEndpoint endpoint = new CustomEndpoint(
            endpointName: "vin",
            accountName: "vzxtq",
            version: "1"
            );

            var response = await _mindeeClient
                .EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

            if (response.Document.Inference.Prediction.Fields.TryGetValue("vin", out var vinField))
            {
                var raw = vinField.ToString();
                var vin = raw.Replace(":value:", "").Trim();
                return vin;
            }

            return "VIN not found.";
        }
    }
}