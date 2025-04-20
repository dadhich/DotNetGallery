using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernGallery.Models
{
    public class ImageFace
    {
        public int Id { get; set; }
        public int ImageId { get; set; }
        public int PersonId { get; set; }
        public Rectangle FaceRectangle { get; set; }
        public byte[] FaceEmbedding { get; set; }
        public float ConfidenceScore { get; set; }
        
        // Navigation properties
        public virtual GalleryImage Image { get; set; }
        public virtual Person Person { get; set; }
    }
}
