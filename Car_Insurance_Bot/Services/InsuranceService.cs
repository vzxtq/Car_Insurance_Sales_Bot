namespace Car_Insurance_Bot.Services
{
    public class InsuranceService
    {
        public async Task<string> GeneratePolicyAsync(string name, string passport, string vin)
        {
            string template = await File.ReadAllTextAsync("template.txt");

            string policy = template
                .Replace("{POLICY_NUMBER}", Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper())
                .Replace("{NAME}", name)
                .Replace("{PASSPORT}", passport)
                .Replace("{VIN}", vin);
            return policy;
        }
    }
}
