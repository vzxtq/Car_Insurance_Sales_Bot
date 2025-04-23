
namespace Car_Insurance_Bot.Utils
{
    public class MarkDownEscaper
    {
        public string EscapeMarkdown(string text)
        {
            var specialChars = new[] { "_", "*", "[", "]", "(", ")", "~", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var c in specialChars)
            {
                text = text.Replace(c, "\\" + c);
            }
            return text;
        }
    }
}