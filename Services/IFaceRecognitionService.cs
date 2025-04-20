// Services/IFaceRecognitionService.cs - Interface for face recognition
using System.Collections.Generic;
using System.Threading.Tasks;
using ModernGallery.Models;

namespace ModernGallery.Services
{
    public interface IFaceRecognitionService
    {
        Task<Person> GetPersonForFaceAsync(byte[] faceEmbedding, float minConfidence = 0.6f);
        Task<int> CreatePersonAsync(string name, byte[] initialEmbedding);
        Task<bool> AddFaceToPersonAsync(int personId, byte[] faceEmbedding);
        Task<List<Person>> GetAllPeopleAsync();
        Task<List<GalleryImage>> FindImagesWithPersonAsync(int personId);
        Task<List<GalleryImage>> FindImagesWithPeopleAsync(List<int> personIds, bool requireAll = false);
        Task<List<GalleryImage>> FindImagesWithoutPersonAsync(int personId);
        Task<float> CalculateFaceSimilarityAsync(byte[] embedding1, byte[] embedding2);
        Task<bool> UpdatePersonNameAsync(int personId, string newName);
    }
}