// ViewModels/MainViewModel.cs - ViewModel for the main gallery interface
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernGallery.Commands;
using ModernGallery.Models;
using ModernGallery.Services;
using ModernGallery.Views;
using Serilog;

namespace ModernGallery.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IImageService _imageService;
        private readonly IDatabaseService _databaseService;
        private readonly ISearchService _searchService;
        
        private ObservableCollection<GalleryImageViewModel> _images;
        private string _currentDirectory;
        private string _searchQuery;
        private bool _isScanning;
        private int _scanProgress;
        private int _totalImages;
        private GalleryImageViewModel _selectedImage;
        private string _statusMessage;
        
        public ObservableCollection<GalleryImageViewModel> Images
        {
            get => _images;
            set
            {
                _images = value;
                OnPropertyChanged(nameof(Images));
            }
        }
        
        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                _currentDirectory = value;
                OnPropertyChanged(nameof(CurrentDirectory));
            }
        }
        
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }
        
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged(nameof(IsScanning));
            }
        }
        
        public int ScanProgress
        {
            get => _scanProgress;
            set
            {
                _scanProgress = value;
                OnPropertyChanged(nameof(ScanProgress));
            }
        }
        
        public int TotalImages
        {
            get => _totalImages;
            set
            {
                _totalImages = value;
                OnPropertyChanged(nameof(TotalImages));
            }
        }
        
        public GalleryImageViewModel SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        
        // Commands
        public ICommand SelectFolderCommand { get; }
        public ICommand ScanFolderCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand OpenImageCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SortImagesCommand { get; }
        
        public MainViewModel(
            IImageService imageService,
            IDatabaseService databaseService,
            ISearchService searchService)
        {
            _imageService = imageService;
            _databaseService = databaseService;
            _searchService = searchService;
            
            Images = new ObservableCollection<GalleryImageViewModel>();
            
            // Initialize commands
            SelectFolderCommand = new RelayCommand(async (param) => await SelectFolderAsync());
            ScanFolderCommand = new RelayCommand(async (param) => await ScanFolderAsync(), (param) => !string.IsNullOrEmpty(CurrentDirectory));
            SearchCommand = new RelayCommand(async (param) => await SearchImagesAsync(), (param) => !string.IsNullOrEmpty(SearchQuery));
            OpenImageCommand = new RelayCommand(OpenImage, (param) => SelectedImage != null);
            RefreshCommand = new RelayCommand(async (param) => await RefreshImagesAsync());
            SortImagesCommand = new RelayCommand(SortImages);
            
            // Set default status message
            StatusMessage = "Ready. Please select a folder to scan or search for images.";
        }
        
        private async Task SelectFolderAsync()
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select a folder to scan for images",
                    ShowNewFolderButton = false
                };
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CurrentDirectory = dialog.SelectedPath;
                    await RefreshImagesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error selecting folder");
                StatusMessage = "Error selecting folder.";
            }
        }
        
        private async Task ScanFolderAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentDirectory))
                {
                    return;
                }
                
                IsScanning = true;
                ScanProgress = 0;
                StatusMessage = "Scanning folder for images...";
                
                // Get all image files in the directory
                var imageFiles = await _imageService.ScanDirectoryAsync(CurrentDirectory, true);
                TotalImages = imageFiles.Count;
                
                // Process each image file
                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var imagePath = imageFiles[i];
                    await _imageService.ProcessImageAsync(imagePath);
                    
                    ScanProgress = (i + 1) * 100 / TotalImages;
                    StatusMessage = $"Processing image {i + 1} of {TotalImages}: {Path.GetFileName(imagePath)}";
                }
                
                // Refresh the image list
                await RefreshImagesAsync();
                
                StatusMessage = $"Scan complete. Found {TotalImages} images.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scanning folder");
                StatusMessage = "Error scanning folder.";
            }
            finally
            {
                IsScanning = false;
            }
        }
        
        private async Task RefreshImagesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentDirectory))
                {
                    return;
                }
                
                StatusMessage = "Loading images...";
                
                // Get images from the database
                var images = await _imageService.GetImagesInDirectoryAsync(CurrentDirectory);
                
                // Convert to view models
                Images.Clear();
                foreach (var image in images)
                {
                    Images.Add(new GalleryImageViewModel(image));
                }
                
                StatusMessage = $"Loaded {Images.Count} images from {CurrentDirectory}.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error refreshing images");
                StatusMessage = "Error loading images.";
            }
        }
        
        private async Task SearchImagesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SearchQuery))
                {
                    return;
                }
                
                StatusMessage = $"Searching for '{SearchQuery}'...";
                
                // Perform natural language search
                var searchResults = await _searchService.SearchByNaturalLanguageAsync(SearchQuery);
                
                // Display results
                Images.Clear();
                foreach (var result in searchResults)
                {
                    Images.Add(new GalleryImageViewModel(result.Image));
                }
                
                StatusMessage = $"Found {Images.Count} images matching '{SearchQuery}'.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error searching for '{SearchQuery}'");
                StatusMessage = "Error performing search.";
            }
        }
        
        private void OpenImage(object param)
        {
            try
            {
                if (SelectedImage == null)
                {
                    return;
                }
                
                // Open the image viewer window
                var imageViewer = new ImageViewerWindow(SelectedImage.Image);
                imageViewer.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening image");
                StatusMessage = "Error opening image.";
            }
        }
        
        private void SortImages(object param)
        {
            try
            {
                if (Images.Count == 0)
                {
                    return;
                }
                
                var sortOption = param?.ToString() ?? "Name";
                
                switch (sortOption.ToLower())
                {
                    case "name":
                        Images = new ObservableCollection<GalleryImageViewModel>(
                            Images.OrderBy(i => i.FileName));
                        break;
                    case "date":
                        Images = new ObservableCollection<GalleryImageViewModel>(
                            Images.OrderByDescending(i => i.ModifiedDate));
                        break;
                    case "size":
                    Images = new ObservableCollection<GalleryImageViewModel>(
                        Images.OrderByDescending(i => i.FileSize));
                    break;
                case "dimension":
                    Images = new ObservableCollection<GalleryImageViewModel>(
                        Images.OrderByDescending(i => i.Width * i.Height));
                    break;
            }
            
            StatusMessage = $"Images sorted by {sortOption}.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error sorting images by {param}");
            StatusMessage = "Error sorting images.";
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}