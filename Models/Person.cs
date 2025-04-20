using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernGallery.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] AverageEmbedding { get; set; }
        
        // Navigation properties
        public virtual ICollection<ImageFace> Appearances { get; set; } = new List<ImageFace>();
    }
}