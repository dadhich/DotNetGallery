// Tests/SearchServiceTests.cs - Unit tests for search service
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ModernGallery.Models;
using ModernGallery.Services;

namespace ModernGallery.Tests
{
    [TestClass]
    public class SearchServiceTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IAIService> _mockAiService;
        private Mock<IFaceRecognitionService> _mockFaceRecognitionService;
        private ISearchService _searchService;
        
        [TestInitialize]
        public void Initialize()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockAiService = new Mock<IAIService>();
            _mockFaceRecognitionService = new Mock<IFaceRecognitionService>();
            
            _searchService = new SearchService(
                _mockDatabaseService.Object,
                _mockAiService.Object,
                _mockFaceRecognitionService.Object);
        }
        
        [TestMethod]
        public async Task SearchByNaturalLanguageAsync_WithPeopleQuery_ShouldSearchForPeople()
        {
            // Arrange
            var query = "find all images with Samantha in it";
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Samantha" },
                new Person { Id = 2, Name = "Tina" }
            };
            
            var images = new List<GalleryImage>
            {
                new GalleryImage { Id = 1, FileName = "image1.jpg" }
            };
            
            _mockDatabaseService.Setup(m => m.GetAllPeopleAsync())
                .ReturnsAsync(people);
            
            _mockFaceRecognitionService.Setup(m => m.FindImagesWithPeopleAsync(It.IsAny<List<int>>(), false))
                .ReturnsAsync(images);
            
            // Act
            var result = await _searchService.SearchByNaturalLanguageAsync(query);
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("image1.jpg", result[0].Image.FileName);
            
            _mockFaceRecognitionService.Verify(m => m.FindImagesWithPeopleAsync(It.IsAny<List<int>>(), false), Times.Once);
        }
        
        [TestMethod]
        public async Task SearchByNaturalLanguageAsync_WithObjectQuery_ShouldSearchForTags()
        {
            // Arrange
            var query = "find all images with a horse in it";
            var images = new List<GalleryImage>
            {
                new GalleryImage
                {
                    Id = 1,
                    FileName = "horse.jpg",
                    Tags = new List<ImageTag>
                    {
                        new ImageTag { TagName = "horse", Confidence = 0.9f }
                    }
                }
            };
            
            _mockDatabaseService.Setup(m => m.GetImagesByTagAsync("horse"))
                .ReturnsAsync(images);
            
            // Act
            var result = await _searchService.SearchByNaturalLanguageAsync(query);
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("horse.jpg", result[0].Image.FileName);
            
            _mockDatabaseService.Verify(m => m.GetImagesByTagAsync("horse"), Times.Once);
        }
        
        [TestMethod]
        public async Task SearchByNaturalLanguageAsync_WithComplexQuery_ShouldHandleMultiplePeople()
        {
            // Arrange
            var query = "find all images where Samantha and Tina are together";
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Samantha" },
                new Person { Id = 2, Name = "Tina" }
            };
            
            var images = new List<GalleryImage>
            {
                new GalleryImage { Id = 1, FileName = "together.jpg" }
            };
            
            _mockDatabaseService.Setup(m => m.GetAllPeopleAsync())
                .ReturnsAsync(people);
            
            _mockFaceRecognitionService.Setup(m => m.FindImagesWithPeopleAsync(It.IsAny<List<int>>(), true))
                .ReturnsAsync(images);
            
            // Act
            var result = await _searchService.SearchByNaturalLanguageAsync(query);
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("together.jpg", result[0].Image.FileName);
            
            _mockFaceRecognitionService.Verify(m => m.FindImagesWithPeopleAsync(It.IsAny<List<int>>(), true), Times.Once);
        }
        
        [TestMethod]
        public async Task SearchByNaturalLanguageAsync_WithExclusionQuery_ShouldExcludePerson()
        {
            // Arrange
            var query = "find all pics where Tina is with Dina but not with Ramesh";
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Tina" },
                new Person { Id = 2, Name = "Dina" },
                new Person { Id = 3, Name = "Ramesh" }
            };
            
            var imagesWithTinaDina = new List<GalleryImage>
            {
                new GalleryImage { Id = 1, FileName = "tina_dina.jpg" },
                new GalleryImage { Id = 2, FileName = "tina_dina_ramesh.jpg" }
            };
            
            var imagesWithRamesh = new List<GalleryImage>
            {
                new GalleryImage { Id = 2, FileName = "tina_dina_ramesh.jpg" }
            };
            
            _mockDatabaseService.Setup(m => m.GetAllPeopleAsync())
                .ReturnsAsync(people);
            
            _mockFaceRecognitionService.Setup(m => m.FindImagesWithPeopleAsync(It.Is<List<int>>(l => l.Contains(1) && l.Contains(2)), true))
                .ReturnsAsync(imagesWithTinaDina);
            
            _mockFaceRecognitionService.Setup(m => m.FindImagesWithPersonAsync(3))
                .ReturnsAsync(imagesWithRamesh);
            
            // Act
            var result = await _searchService.SearchByNaturalLanguageAsync(query);
            
            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("tina_dina.jpg", result[0].Image.FileName);
        }
    }
}