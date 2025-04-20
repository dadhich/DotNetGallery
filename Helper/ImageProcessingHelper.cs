// Helper/ImageProcessingHelper.cs - Utilities for image processing
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ModernGallery.Helper
{
    public static class ImageProcessingHelper
    {
        public static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            
            return destImage;
        }
        
        public static Image CropImage(Image image, Rectangle cropArea)
        {
            var bmpImage = new Bitmap(image);
            var bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            return bmpCrop;
        }
        
        public static string GetImageFormat(string filePath)
        {
            using (var img = Image.FromFile(filePath))
            {
                if (img.RawFormat.Equals(ImageFormat.Jpeg))
                    return "JPEG";
                if (img.RawFormat.Equals(ImageFormat.Png))
                    return "PNG";
                if (img.RawFormat.Equals(ImageFormat.Bmp))
                    return "BMP";
                if (img.RawFormat.Equals(ImageFormat.Gif))
                    return "GIF";
                if (img.RawFormat.Equals(ImageFormat.Tiff))
                    return "TIFF";
                
                return "Unknown";
            }
        }
        
        public static DateTime? GetDateTaken(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var img = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = null;
                    
                    try
                    {
                        propItem = img.GetPropertyItem(36867); // 0x9003 = DateTimeOriginal
                    }
                    catch
                    {
                        // Property doesn't exist
                        return null;
                    }
                    
                    if (propItem != null && propItem.Value != null)
                    {
                        // Convert from format "YYYY:MM:DD HH:MM:SS" to DateTime
                        string dateTaken = System.Text.Encoding.UTF8.GetString(propItem.Value).Trim('\0');
                        if (DateTime.TryParseExact(dateTaken, "yyyy:MM:dd HH:mm:ss", 
                            System.Globalization.CultureInfo.InvariantCulture, 
                            System.Globalization.DateTimeStyles.None, out DateTime result))
                        {
                            return result;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return null;
        }
        
        public static void DrawFaceRectangles(Graphics graphics, List<Rectangle> faceRectangles, Color color)
        {
            using (var pen = new Pen(color, 2))
            {
                foreach (var rect in faceRectangles)
                {
                    graphics.DrawRectangle(pen, rect);
                }
            }
        }
        
        public static void DrawObjectBoxes(Graphics graphics, List<(Rectangle Box, string Label, float Confidence)> objects)
        {
            foreach (var (box, label, confidence) in objects)
            {
                // Use different colors for different confidence levels
                Color color;
                if (confidence > 0.8f)
                    color = Color.Green;
                else if (confidence > 0.6f)
                    color = Color.Yellow;
                else
                    color = Color.Red;
                
                using (var pen = new Pen(color, 2))
                {
                    graphics.DrawRectangle(pen, box);
                }
                
                // Draw label with confidence
                var labelText = $"{label} ({confidence:P0})";
                var font = new Font("Arial", 10, FontStyle.Bold);
                var textSize = graphics.MeasureString(labelText, font);
                var textRect = new RectangleF(box.X, box.Y - textSize.Height, textSize.Width, textSize.Height);
                
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(180, color)), textRect);
                graphics.DrawString(labelText, font, Brushes.White, textRect);
            }
        }
    }
}