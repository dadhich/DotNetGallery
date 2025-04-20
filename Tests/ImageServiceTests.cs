// Tests/ImageServiceTests.cs - Unit tests for image service
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ModernGallery.Models;
using ModernGallery.Services;

namespace ModernGallery.Tests
{
    [TestClass]
    public class ImageServiceTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IAIService> _mockAiService;
        private IImageService _imageService;
        private string _testDirectory;
        
        [TestInitialize]
        public void Initialize()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockAiService = new Mock<IAIService>();
            
            _imageService = new ImageService(_mockDatabaseService.Object, _mockAiService.Object);
            
            // Create temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "ModernGalleryTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            // Create test images
            CreateTestImage(Path.Combine(_testDirectory, "test1.jpg"));
            CreateTestImage(Path.Combine(_testDirectory, "test2.png"));
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        
        [TestMethod]
        public async Task ScanDirectoryAsync_ShouldFindImages()
        {
            // Arrange
            
            // Act
            var result = await _imageService.ScanDirectoryAsync(_testDirectory, false);
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(Path.Combine(_testDirectory, "test1.jpg")));
            Assert.IsTrue(result.Contains(Path.Combine(_testDirectory, "test2.png")));
        }
        
        [TestMethod]
        public async Task ProcessImageAsync_ShouldProcessAndSaveImage()
        {
            // Arrange
            var imagePath = Path.Combine(_testDirectory, "test1.jpg");
            var aiResult = new AIImageAnalysisResult
            {
                Description = "Test description",
                Tags = new List<string> { "test", "image" },
                ContainsPeople = false,
                DetectedFaces = new List<DetectedFace>()
            };
            
            _mockAiService.Setup(m => m.AnalyzeImageAsync(imagePath))
                .ReturnsAsync(aiResult);
            
            _mockDatabaseService.Setup(m => m.AddImageAsync(It.IsAny<GalleryImage>()))
                .ReturnsAsync(1);
            
            // Act
            var result = await _imageService.ProcessImageAsync(imagePath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(imagePath, result.FilePath);
            Assert.AreEqual("test1.jpg", result.FileName);
            Assert.AreEqual("Test description", result.Description);
            Assert.AreEqual("test, image", result.AiGeneratedTags);
            Assert.IsFalse(result.ContainsPeople);
            
            _mockDatabaseService.Verify(m => m.AddImageAsync(It.IsAny<GalleryImage>()), Times.Once);
        }
        
        [TestMethod]
        public async Task GetImagesInDirectoryAsync_ShouldReturnImagesFromDatabase()
        {
            // Arrange
            var expectedImages = new List<GalleryImage>
            {
                new GalleryImage { Id = 1, FileName = "test1.jpg" },
                new GalleryImage { Id = 2, FileName = "test2.png" }
            };
            
            _mockDatabaseService.Setup(m => m.GetImagesByDirectoryAsync(_testDirectory))
                .ReturnsAsync(expectedImages);
            
            // Act
            var result = await _imageService.GetImagesInDirectoryAsync(_testDirectory);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("test1.jpg", result[0].FileName);
            Assert.AreEqual("test2.png", result[1].FileName);
            
            _mockDatabaseService.Verify(m => m.GetImagesByDirectoryAsync(_testDirectory), Times.Once);
        }
        
        private void CreateTestImage(string path)
        {
            // Create a simple test image
            using (var bitmap = new System.Drawing.Bitmap(100, 100))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.White);
                }
                
                bitmap.Save(path);
            }
        }
    }
}