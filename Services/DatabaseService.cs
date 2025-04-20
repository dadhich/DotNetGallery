// Services/DatabaseService.cs - Implementation of database service
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernGallery.Data;
using ModernGallery.Models;
using Serilog;

namespace ModernGallery.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        
        public DatabaseService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernGallery");
            Directory.CreateDirectory(appDataPath);
            _connectionString = $"Data Source={Path.Combine(appDataPath, "gallery.db")}";
        }
        
        public void InitializeDatabase()
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                context.Database.Migrate();
                Log.Information("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize database");
                throw;
            }
        }
        
        public async Task<int> AddImageAsync(GalleryImage image)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                context.Images.Add(image);
                await context.SaveChangesAsync();
                return image.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add image to database");
                throw;
            }
        }
        
        public async Task<bool> UpdateImageAsync(GalleryImage image)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                context.Images.Update(image);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update image in database");
                return false;
            }
        }
        
        public async Task<GalleryImage> GetImageByIdAsync(int id)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Include(i => i.Faces)
                        .ThenInclude(f => f.Person)
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get image by ID");
                return null;
            }
        }
        
        public async Task<GalleryImage> GetImageByPathAsync(string path)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Include(i => i.Faces)
                        .ThenInclude(f => f.Person)
                    .FirstOrDefaultAsync(i => i.FilePath == path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get image by path");
                return null;
            }
        }
        
        public async Task<List<GalleryImage>> GetImagesAsync(int skip = 0, int take = 100)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get images");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<GalleryImage>> GetImagesByDirectoryAsync(string directory)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Where(i => i.DirectoryPath == directory)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get images by directory");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<GalleryImage>> GetImagesByTagAsync(string tag)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Where(i => i.Tags.Any(t => t.TagName == tag))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get images by tag");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<GalleryImage>> GetImagesByPersonAsync(int personId)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.Images
                    .Include(i => i.Tags)
                    .Include(i => i.Faces)
                        .ThenInclude(f => f.Person)
                    .Where(i => i.Faces.Any(f => f.PersonId == personId))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get images by person");
                return new List<GalleryImage>();
            }
        }
        
        public async Task<List<Person>> GetAllPeopleAsync()
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.People.ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get all people");
                return new List<Person>();
            }
        }
        
        public async Task<Person> GetPersonByNameAsync(string name)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                return await context.People
                    .FirstOrDefaultAsync(p => p.Name == name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get person by name");
                return null;
            }
        }
        
        public async Task<int> AddPersonAsync(Person person)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                context.People.Add(person);
                await context.SaveChangesAsync();
                return person.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add person");
                throw;
            }
        }
        
        public async Task<bool> AssociateFaceWithPersonAsync(int faceId, int personId)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                var face = await context.ImageFaces.FindAsync(faceId);
                if (face == null) return false;
                
                face.PersonId = personId;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to associate face with person");
                return false;
            }
        }
        
        public async Task<List<GalleryImage>> SearchImagesAsync(string searchTerm)
        {
            try
            {
                using var context = new GalleryDbContext(_connectionString);
                // Basic search implementation - will be enhanced by the SearchService
                return await context.Images
                    .Include(i => i.Tags)
                    .Include(i => i.Faces)
                        .ThenInclude(f => f.Person)
                    .Where(i => 
                        i.FileName.Contains(searchTerm) || 
                        i.Description.Contains(searchTerm) || 
                        i.AiGeneratedTags.Contains(searchTerm) ||
                        i.Tags.Any(t => t.TagName.Contains(searchTerm)) ||
                        i.Faces.Any(f => f.Person.Name.Contains(searchTerm)))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to search images");
                return new List<GalleryImage>();
            }
        }
    }
}