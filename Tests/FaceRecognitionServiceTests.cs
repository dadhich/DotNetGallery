// Tests/FaceRecognitionServiceTests.cs - Unit tests for face recognition
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ModernGallery.Models;
using ModernGallery.Services;

namespace ModernGallery.Tests
{
    [TestClass]
    public class FaceRecognitionServiceTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private IFaceRecognitionService _faceRecognitionService;
        
        [TestInitialize]
        public void Initialize()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _faceRecognitionService = new FaceRecognitionService(_mockDatabaseService.Object);
        }
        
        [TestMethod]
        public async Task GetPersonForFaceAsync_WithMatchingFace_ShouldReturnPerson()
        {
            // Arrange
            var faceEmbedding = CreateRandomEmbedding();
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Person1", AverageEmbedding = CreateSimilarEmbedding(faceEmbedding, 0.95f) },
                new Person { Id = 2, Name = "Person2", AverageEmbedding = CreateRandomEmbedding() }
            };
            
            _mockDatabaseService.Setup(m => m.GetAllPeopleAsync())
                .ReturnsAsync(people);
            
            // Act
            var result = await _faceRecognitionService.GetPersonForFaceAsync(faceEmbedding, 0.6f);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("Person1", result.Name);
        }
        
        [TestMethod]
        public async Task GetPersonForFaceAsync_WithNoMatchingFace_ShouldReturnNull()
        {
            // Arrange
            var faceEmbedding = CreateRandomEmbedding();
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Person1", AverageEmbedding = CreateSimilarEmbedding(faceEmbedding, 0.3f) },
                new Person { Id = 2, Name = "Person2", AverageEmbedding = CreateRandomEmbedding() }
            };
            
            _mockDatabaseService.Setup(m => m.GetAllPeopleAsync())
                .ReturnsAsync(people);
            
            // Act
            var result = await _faceRecognitionService.GetPersonForFaceAsync(faceEmbedding, 0.6f);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public async Task CreatePersonAsync_ShouldCreatePersonInDatabase()
        {
            // Arrange
            var personName = "NewPerson";
            var embedding = CreateRandomEmbedding();
            
            _mockDatabaseService.Setup(m => m.AddPersonAsync(It.IsAny<Person>()))
                .ReturnsAsync(1);
            
            // Act
            var result = await _faceRecognitionService.CreatePersonAsync(personName, embedding);
            
            // Assert
            Assert.AreEqual(1, result);
            
            _mockDatabaseService.Verify(m => m.AddPersonAsync(It.Is<Person>(p => 
                p.Name == personName && p.AverageEmbedding == embedding)), Times.Once);
        }
        
        [TestMethod]
        public async Task FindImagesWithPeopleAsync_RequireAll_ShouldReturnImagesWithAllPeople()
        {
            // Arrange
            var personIds = new List<int> { 1, 2 };
            var imagesWithPerson1 = new List<GalleryImage>
            {
                new GalleryImage { Id = 1, FileName = "image1.jpg" },
                new GalleryImage { Id = 2, FileName = "image2.jpg" }
            };
            
            var imagesWithPerson2 = new List<GalleryImage>
            {
                new GalleryImage { Id = 2, FileName = "image2.jpg" },
                new GalleryImage { Id = 3, FileName = "image3.jpg" }
            };
            
            _mockDatabaseService.Setup(m => m.GetImagesByPersonAsync(1))
                .ReturnsAsync(imagesWithPerson1);
            
            _mockDatabaseService.Setup(m => m.GetImagesByPersonAsync(2))
                .ReturnsAsync(imagesWithPerson2);
            
            // Act
            var result = await _faceRecognitionService.FindImagesWithPeopleAsync(personIds, true);
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("image2.jpg", result[0].FileName);
        }
        
        [TestMethod]
        public async Task CalculateFaceSimilarityAsync_WithSimilarFaces_ShouldReturnHighSimilarity()
        {
            // Arrange
            var embedding1 = CreateRandomEmbedding();
            var embedding2 = CreateSimilarEmbedding(embedding1, 0.9f);
            
            // Act
            var result = await _faceRecognitionService.CalculateFaceSimilarityAsync(embedding1, embedding2);
            
            // Assert
            Assert.IsTrue(result > 0.8f);
        }
        
        private byte[] CreateRandomEmbedding()
        {
            // Create a random embedding vector
            var random = new Random();
            var embeddingSize = 128; // Typical face embedding size
            
            float[] embedding = new float[embeddingSize];
            for (int i = 0; i < embeddingSize; i++)
            {
                embedding[i] = (float)random.NextDouble();
            }
            
            // Normalize the embedding
            float sum = 0;
            for (int i = 0; i < embeddingSize; i++)
            {
                sum += embedding[i] * embedding[i];
            }
            
            float magnitude = (float)Math.Sqrt(sum);
            for (int i = 0; i < embeddingSize; i++)
            {
                embedding[i] /= magnitude;
            }
            
            // Convert to byte array
            byte[] bytes = new byte[embeddingSize * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            
            return bytes;
        }
        
        private byte[] CreateSimilarEmbedding(byte[] baseEmbedding, float similarity)
        {
            // Create an embedding similar to the base embedding by the given similarity factor
            var random = new Random();
            int embeddingSize = baseEmbedding.Length / sizeof(float);
            
            float[] original = new float[embeddingSize];
            Buffer.BlockCopy(baseEmbedding, 0, original, 0, baseEmbedding.Length);
            
            float[] modified = new float[embeddingSize];
            for (int i = 0; i < embeddingSize; i++)
            {
                // Mix original value with random noise based on similarity
                modified[i] = original[i] * similarity + (float)random.NextDouble() * (1 - similarity);
            }
            
            // Normalize the modified embedding
            float sum = 0;
            for (int i = 0; i < embeddingSize; i++)
            {
                sum += modified[i] * modified[i];
            }
            
            float magnitude = (float)Math.Sqrt(sum);
            for (int i = 0; i < embeddingSize; i++)
            {
                modified[i] /= magnitude;
            }
            
            // Convert to byte array
            byte[] bytes = new byte[embeddingSize * sizeof(float)];
            Buffer.BlockCopy(modified, 0, bytes, 0, bytes.Length);
            
            return bytes;
        }
    }
}