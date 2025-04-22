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
                engine.SetVariable("tessedit_pageseg_mode", "6");

                using var pix = Pix.LoadFromFile(mrzImagePath);
                using var page = engine.Process(pix);

                var lines = page.GetText().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

                Console.WriteLine("OCR Result:\n" + string.Join("\n", lines));

                string line1 = null;
                string line2 = null;

                if(lines.Length == 1 && lines[0].Length >= 88)
                {
                    Console.WriteLine("Mrz possibly not cropped correctly. Trying to split the line...");
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
                    }
                }

                foreach(var line in lines)
                {
                    Console.WriteLine($"[OCR Line] ({line.Length}) : {line}");  
                }

                if (string.IsNullOrEmpty(line1) || string.IsNullOrEmpty(line2))
                    return ("Unknown", "Unknown");

                line1 = line1.PadRight(44, '<').ToUpperInvariant();
                var nameParts = line1.Substring(5).Split("<<", StringSplitOptions.RemoveEmptyEntries);

                string surname = nameParts.Length > 0 ? nameParts[0].Replace("<", " ").Trim() : "";
                string givenNames = nameParts.Length > 1 ? nameParts[1].Replace("<", " ").Trim() : "";

                string fullName = $"{surname} {givenNames}".Trim();

                line2 = line2.PadRight(44, '<').ToUpperInvariant();
                string passportNumber = Regex.Replace(line2.Substring(0, 9), "[^0-9]", "").Trim();

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

    public static class MrzCropper
    {
        public static string CropMrz(string inputPath)
        {
            var image = Cv2.ImRead(inputPath, OpenCvSharp.ImreadModes.Grayscale);

            int targetWidth = 1000;
            double scaleFactor = (double)targetWidth / image.Cols;
            int targetHeight = (int)(image.Rows * scaleFactor);
            Cv2.Resize(image, image, new OpenCvSharp.Size(targetWidth, targetHeight));

            var roi = new OpenCvSharp.Rect(0, (int)(image.Rows * 0.8), targetWidth, (int)(image.Rows * 0.2));
            var mrzRegion = new OpenCvSharp.Mat(image, roi);

            Cv2.GaussianBlur(mrzRegion, mrzRegion, new OpenCvSharp.Size(3, 3), 0);
            Cv2.AdaptiveThreshold(mrzRegion, mrzRegion, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 15, 10);

            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath), "mrz_" + Path.GetFileName(inputPath));
            Cv2.ImWrite(outputPath, mrzRegion);

            return outputPath;
        }
    }
}
