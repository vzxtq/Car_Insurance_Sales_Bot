namespace Car_Insurance_Bot.Models
{
    public class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }
    }

    public class Content
    {
        public List<Part>? Parts { get; set; }
        public string? Role { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }
}
