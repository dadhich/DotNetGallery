using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModernGallery.Models;
using Serilog;

namespace ModernGallery.Services
{
    public partial class FaceRecognitionService : IFaceRecognitionService
    {
        // Continuation of previous implementation
        public async Task<bool> AddFaceToPersonAsync(int personId, byte[] faceEmbedding)
        {
            try
            {
                var people = await _databaseService.GetAllPeopleAsync();
                var person = people.FirstOrDefault(p => p.Id == personId);
                
                if (person == null)
                {
                    return false;
                }
                
                // Update the average embedding
                if (person.AverageEmbedding == null || person.AverageEmbedding.Length == 0)
                {
                    // First face for this person
                    person.AverageEmbedding = faceEmbedding;
                }
                else
                {
                    // Update the average embedding
                    int embeddingLength = faceEmbedding.Length / sizeof(float);
                    float[] currentEmbedding = new float[embeddingLength];
                    float[] newEmbedding = new float[embeddingLength];
                    
                    Buffer.BlockCopy(person.AverageEmbedding, 0, currentEmbedding, 0, person.AverageEmbedding.Length);
                    Buffer.BlockCopy(faceEmbedding, 0, newEmbedding, 0, faceEmbedding.Length);
                    
                    // Calculate average
                    for (int i = 0; i < embeddingLength; i++)
                    {
                        currentEmbedding[i] = (currentEmbedding[i] + newEmbedding[i]) / 2.0f;
                    }
                    
                    // Convert back to byte array
                    byte[] averageEmbedding = new byte[embeddingLength * sizeof(float)];
                    Buffer.BlockCopy(currentEmbedding, 0, averageEmbedding, 0, averageEmbedding.Length);
                    person.AverageEmbedding = averageEmbedding;
                }
                
                // Update the person
                var updatedPerson = new Person
                {
                    Id = person.Id,
                    Name = person.Name,
                    AverageEmbedding = person.AverageEmbedding
                };
                
                return await _databaseService.UpdatePersonAsync(updatedPerson);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error adding face to person: {personId}");
                return false;
            }
        }
        
        public async Task<List<Person>> GetAllPeopleAsync()
        {
            try
            {
                return await _databaseService.GetAllPeopleAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all people");
                return new List<Person>();
            }
        }
        
        public async Task<List<GalleryImage>> FindImagesWithPersonAsync(int personId)
        {
            try
            {
                return await _databaseService.GetImagesByPersonAsync(personId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error finding images with person: {personId}");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<GalleryImage>> FindImagesWithPeopleAsync(List<int> personIds, bool requireAll = false)
        {
            try
            {
                var allImages = new List<GalleryImage>();
                
                foreach (var personId in personIds)
                {
                    var images = await _databaseService.GetImagesByPersonAsync(personId);
                    allImages.AddRange(images);
                }
                
                if (requireAll)
                {
                    // Filter to only include images that have all specified people
                    var filteredImages = allImages
                        .GroupBy(i => i.Id)
                        .Where(g => g.Count() == personIds.Count)
                        .Select(g => g.First())
                        .ToList();
                    
                    return filteredImages;
                }
                else
                {
                    // Remove duplicates
                    return allImages
                        .GroupBy(i => i.Id)
                        .Select(g => g.First())
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error finding images with multiple people");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<GalleryImage>> FindImagesWithoutPersonAsync(int personId)
        {
            try
            {
                // Get all images with faces
                var allImages = await _databaseService.GetImagesWithFacesAsync();
                
                // Get images with the specified person
                var imagesWithPerson = await _databaseService.GetImagesByPersonAsync(personId);
                
                // Filter out images that contain the person
                var imagesWithoutPerson = allImages
                    .Where(i => !imagesWithPerson.Any(p => p.Id == i.Id))
                    .ToList();
                
                return imagesWithoutPerson;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error finding images without person: {personId}");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<float> CalculateFaceSimilarityAsync(byte[] embedding1, byte[] embedding2)
        {
            try
            {
                if (embedding1 == null || embedding2 == null ||
                    embedding1.Length == 0 || embedding2.Length == 0 ||
                    embedding1.Length != embedding2.Length)
                {
                    return 0;
                }
                
                // Convert byte arrays to float arrays
                int embeddingLength = embedding1.Length / sizeof(float);
                float[] vec1 = new float[embeddingLength];
                float[] vec2 = new float[embeddingLength];
                
                Buffer.BlockCopy(embedding1, 0, vec1, 0, embedding1.Length);
                Buffer.BlockCopy(embedding2, 0, vec2, 0, embedding2.Length);
                
                // Calculate cosine similarity
                float dotProduct = 0;
                float mag1 = 0;
                float mag2 = 0;
                
                for (int i = 0; i < embeddingLength; i++)
                {
                    dotProduct += vec1[i] * vec2[i];
                    mag1 += vec1[i] * vec1[i];
                    mag2 += vec2[i] * vec2[i];
                }
                
                mag1 = (float)Math.Sqrt(mag1);
                mag2 = (float)Math.Sqrt(mag2);
                
                if (mag1 > 0 && mag2 > 0)
                {
                    return dotProduct / (mag1 * mag2);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating face similarity");
                return 0;
            }
        }
        
        public async Task<bool> UpdatePersonNameAsync(int personId, string newName)
        {
            try
            {
                var people = await _databaseService.GetAllPeopleAsync();
                var person = people.FirstOrDefault(p => p.Id == personId);
                
                if (person == null)
                {
                    return false;
                }
                
                person.Name = newName;
                
                var updatedPerson = new Person
                {
                    Id = person.Id,
                    Name = person.Name,
                    AverageEmbedding = person.AverageEmbedding
                };
                
                return await _databaseService.UpdatePersonAsync(updatedPerson);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error updating person name: {personId}");
                return false;
            }
        }
    }
}