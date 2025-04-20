// Tests/AIServiceTests.cs - Unit tests for AI service
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModernGallery.Models;
using ModernGallery.Services;

namespace ModernGallery.Tests
{
    [TestClass]
    public class AIServiceTests
    {
        private string _testImagePath;
        
        [TestInitialize]
        public void Initialize()
        {
            // Create a test image
            _testImagePath = Path.Combine(Path.GetTempPath(), "test_image.jpg");
            CreateTestImage(_testImagePath);
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test image
            if (File.Exists(_testImagePath))
            {
                File.Delete(_testImagePath);
            }
        }
        
        [TestMethod]
        [Ignore] // This test requires actual AI models, so it's disabled by default
        public async Task AnalyzeImageAsync_ShouldReturnAnalysisResults()
        {
            // Arrange
            var aiService = new LocalAIService();
            aiService.LoadModels();
            
            // Act
            var result = await aiService.AnalyzeImageAsync(_testImagePath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Description);
            Assert.IsTrue(result.Tags.Count > 0);
        }
        
        [TestMethod]
        [Ignore] // This test requires actual AI models, so it's disabled by default
        public async Task DetectFacesAsync_WithFaceImage_ShouldDetectFaces()
        {
            // Arrange
            var aiService = new LocalAIService();
            aiService.LoadModels();
            
            // Create a face test image
            var faceImagePath = Path.Combine(Path.GetTempPath(), "face_test.jpg");
            CreateFaceTestImage(faceImagePath);
            
            try
            {
                // Act
                var result = await aiService.DetectFacesAsync(faceImagePath);
                
                // Assert
                Assert.IsTrue(result.Count > 0);
            }
            finally
            {
                // Clean up
                if (File.Exists(faceImagePath))
                {
                    File.Delete(faceImagePath);
                }
            }
        }
        
        [TestMethod]
        [Ignore] // This test requires actual AI models, so it's disabled by default
        public async Task GenerateImageDescriptionAsync_ShouldReturnDescription()
        {
            // Arrange
            var aiService = new LocalAIService();
            aiService.LoadModels();
            
            // Act
            var result = await aiService.GenerateImageDescriptionAsync(_testImagePath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }
        
        private void CreateTestImage(string path)
        {
            // Create a simple test image
            using (var bitmap = new System.Drawing.Bitmap(100, 100))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.White);
                    
                    // Draw some shapes
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 5))
                    {
                        graphics.DrawRectangle(pen, 20, 20, 60, 60);
                    }
                }
                
                bitmap.Save(path);
            }
        }
        
        private void CreateFaceTestImage(string path)
        {
            // This method would create an image with a face
            // For testing purposes, we're just creating a circle that might be detected as a face
            using (var bitmap = new System.Drawing.Bitmap(200, 200))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.White);
                    
                    // Draw a circle for the face
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Bisque))
                    {
                        graphics.FillEllipse(brush, 50, 50, 100, 100);
                    }
                    
                    // Draw eyes
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                    {
                        graphics.FillEllipse(brush, 75, 80, 15, 15);
                        graphics.FillEllipse(brush, 110, 80, 15, 15);
                    }
                    
                    // Draw mouth
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 3))
                    {
                        graphics.DrawArc(pen, 70, 100, 60, 30, 0, 180);
                    }
                }
                
                bitmap.Save(path);
            }
        }
    }
}