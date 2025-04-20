// ViewModels/ImageViewerViewModel.cs - ViewModel for the image viewer
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernGallery.Commands;
using ModernGallery.Models;
using ModernGallery.Services;
using Serilog;

namespace ModernGallery.ViewModels
{
    public class ChatMessageViewModel
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ImageViewerViewModel : INotifyPropertyChanged
    {
        private readonly GalleryImage _image;
        private readonly IAIService _aiService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        
        private double _zoomLevel;
        private int _chatPanelWidth;
        private string _chatInput;
        private bool _isProcessing;
        private ObservableCollection<ChatMessageViewModel> _chatMessages;
        private bool _showFaceRectangles;
        private bool _showObjectBoundingBoxes;
        
        public GalleryImage Image => _image;
        
        public string ImagePath => _image.FilePath;
        public string ImageTitle => _image.FileName;
        public string Description => _image.Description;
        
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = value;
                OnPropertyChanged(nameof(ZoomLevel));
            }
        }
        
        public int ChatPanelWidth
        {
            get => _chatPanelWidth;
            set
            {
                _chatPanelWidth = value;
                OnPropertyChanged(nameof(ChatPanelWidth));
            }
        }
        
        public string ChatInput
        {
            get => _chatInput;
            set
            {
                _chatInput = value;
                OnPropertyChanged(nameof(ChatInput));
            }
        }
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }
        
        public ObservableCollection<ChatMessageViewModel> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged(nameof(ChatMessages));
            }
        }
        
        public bool ShowFaceRectangles
        {
            get => _showFaceRectangles;
            set
            {
                _showFaceRectangles = value;
                OnPropertyChanged(nameof(ShowFaceRectangles));
            }
        }
        
        public bool ShowObjectBoundingBoxes
        {
            get => _showObjectBoundingBoxes;
            set
            {
                _showObjectBoundingBoxes = value;
                OnPropertyChanged(nameof(ShowObjectBoundingBoxes));
            }
        }
        
        // Commands
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand SendChatMessageCommand { get; }
        public ICommand TagFaceCommand { get; }
        public ICommand ToggleFaceRectanglesCommand { get; }
        public ICommand ToggleObjectBoxesCommand { get; }
        
        public ImageViewerViewModel(
            GalleryImage image,
            IAIService aiService,
            IFaceRecognitionService faceRecognitionService)
        {
            _image = image;
            _aiService = aiService;
            _faceRecognitionService = faceRecognitionService;
            
            // Initialize properties
            ZoomLevel = 1.0;
            ChatPanelWidth = 300;
            ChatInput = string.Empty;
            ChatMessages = new ObservableCollection<ChatMessageViewModel>();
            
            // Initialize commands
            ZoomInCommand = new RelayCommand(param => ZoomIn());
            ZoomOutCommand = new RelayCommand(param => ZoomOut());
            ResetZoomCommand = new RelayCommand(param => ResetZoom());
            SendChatMessageCommand = new RelayCommand(async param => await SendChatMessageAsync(), param => !string.IsNullOrWhiteSpace(ChatInput));
            TagFaceCommand = new RelayCommand<object>(TagFace);
            ToggleFaceRectanglesCommand = new RelayCommand(param => ShowFaceRectangles = !ShowFaceRectangles);
            ToggleObjectBoxesCommand = new RelayCommand(param => ShowObjectBoundingBoxes = !ShowObjectBoundingBoxes);
            
            // Add initial description message
            if (!string.IsNullOrEmpty(image.Description))
            {
                ChatMessages.Add(new ChatMessageViewModel
                {
                    Role = "assistant",
                    Content = image.Description,
                    Timestamp = DateTime.Now
                });
            }
        }
        
        private void ZoomIn()
        {
            ZoomLevel = Math.Min(ZoomLevel * 1.2, 5.0);
        }
        
        private void ZoomOut()
        {
            ZoomLevel = Math.Max(ZoomLevel / 1.2, 0.1);
        }
        
        private void ResetZoom()
        {
            ZoomLevel = 1.0;
        }
        
        private async Task SendChatMessageAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ChatInput))
                {
                    return;
                }
                
                IsProcessing = true;
                
                // Add user message to chat
                var userMessage = new ChatMessageViewModel
                {
                    Role = "user",
                    Content = ChatInput,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(userMessage);
                
                // Clear input field
                var inputText = ChatInput;
                ChatInput = string.Empty;
                
                // Convert chat history to format expected by AIService
                var chatHistory = ChatMessages
                    .Select(m => new ChatMessage
                    {
                        Role = m.Role,
                        Content = m.Content
                    })
                    .ToList();
                
                // Get AI response
                var response = await _aiService.GetChatResponseAsync(_image.FilePath, chatHistory);
                
                // Add AI response to chat
                var assistantMessage = new ChatMessageViewModel
                {
                    Role = "assistant",
                    Content = response,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(assistantMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending chat message");
                
                // Add error message to chat
                ChatMessages.Add(new ChatMessageViewModel
                {
                    Role = "assistant",
                    Content = "I apologise, but I'm having trouble processing your question. Please try again.",
                    Timestamp = DateTime.Now
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void TagFace(object param)
        {
            // Implement face tagging logic
            // This would open a dialog to select or create a person
            // and associate the face with that person
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}