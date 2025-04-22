using System.Text.RegularExpressions;
using Tesseract;
using OpenCvSharp;

namespace Car_Insurance_Bot.Services
{
    public class TesseractPassportService
    {
        private readonly string _tessDataPath;

        public TesseractPassportService(string tessDataPath)
        {
            _tessDataPath = tessDataPath;
        }

        private string CleanNoise(string raw)
        {
            raw = Regex.Replace(raw.ToUpperInvariant(), @"[^A-Z<]", string.Empty);
            raw = Regex.Replace(raw, "(<)\\1{2,}", "$1");
            return raw;
        }

        public async Task<(string Name, string Passport)> ParsePassport(string imagePath)
        {
            try
            {
                string mrzImagePath = MrzCropper.CropMrz(imagePath);

                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ<0123456789");
                engine.SetVariable("tessedit_pageseg_mode", "7");

                using var pix = Pix.LoadFromFile(mrzImagePath);
                using var page = engine.Process(pix);

                var lines = page.GetText().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

                Console.WriteLine("OCR Result:\n" + string.Join("\n", lines));

                string line1 = null;
                string line2 = null;

                if (lines.Length == 1 && lines[0].Length >= 88)
                {
                    Console.WriteLine("MRZ possibly not cropped correctly. Trying to split the line...");
                    string fullMrz =  lines[0].PadRight(88, '<');
                    line1 = fullMrz.Substring(0, 44);
                    line2 = fullMrz.Substring(44, 44);
                }
                else
                {
                    line1 = lines.FirstOrDefault(l => l.StartsWith("P<"));
                    line2 = lines.FirstOrDefault(l => Regex.IsMatch(l, @"^\d{9}"));

                    if (string.IsNullOrEmpty(line2) && lines.Length >= 2)
                    {
                        line2 = lines[1];
                        Console.WriteLine($"[INFO] line2 was empty. Assigned value: {line2}");
                    }
                }

                Console.WriteLine($"line1: {line1}");
                Console.WriteLine($"line2: {line2}");

                string fullName = "Unknown";
                string passportNumber = "Unknown";

                if (!string.IsNullOrEmpty(line1) && line1.StartsWith("P<"))
                {
                    var nameParts = line1.Substring(5).Split("<<", StringSplitOptions.RemoveEmptyEntries);
                    string surname = nameParts.Length > 0 ? nameParts[0].Replace("<", " ").Trim() : "";
                    string givenNames = nameParts.Length > 1 ? nameParts[1].Replace("<", " ").Trim() : "";
                    fullName = $"{surname} {givenNames}".Trim();

                    Console.WriteLine($"Name extracted: {fullName}");
                }
                else
                {
                    Console.WriteLine("line1 is too short or empty to extract name.");
                }

                if (!string.IsNullOrEmpty(line2))
                {
                    passportNumber = Regex.Replace(line2.Substring(0, 9), "[^0-9]", "").Trim();
                    Console.WriteLine($"Passport number extracted from line2: {passportNumber}");
                }
                else if (line1.Length >= 44)
                {
                    passportNumber = Regex.Replace(line1.Substring(44, 9), "[^0-9]", "").Trim();
                    Console.WriteLine($"Passport number extracted from line1: {passportNumber}");
                }

                return (
                    string.IsNullOrWhiteSpace(fullName) ? "Unknown" : fullName,
                    string.IsNullOrWhiteSpace(passportNumber) ? "Unknown" : passportNumber
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return ("Error", "Error");
            }
        }
    }
}
