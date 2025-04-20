using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernGallery.Models
{
    public class GalleryImage
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string DirectoryPath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ThumbnailPath { get; set; }
        public string Description { get; set; }
        public string AiGeneratedTags { get; set; }
        public bool ContainsPeople { get; set; }
        public int FaceCount { get; set; }
        
        // Navigation properties
        public virtual ICollection<ImageFace> Faces { get; set; } = new List<ImageFace>();
        public virtual ICollection<ImageTag> Tags { get; set; } = new List<ImageTag>();
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] AverageEmbedding { get; set; }
        
        // Navigation properties
        public virtual ICollection<ImageFace> Appearances { get; set; } = new List<ImageFace>();
    }

    public class ImageTag
    {
        public int Id { get; set; }
        public int ImageId { get; set; }
        public string TagName { get; set; }
        public float Confidence { get; set; }
        public Rectangle BoundingBox { get; set; } // For object detection tags
        
        // Navigation properties
        public virtual GalleryImage Image { get; set; }
    }
}