using Mindee;
using Mindee.Input;
using Mindee.Http;
using Mindee.Product.Generated;
using Mindee.Product.InternationalId;

namespace Car_Insurance_Bot.Services
{
    public class MindeePassportService
    {
        private readonly MindeeClient _mindeeClient;

        public MindeePassportService(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "Mindee API key is missing.");  
            }
            
            _mindeeClient = new MindeeClient(apiKey);
        }

        public async Task<(string Fullname, string IdNumber)> ProcessPassportAsync(string filePath)
        {
            var inputSource = new LocalInputSource(filePath);

            var response = await _mindeeClient
                .EnqueueAndParseAsync<InternationalIdV2>(inputSource);

            var prediction = response?.Document?.Inference?.Prediction;

            if (prediction == null)
            {
                throw new Exception("Failed to get prediction from Mindee response.");
            }

            var name = prediction.GivenNames?.FirstOrDefault()?.Value ?? "Unknown";
            var surname = prediction.Surnames?.FirstOrDefault()?.Value ?? "Unknown";
            var idNumber = prediction.DocumentNumber?.Value ?? "Unknown";

            var fullname = $"{name} {surname}".Trim();
            return (fullname, idNumber);
        }
    }
}