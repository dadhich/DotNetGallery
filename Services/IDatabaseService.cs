// Services/IDatabaseService.cs - Interface for database operations
using System.Collections.Generic;
using System.Threading.Tasks;
using ModernGallery.Models;

namespace ModernGallery.Services
{
    public interface IDatabaseService
    {
        void InitializeDatabase();
        Task<int> AddImageAsync(GalleryImage image);
        Task<bool> UpdateImageAsync(GalleryImage image);
        Task<GalleryImage> GetImageByIdAsync(int id);
        Task<GalleryImage> GetImageByPathAsync(string path);
        Task<List<GalleryImage>> GetImagesAsync(int skip = 0, int take = 100);
        Task<List<GalleryImage>> GetImagesByDirectoryAsync(string directory);
        Task<List<GalleryImage>> GetImagesByTagAsync(string tag);
        Task<List<GalleryImage>> GetImagesByPersonAsync(int personId);
        Task<List<Person>> GetAllPeopleAsync();
        Task<Person> GetPersonByNameAsync(string name);
        Task<int> AddPersonAsync(Person person);
        Task<bool> AssociateFaceWithPersonAsync(int faceId, int personId);
        Task<List<GalleryImage>> SearchImagesAsync(string searchTerm);
    }
}