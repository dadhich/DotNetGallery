// Deployment/ModelDownloader.cs - Utility for downloading AI models
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace ModernGallery.Deployment
{
    public class ModelDownloader
    {
        private readonly string _modelsDirectory;
        private readonly HttpClient _httpClient;
        
        public ModelDownloader(string modelsDirectory)
        {
            _modelsDirectory = modelsDirectory;
            _httpClient = new HttpClient();
        }
        
        public async Task<bool> DownloadRequiredModelsAsync(IProgress<(string, float)> progress = null)
        {
            try
            {
                // Ensure models directory exists
                Directory.CreateDirectory(_modelsDirectory);
                
                // Define models to download
                var models = new[]
                {
                    ("YOLOv8 Object Detection", "yolov8n.onnx", "https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.onnx"),
                    ("BlazeFace Detection", "blazeface.onnx", "https://github.com/PINTO0309/PINTO_model_zoo/raw/main/307_BlazeFace/blazeface_1.onnx"),
                    ("ArcFace Recognition", "arcface.onnx", "https://github.com/onnx/models/raw/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx"),
                    ("LLAMA 3 Language Model", "llama-3-8b-q4_0.gguf", "https://huggingface.co/TheBloke/Llama-3-8B-GGUF/resolve/main/llama-3-8b.Q4_0.gguf")
                };
                
                int totalModels = models.Length;
                int completedModels = 0;
                
                foreach (var (name, filename, url) in models)
                {
                    var filePath = Path.Combine(_modelsDirectory, filename);
                    
                    // Skip if already downloaded
                    if (File.Exists(filePath))
                    {
                        Log.Information($"Model {name} already exists, skipping download");
                        completedModels++;
                        progress?.Report((name, (float)completedModels / totalModels));
                        continue;
                    }
                    
                    Log.Information($"Downloading {name} from {url}");
                    
                    // Download with progress reporting
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long bytesRead = 0;
                        int read;
                        
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            bytesRead += read;
                            
                            if (totalBytes > 0)
                            {
                                float modelProgress = (float)bytesRead / totalBytes;
                                float overallProgress = ((float)completedModels + modelProgress) / totalModels;
                                progress?.Report((name, overallProgress));
                            }
                        }
                    }
                    
                    Log.Information($"Downloaded {name} to {filePath}");
                    completedModels++;
                    progress?.Report((name, (float)completedModels / totalModels));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading models");
                return false;
            }
        }
    }
}