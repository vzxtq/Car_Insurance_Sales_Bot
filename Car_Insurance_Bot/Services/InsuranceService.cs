namespace Car_Insurance_Bot.Services
{
    public class InsuranceService
    {
        public async Task<string> GeneratePolicyAsync(string name, string passport, string vin)
        {
            // Generate a mock VIN for testing purposes
            vin = "1234567890123456"; // Example VIN for testing

            // Read the template file asynchronously
            string template = await File.ReadAllTextAsync("template.txt");

            // Replace placeholders in the template with actual values
            string policy = template
                .Replace("{POLICY_NUMBER}", Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper())
                .Replace("{NAME}", name)
                .Replace("{PASSPORT}", passport)
                .Replace("{VIN}", vin);
            return policy;
        }
    }
}
