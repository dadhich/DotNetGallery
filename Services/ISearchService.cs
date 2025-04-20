// Services/ISearchService.cs - Interface for natural language search
using System.Collections.Generic;
using System.Threading.Tasks;
using ModernGallery.Models;

namespace ModernGallery.Services
{
    public class SearchResult
    {
        public GalleryImage Image { get; set; }
        public float Relevance { get; set; }
    }
    
    public interface ISearchService
    {
        Task<List<SearchResult>> SearchByNaturalLanguageAsync(string query);
        Task<List<SearchResult>> SearchByTagsAsync(List<string> tags);
        Task<List<SearchResult>> SearchByPeopleAsync(List<string> peopleNames);
        Task<List<SearchResult>> AdvancedSearchAsync(string query);
    }
}