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
                accountName: "vzxtq17171",
                version: "1"
            );

            var response = await _mindeeClient
                .EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

            if (response == null)
            {
                throw new Exception("Mindee Vin Service response is null.");
            }

            if (response.Document == null)
            {
                throw new Exception("Mindee Vin Service document is null.");
            }

            if (response.Document.Inference == null)
            {
                throw new Exception("Mindee Vin Service inference is null.");
            }

            if (response.Document.Inference.Prediction == null)
            {
                throw new Exception("Mindee Vin Service prediction is null.");
            }

            if (response.Document.Inference.Prediction.Fields == null)
            {
                throw new Exception("Mindee Vin Service fields are null.");
            }

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