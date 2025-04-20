// ViewModels/GalleryImageViewModel.cs - ViewModel for individual gallery images
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ModernGallery.Models;

namespace ModernGallery.ViewModels
{
    public class GalleryImageViewModel : INotifyPropertyChanged
    {
        private readonly GalleryImage _image;
        
        public GalleryImage Image => _image;
        
        public int Id => _image.Id;
        public string FilePath => _image.FilePath;
        public string FileName => _image.FileName;
        public string DirectoryPath => _image.DirectoryPath;
        public long FileSize => _image.FileSize;
        public DateTime CreationDate => _image.CreationDate;
        public DateTime ModifiedDate => _image.ModifiedDate;
        public int Width => _image.Width;
        public int Height => _image.Height;
        public string ThumbnailPath => _image.ThumbnailPath;
        public string Description => _image.Description;
        public string Tags => _image.AiGeneratedTags;
        public bool ContainsPeople => _image.ContainsPeople;
        public int FaceCount => _image.FaceCount;
        public string Dimensions => $"{Width} × {Height}";
        public string FileSizeFormatted => FormatFileSize(FileSize);
        
        public string PeopleTags => string.Join(", ", 
            _image.Faces
                .Where(f => f.Person != null)
                .Select(f => f.Person.Name)
                .Distinct());
        
        public GalleryImageViewModel(GalleryImage image)
        {
            _image = image;
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}