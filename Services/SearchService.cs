// Services/SearchService.cs - Implementation of search functionality
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModernGallery.Models;
using Serilog;

namespace ModernGallery.Services
{
    public class SearchService : ISearchService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAIService _aiService;
        private readonly IFaceRecognitionService _faceRecognitionService;
        
        public SearchService(
            IDatabaseService databaseService,
            IAIService aiService,
            IFaceRecognitionService faceRecognitionService)
        {
            _databaseService = databaseService;
            _aiService = aiService;
            _faceRecognitionService = faceRecognitionService;
        }
        
        public async Task<List<SearchResult>> SearchByNaturalLanguageAsync(string query)
        {
            try
            {
                Log.Information($"Natural language search: {query}");
                
                // First, try to parse the query to identify specific patterns
                var parsedQuery = ParseNaturalLanguageQuery(query);
                
                // If the query is about finding people
                if (parsedQuery.ContainsPeople && parsedQuery.PeopleNames.Count > 0)
                {
                    return await SearchByPeopleAsync(parsedQuery.PeopleNames, parsedQuery.RequireAll, parsedQuery.ExcludedPeople);
                }
                
                // If the query is about finding objects/tags
                if (parsedQuery.ContainsTags && parsedQuery.Tags.Count > 0)
                {
                    return await SearchByTagsAsync(parsedQuery.Tags);
                }
                
                // If we couldn't parse the query specifically, use a more generic approach
                return await AdvancedSearchAsync(query);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in natural language search: {query}");
                return new List<SearchResult>();
            }
        }
        
        public async Task<List<SearchResult>> SearchByTagsAsync(List<string> tags)
        {
            try
            {
                Log.Information($"Tag search: {string.Join(", ", tags)}");
                
                var results = new List<SearchResult>();
                
                foreach (var tag in tags)
                {
                    var images = await _databaseService.GetImagesByTagAsync(tag);
                    
                    foreach (var image in images)
                    {
                        // Calculate relevance score based on confidence of tag detection
                        float relevance = image.Tags
                            .Where(t => t.TagName.ToLower() == tag.ToLower())
                            .Select(t => t.Confidence)
                            .FirstOrDefault();
                        
                        results.Add(new SearchResult
                        {
                            Image = image,
                            Relevance = relevance
                        });
                    }
                }
                
                // Remove duplicates, keeping the highest relevance score
                var uniqueResults = results
                    .GroupBy(r => r.Image.Id)
                    .Select(g => g.OrderByDescending(r => r.Relevance).First())
                    .OrderByDescending(r => r.Relevance)
                    .ToList();
                
                return uniqueResults;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in tag search: {string.Join(", ", tags)}");
                return new List<SearchResult>();
            }
        }
        
        public async Task<List<SearchResult>> SearchByPeopleAsync(List<string> peopleNames, bool requireAll = false, List<string> excludedPeople = null)
        {
            try
            {
                Log.Information($"People search: {string.Join(", ", peopleNames)}, Require all: {requireAll}");
                
                // Get all people from the database
                var allPeople = await _databaseService.GetAllPeopleAsync();
                
                // Find the person IDs that match the requested names
                var personIds = new List<int>();
                foreach (var name in peopleNames)
                {
                    var matchingPeople = allPeople
                        .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                        .ToList();
                    
                    personIds.AddRange(matchingPeople.Select(p => p.Id));
                }
                
                // Find excluded person IDs
                var excludedPersonIds = new List<int>();
                if (excludedPeople != null && excludedPeople.Count > 0)
                {
                    foreach (var name in excludedPeople)
                    {
                        var matchingPeople = allPeople
                            .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                            .ToList();
                        
                        excludedPersonIds.AddRange(matchingPeople.Select(p => p.Id));
                    }
                }
                
                // Find images with the specified people
                var images = await _faceRecognitionService.FindImagesWithPeopleAsync(personIds, requireAll);
                
                // Exclude images with excluded people
                if (excludedPersonIds.Count > 0)
                {
                    var imagesToExclude = new HashSet<int>();
                    
                    foreach (var personId in excludedPersonIds)
                    {
                        var excludedImages = await _faceRecognitionService.FindImagesWithPersonAsync(personId);
                        foreach (var image in excludedImages)
                        {
                            imagesToExclude.Add(image.Id);
                        }
                    }
                    
                    images = images.Where(i => !imagesToExclude.Contains(i.Id)).ToList();
                }
                
                // Convert to search results
                var results = images.Select(i => new SearchResult
                {
                    Image = i,
                    Relevance = 1.0f // Default relevance for people search
                }).ToList();
                
                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in people search: {string.Join(", ", peopleNames)}");
                return new List<SearchResult>();
            }
        }
        
        public async Task<List<SearchResult>> AdvancedSearchAsync(string query)
        {
            try
            {
                Log.Information($"Advanced search: {query}");
                
                // Simplistic keyword extraction from the query
                var keywords = ExtractKeywords(query);
                
                // Search for images by tags matching keywords
                var tagResults = await SearchByTagsAsync(keywords);
                
                // Search for images by people matching keywords
                var peopleResults = await SearchByPeopleAsync(keywords);
                
                // Combine results
                var allResults = tagResults.Concat(peopleResults).ToList();
                
                // Remove duplicates, keeping the highest relevance score
                var uniqueResults = allResults
                    .GroupBy(r => r.Image.Id)
                    .Select(g => g.OrderByDescending(r => r.Relevance).First())
                    .OrderByDescending(r => r.Relevance)
                    .ToList();
                
                return uniqueResults;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in advanced search: {query}");
                return new List<SearchResult>();
            }
        }
        
        private (bool ContainsPeople, List<string> PeopleNames, bool RequireAll, bool ContainsTags, List<string> Tags, List<string> ExcludedPeople) ParseNaturalLanguageQuery(string query)
        {
            var result = (
                ContainsPeople: false,
                PeopleNames: new List<string>(),
                RequireAll: false,
                ContainsTags: false,
                Tags: new List<string>(),
                ExcludedPeople: new List<string>()
            );
            
            // Normalize query
            query = query.ToLower().Trim();
            
            // Check for patterns related to people
            var peoplePatterns = new[]
            {
                @"find\s+all\s+images\s+with\s+([a-zA-Z\s]+)\s+in\s+it",
                @"find\s+pictures?\s+with\s+([a-zA-Z\s]+)\s+in\s+it",
                @"show\s+me\s+photos?\s+with\s+([a-zA-Z\s]+)\s+in\s+it",
                @"find\s+images?\s+where\s+([a-zA-Z\s]+)\s+(?:is|are)\s+",
                @"find\s+pics?\s+where\s+([a-zA-Z\s]+)\s+(?:is|are)\s+"
            };
            
            foreach (var pattern in peoplePatterns)
            {
                var match = Regex.Match(query, pattern);
                if (match.Success)
                {
                    result.ContainsPeople = true;
                    
                    // Extract people names
                    var peopleText = match.Groups[1].Value;
                    
                    // Check for "and" to determine if all people are required
                    if (peopleText.Contains(" and "))
                    {
                        result.RequireAll = true;
                        var names = peopleText.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
                        result.PeopleNames.AddRange(names.Select(n => n.Trim()));
                    }
                    else if (peopleText.Contains(","))
                    {
                        result.RequireAll = true;
                        var names = peopleText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        result.PeopleNames.AddRange(names.Select(n => n.Trim()));
                    }
                    else
                    {
                        result.PeopleNames.Add(peopleText.Trim());
                    }
                    
                    break;
                }
            }
            
            // Check for exclusion patterns
            var exclusionPatterns = new[]
            {
                @"but\s+not\s+with\s+([a-zA-Z\s]+)",
                @"but\s+not\s+([a-zA-Z\s]+)",
                @"without\s+([a-zA-Z\s]+)"
            };
            
            foreach (var pattern in exclusionPatterns)
            {
                var match = Regex.Match(query, pattern);
                if (match.Success)
                {
                    var excludedText = match.Groups[1].Value;
                    result.ExcludedPeople.Add(excludedText.Trim());
                }
            }
            
            // Check for patterns related to objects/tags
            var tagPatterns = new[]
            {
                @"find\s+all\s+images\s+with\s+(?:a|an)?\s+([a-zA-Z\s]+)\s+in\s+it",
                @"find\s+pictures?\s+with\s+(?:a|an)?\s+([a-zA-Z\s]+)\s+in\s+it",
                @"show\s+me\s+photos?\s+with\s+(?:a|an)?\s+([a-zA-Z\s]+)\s+in\s+it",
                @"search\s+for\s+([a-zA-Z\s]+)"
            };
            
            // Check if the query is about objects, not people
            if (!result.ContainsPeople)
            {
                foreach (var pattern in tagPatterns)
                {
                    var match = Regex.Match(query, pattern);
                    if (match.Success)
                    {
                        result.ContainsTags = true;
                        
                        // Extract tag
                        var tagText = match.Groups[1].Value;
                        
                        // Remove common articles and prepositions
                        var wordsToRemove = new[] { "a", "an", "the", "in", "on", "at", "with" };
                        foreach (var word in wordsToRemove)
                        {
                            tagText = tagText.Replace($" {word} ", " ");
                        }
                        
                        result.Tags.Add(tagText.Trim());
                        break;
                    }
                }
            }
            
            return result;
        }
        
        private List<string> ExtractKeywords(string query)
        {
            // Remove common words and punctuation
            var commonWords = new HashSet<string>
            {
                "find", "all", "images", "with", "in", "it", "where", "is", "are", "a", "an", "the",
                "picture", "pictures", "photo", "photos", "pic", "pics", "image", "images", "show", "me"
            };
            
            // Tokenize the query
            var tokens = query.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\"', '\'', '(', ')', '[', ']', '{', '}' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !commonWords.Contains(t) && t.Length > 1)
                .ToList();
            
            return tokens;
        }
    }
}