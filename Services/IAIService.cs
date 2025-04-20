// Services/IAIService.cs - Interface for AI operations
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ModernGallery.Services
{
    public class AIImageAnalysisResult
    {
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool ContainsPeople { get; set; }
        public List<DetectedFace> DetectedFaces { get; set; } = new List<DetectedFace>();
    }
    
    public class DetectedFace
    {
        public Rectangle Rectangle { get; set; }
        public byte[] Embedding { get; set; }
        public float Confidence { get; set; }
    }
    
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
    
    public interface IAIService
    {
        void LoadModels();
        Task<AIImageAnalysisResult> AnalyzeImageAsync(string imagePath);
        Task<List<DetectedFace>> DetectFacesAsync(string imagePath);
        Task<string> GenerateImageDescriptionAsync(string imagePath);
        Task<string> GetChatResponseAsync(string imagePath, List<ChatMessage> conversation);
    }
}