// Services/ImageService.cs - Implementation for image scanning and processing
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using ModernGallery.Models;
using Serilog;

namespace ModernGallery.Services
{
    public class ImageService : IImageService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAIService _aiService;
        private readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string _thumbnailDirectory;
        
        public ImageService(IDatabaseService databaseService, IAIService aiService)
        {
            _databaseService = databaseService;
            _aiService = aiService;
            
            _thumbnailDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModernGallery",
                "Thumbnails"
            );
            Directory.CreateDirectory(_thumbnailDirectory);
        }
        
        public async Task<List<string>> ScanDirectoryAsync(string directory, bool includeSubdirectories, CancellationToken cancellationToken = default)
        {
            try
            {
                Log.Information($"Scanning directory: {directory}, Include subdirectories: {includeSubdirectories}");
                
                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var imageFiles = new List<string>();
                
                foreach (var extension in _supportedExtensions)
                {
                    var files = Directory.GetFiles(directory, $"*{extension}", searchOption);
                    imageFiles.AddRange(files);
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Log.Information("Scanning cancelled by user");
                        break;
                    }
                }
                
                Log.Information($"Found {imageFiles.Count} image files");
                return imageFiles;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error scanning directory: {directory}");
                throw;
            }
        }
        
        public async Task<string> GenerateThumbnailAsync(string imagePath, int maxSize = 256)
        {
            try
            {
                var fileInfo = new FileInfo(imagePath);
                var thumbnailFilename = $"{GetMD5Hash(imagePath)}{fileInfo.Extension}";
                var thumbnailPath = Path.Combine(_thumbnailDirectory, thumbnailFilename);
                
                // Check if thumbnail already exists
                if (File.Exists(thumbnailPath))
                {
                    return thumbnailPath;
                }
                
                using (var image = Image.FromFile(imagePath))
                {
                    var ratioX = (double)maxSize / image.Width;
                    var ratioY = (double)maxSize / image.Height;
                    var ratio = Math.Min(ratioX, ratioY);
                    
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);
                    
                    using (var thumbnail = new Bitmap(newWidth, newHeight))
                    {
                        using (var graphics = Graphics.FromImage(thumbnail))
                        {
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                        }
                        
                        thumbnail.Save(thumbnailPath, ImageFormat.Jpeg);
                    }
                }
                
                return thumbnailPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error generating thumbnail for: {imagePath}");
                return null;
            }
        }
        
        public async Task<GalleryImage> ProcessImageAsync(string filePath)
        {
            try
            {
                Log.Information($"Processing image: {filePath}");
                
                // Check if image already exists in database
                var existingImage = await _databaseService.GetImageByPathAsync(filePath);
                if (existingImage != null)
                {
                    return existingImage;
                }
                
                var fileInfo = new FileInfo(filePath);
                
                // Generate basic metadata
                var image = new GalleryImage
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    DirectoryPath = fileInfo.DirectoryName,
                    FileSize = fileInfo.Length,
                    CreationDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime
                };
                
                // Get image dimensions
                using (var img = Image.FromFile(filePath))
                {
                    image.Width = img.Width;
                    image.Height = img.Height;
                }
                
                // Generate thumbnail
                image.ThumbnailPath = await GenerateThumbnailAsync(filePath);
                
                // Process with AI
                var aiResults = await _aiService.AnalyzeImageAsync(filePath);
                image.Description = aiResults.Description;
                image.AiGeneratedTags = string.Join(", ", aiResults.Tags);
                image.ContainsPeople = aiResults.ContainsPeople;
                image.FaceCount = aiResults.DetectedFaces.Count;
                
                // Add tags
                foreach (var tag in aiResults.Tags)
                {
                    image.Tags.Add(new ImageTag
                    {
                        TagName = tag,
                        Confidence = 1.0f // Default confidence
                    });
                }
                
                // Process faces
                if (image.ContainsPeople)
                {
                    foreach (var face in aiResults.DetectedFaces)
                    {
                        image.Faces.Add(new ImageFace
                        {
                            FaceRectangle = face.Rectangle,
                            FaceEmbedding = face.Embedding,
                            ConfidenceScore = face.Confidence
                        });
                    }
                }
                
                // Save to database
                await _databaseService.AddImageAsync(image);
                
                return image;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing image: {filePath}");
                return null;
            }
        }
        
        public async Task<List<GalleryImage>> GetImagesInDirectoryAsync(string directory)
        {
            return await _databaseService.GetImagesByDirectoryAsync(directory);
        }
        
        public async Task<GalleryImage> GetImageByPathAsync(string path)
        {
            return await _databaseService.GetImageByPathAsync(path);
        }
        
        private string GetMD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
    }
}