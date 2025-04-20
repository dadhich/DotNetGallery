// Services/IImageService.cs - Interface for image scanning and management
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModernGallery.Models;

namespace ModernGallery.Services
{
    public interface IImageService
    {
        Task<List<string>> ScanDirectoryAsync(string directory, bool includeSubdirectories, CancellationToken cancellationToken = default);
        Task<string> GenerateThumbnailAsync(string imagePath, int maxSize = 256);
        Task<GalleryImage> ProcessImageAsync(string filePath);
        Task<List<GalleryImage>> GetImagesInDirectoryAsync(string directory);
        Task<GalleryImage> GetImageByPathAsync(string path);
    }
}