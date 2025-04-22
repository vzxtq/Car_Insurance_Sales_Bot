using OpenCvSharp;

namespace Car_Insurance_Bot.Services
{
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

            Cv2.GaussianBlur(mrzRegion, mrzRegion, new OpenCvSharp.Size(3, 3), 0);//noise reduction and text enchancement
            Cv2.Threshold(mrzRegion, mrzRegion, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath), "mrz_" + Path.GetFileName(inputPath));
            Cv2.ImWrite(outputPath, mrzRegion);

            return outputPath;
        }
    }
}
