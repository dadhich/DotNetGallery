// Helper/NaturalLanguageParser.cs - Parsing natural language queries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModernGallery.Helper
{
    public static class NaturalLanguageParser
    {
        private static readonly Regex PeopleQueryRegex = new Regex(
            @"(?:find|show|get)\s+(?:all\s+)?(?:images?|photos?|pictures?|pics?)\s+(?:with|containing|of)\s+([a-zA-Z\s,]+(?:\s+and\s+[a-zA-Z\s]+)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        private static readonly Regex ExclusionRegex = new Regex(
            @"(?:but\s+not|without|excluding)\s+([a-zA-Z\s,]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        private static readonly Regex ObjectQueryRegex = new Regex(
            @"(?:find|show|get)\s+(?:all\s+)?(?:images?|photos?|pictures?|pics?)\s+(?:with|containing|of)\s+(?:a|an)?\s+([a-zA-Z\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        public static (List<string> People, bool RequireAll, List<string> ExcludedPeople, List<string> Objects) ParseQuery(string query)
        {
            var people = new List<string>();
            var excludedPeople = new List<string>();
            var objects = new List<string>();
            var requireAll = false;
            
            // Check for people query
            var peopleMatch = PeopleQueryRegex.Match(query);
            if (peopleMatch.Success)
            {
                var peopleText = peopleMatch.Groups[1].Value;
                
                // Check if multiple people are required
                if (peopleText.Contains(" and ") || peopleText.Contains(","))
                {
                    requireAll = true;
                    
                    // Split by "and" or commas
                    if (peopleText.Contains(" and "))
                    {
                        people.AddRange(peopleText.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()));
                    }
                    else
                    {
                        people.AddRange(peopleText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()));
                    }
                }
                else
                {
                    people.Add(peopleText.Trim());
                }
            }
            
            // Check for exclusions
            var exclusionMatch = ExclusionRegex.Match(query);
            if (exclusionMatch.Success)
            {
                var excludedText = exclusionMatch.Groups[1].Value;
                
                // Split by "and" or commas if multiple exclusions
                if (excludedText.Contains(" and ") || excludedText.Contains(","))
                {
                    if (excludedText.Contains(" and "))
                    {
                        excludedPeople.AddRange(excludedText.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()));
                    }
                    else
                    {
                        excludedPeople.AddRange(excludedText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()));
                    }
                }
                else
                {
                    excludedPeople.Add(excludedText.Trim());
                }
            }
            
            // Check for object query if no people were found
            if (people.Count == 0)
            {
                var objectMatch = ObjectQueryRegex.Match(query);
                if (objectMatch.Success)
                {
                    var objectText = objectMatch.Groups[1].Value;
                    
                    // Clean up the object text
                    objectText = RemoveArticles(objectText);
                    
                    objects.Add(objectText.Trim());
                }
            }
            
            return (people, requireAll, excludedPeople, objects);
        }
        
        private static string RemoveArticles(string text)
        {
            var articles = new[] { " a ", " an ", " the " };
            
            foreach (var article in articles)
            {
                text = text.Replace(article, " ");
            }
            
            return text.Trim();
        }
    }
}