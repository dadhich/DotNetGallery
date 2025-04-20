using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernGallery.Models
{
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