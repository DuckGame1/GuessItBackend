using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;


namespace GuessItAPI.Services
{
    public static class ImageHelper
    {
        public static byte[] ResizeImage(byte[] imageBytes, int maxSide = 128, int jpegQuality = 75)
        {
            using Image image = Image.Load(imageBytes);

            IImageFormat? format = image.Metadata.DecodedImageFormat;

            int width = image.Width;
            int height = image.Height;

            if (width > maxSide || height > maxSide)
            {
                float ratio = Math.Min((float)maxSide / width, (float)maxSide / height);

                int newWidth = (int)Math.Round(width * ratio);
                int newHeight = (int)Math.Round(height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            using var ms = new MemoryStream();

            if (format?.Name.Equals("PNG", StringComparison.OrdinalIgnoreCase) == true)
            {
                image.SaveAsPng(ms, new PngEncoder());
            }
            else
            {
                image.SaveAsJpeg(ms, new JpegEncoder
                {
                    Quality = jpegQuality
                });
            }

            return ms.ToArray();
        }
    }
}
