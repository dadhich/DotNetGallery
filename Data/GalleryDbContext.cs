// Data/GalleryDbContext.cs - Entity Framework DbContext for database operations
using Microsoft.EntityFrameworkCore;
using ModernGallery.Models;

namespace ModernGallery.Data
{
    public class GalleryDbContext : DbContext
    {
        private readonly string _connectionString;
        
        public DbSet<GalleryImage> Images { get; set; }
        public DbSet<ImageTag> ImageTags { get; set; }
        public DbSet<ImageFace> ImageFaces { get; set; }
        public DbSet<Person> People { get; set; }
        
        public GalleryDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure entity relationships
            
            // GalleryImage
            modelBuilder.Entity<GalleryImage>()
                .HasKey(i => i.Id);
            
            modelBuilder.Entity<GalleryImage>()
                .HasIndex(i => i.FilePath)
                .IsUnique();
            
            // ImageTag
            modelBuilder.Entity<ImageTag>()
                .HasKey(t => t.Id);
            
            modelBuilder.Entity<ImageTag>()
                .HasIndex(t => new { t.ImageId, t.TagName });
            
            modelBuilder.Entity<ImageTag>()
                .HasOne(t => t.Image)
                .WithMany(i => i.Tags)
                .HasForeignKey(t => t.ImageId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // ImageFace
            modelBuilder.Entity<ImageFace>()
                .HasKey(f => f.Id);
            
            modelBuilder.Entity<ImageFace>()
                .HasOne(f => f.Image)
                .WithMany(i => i.Faces)
                .HasForeignKey(f => f.ImageId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<ImageFace>()
                .HasOne(f => f.Person)
                .WithMany(p => p.Appearances)
                .HasForeignKey(f => f.PersonId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Configure value conversion for Rectangle
            modelBuilder.Entity<ImageFace>()
                .Property(f => f.FaceRectangle)
                .HasConversion(
                    rect => $"{rect.X},{rect.Y},{rect.Width},{rect.Height}",
                    str =>
                    {
                        var parts = str.Split(',');
                        return new System.Drawing.Rectangle(
                            int.Parse(parts[0]),
                            int.Parse(parts[1]),
                            int.Parse(parts[2]),
                            int.Parse(parts[3]));
                    });
            
            modelBuilder.Entity<ImageTag>()
                .Property(t => t.BoundingBox)
                .HasConversion(
                    rect => $"{rect.X},{rect.Y},{rect.Width},{rect.Height}",
                    str =>
                    {
                        var parts = str.Split(',');
                        return new System.Drawing.Rectangle(
                            int.Parse(parts[0]),
                            int.Parse(parts[1]),
                            int.Parse(parts[2]),
                            int.Parse(parts[3]));
                    });
        }
    }
}