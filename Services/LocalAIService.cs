using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LLamaSharp;
using LLamaSharp.LLama;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Serilog;

namespace ModernGallery.Services
{
    public class LocalAIService : IAIService
    {
        private InferenceSession _objectDetectionModel;
        private InferenceSession _faceDetectionModel;
        private InferenceSession _faceRecognitionModel;
        private LLamaModel _llamaModel;
        private LLamaContext _llamaContext;
        private bool _modelsLoaded = false;
        private readonly string _modelsDirectory;
        private readonly HttpClient _httpClient;
        
        public LocalAIService()
        {
            _modelsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModernGallery",
                "Models"
            );
            Directory.CreateDirectory(_modelsDirectory);
            _httpClient = new HttpClient();
        }

        public void LoadModels()
        {
            try
            {
                Log.Information("Loading AI models...");
                
                // Load object detection model (YOLOv8)
                var objectDetectionPath = Path.Combine(_modelsDirectory, "yolov8n.onnx");
                EnsureModelExists(objectDetectionPath, "https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.onnx");
                _objectDetectionModel = new InferenceSession(objectDetectionPath);
                
                // Load face detection model (BlazeFace)
                var faceDetectionPath = Path.Combine(_modelsDirectory, "blazeface.onnx");
                EnsureModelExists(faceDetectionPath, "https://github.com/PINTO0309/PINTO_model_zoo/raw/main/307_BlazeFace/blazeface_1.onnx");
                _faceDetectionModel = new InferenceSession(faceDetectionPath);
                
                // Load face recognition model (ArcFace)
                var faceRecognitionPath = Path.Combine(_modelsDirectory, "arcface.onnx");
                EnsureModelExists(faceRecognitionPath, "https://github.com/onnx/models/raw/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx");
                _faceRecognitionModel = new InferenceSession(faceRecognitionPath);
                
                // Load LLM model (LLAMA)
                var llamaModelPath = Path.Combine(_modelsDirectory, "llama-3-8b-q4_0.gguf");
                EnsureModelExists(llamaModelPath, "https://huggingface.co/TheBloke/Llama-3-8B-GGUF/resolve/main/llama-3-8b.Q4_0.gguf");
                
                var parameters = new ModelParams(llamaModelPath)
                {
                    ContextSize = 2048,
                    Seed = 42,
                    GpuLayerCount = 20 // Use GPU acceleration if available
                };
                
                _llamaModel = new LLamaModel(parameters);
                _llamaContext = _llamaModel.CreateContext();
                
                _modelsLoaded = true;
                Log.Information("AI models loaded successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading AI models");
                throw;
            }
        }
        
        private void EnsureModelExists(string filePath, string downloadUrl)
        {
            if (File.Exists(filePath))
            {
                return;
            }
            
            Log.Information($"Downloading model from {downloadUrl}");
            
            try
            {
                var response = _httpClient.GetAsync(downloadUrl).Result;
                response.EnsureSuccessStatusCode();
                
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    response.Content.CopyToAsync(fileStream).Wait();
                }
                
                Log.Information($"Model downloaded to {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error downloading model from {downloadUrl}");
                throw;
            }
        }
        
        public async Task<AIImageAnalysisResult> AnalyzeImageAsync(string imagePath)
        {
            if (!_modelsLoaded)
            {
                throw new InvalidOperationException("AI models are not loaded");
            }
            
            var result = new AIImageAnalysisResult();
            
            try
            {
                // Detect objects
                var detectionResults = await DetectObjectsAsync(imagePath);
                result.Tags = detectionResults.Select(r => r.Label).Distinct().ToList();
                result.ContainsPeople = detectionResults.Any(r => r.Label == "person");
                
                // Detect faces if people are detected
                if (result.ContainsPeople)
                {
                    result.DetectedFaces = await DetectFacesAsync(imagePath);
                }
                
                // Generate description
                result.Description = await GenerateImageDescriptionAsync(imagePath);
                
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error analyzing image: {imagePath}");
                throw;
            }
        }
        
        private async Task<List<(string Label, float Confidence, Rectangle BoundingBox)>> DetectObjectsAsync(string imagePath)
        {
            var results = new List<(string Label, float Confidence, Rectangle BoundingBox)>();
            
            try
            {
                // Load and preprocess image
                using (var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath))
                {
                    // Resize image to model input size (640x640 for YOLOv8)
                    image.Mutate(x => x.Resize(640, 640));
                    
                    // Convert to tensor
                    var input = new DenseTensor<float>(new[] { 1, 3, 640, 640 });
                    
                    // Normalize pixel values
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = image[x, y];
                            input[0, 0, y, x] = pixel.R / 255.0f; // R
                            input[0, 1, y, x] = pixel.G / 255.0f; // G
                            input[0, 2, y, x] = pixel.B / 255.0f; // B
                        }
                    }
                    
                    // Run inference
                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("images", input)
                    };
                    
                    using (var outputs = _objectDetectionModel.Run(inputs))
                    {
                        // Process outputs (YOLO format)
                        var outputTensor = outputs.First().AsTensor<float>();
                        var originalWidth = System.Drawing.Image.FromFile(imagePath).Width;
                        var originalHeight = System.Drawing.Image.FromFile(imagePath).Height;
                        
                        // YOLO v8 output is 1x84x8400, where 84 = 4 (bbox) + 80 (class scores)
                        int numClasses = 80;
                        int numDetections = outputTensor.Dimensions[2]; // 8400 for YOLOv8n
                        
                        var detections = new List<(Rectangle bbox, float confidence, int classId)>();
                        
                        // Process each detection
                        for (int i = 0; i < numDetections; i++)
                        {
                            float[] scores = new float[numClasses];
                            for (int j = 0; j < numClasses; j++)
                            {
                                scores[j] = outputTensor[0, 4 + j, i];
                            }
                            
                            // Find the class with the highest score
                            int classId = Array.IndexOf(scores, scores.Max());
                            float confidence = scores[classId];
                            
                            // Apply threshold
                            if (confidence > 0.25f)
                            {
                                // Get bounding box
                                float x = outputTensor[0, 0, i];
                                float y = outputTensor[0, 1, i];
                                float width = outputTensor[0, 2, i];
                                float height = outputTensor[0, 3, i];
                                
                                // Convert to pixel coordinates
                                int xMin = (int)Math.Max(0, (x - width / 2) * originalWidth / 640);
                                int yMin = (int)Math.Max(0, (y - height / 2) * originalHeight / 640);
                                int xMax = (int)Math.Min(originalWidth, (x + width / 2) * originalWidth / 640);
                                int yMax = (int)Math.Min(originalHeight, (y + height / 2) * originalHeight / 640);
                                
                                var rect = new Rectangle(xMin, yMin, xMax - xMin, yMax - yMin);
                                detections.Add((rect, confidence, classId));
                            }
                        }
                        
                        // Apply non-maximum suppression
                        var finalDetections = NonMaximumSuppression(detections, 0.5f);
                        
                        // Map class IDs to labels
                        foreach (var detection in finalDetections)
                        {
                            results.Add((GetCocoLabel(detection.classId), detection.confidence, detection.bbox));
                        }
                    }
                }
                
                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error detecting objects in image: {imagePath}");
                return new List<(string, float, Rectangle)>();
            }
        }
        
        public async Task<List<DetectedFace>> DetectFacesAsync(string imagePath)
        {
            var faces = new List<DetectedFace>();
            
            try
            {
                // Load and preprocess image
                using (var originalImage = System.Drawing.Image.FromFile(imagePath))
                using (var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath))
                {
                    // BlazeFace input size is typically 128x128
                    image.Mutate(x => x.Resize(128, 128));
                    
                    // Convert to tensor
                    var input = new DenseTensor<float>(new[] { 1, 3, 128, 128 });
                    
                    // Normalize pixel values
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = image[x, y];
                            // BlazeFace expects [0,1] normalized inputs
                            input[0, 0, y, x] = pixel.R / 255.0f;
                            input[0, 1, y, x] = pixel.G / 255.0f;
                            input[0, 2, y, x] = pixel.B / 255.0f;
                        }
                    }
                    
                    // Run face detection
                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("input", input)
                    };
                    
                    using (var outputs = _faceDetectionModel.Run(inputs))
                    {
                        // Process detection results
                        var boxes = outputs.First(x => x.Name == "boxes").AsTensor<float>();
                        var scores = outputs.First(x => x.Name == "scores").AsTensor<float>();
                        
                        int numDetections = boxes.Dimensions[1];
                        var originalWidth = originalImage.Width;
                        var originalHeight = originalImage.Height;
                        
                        for (int i = 0; i < numDetections; i++)
                        {
                            float score = scores[0, i, 0];
                            
                            // Apply confidence threshold
                            if (score > 0.75f)
                            {
                                // BlazeFace outputs normalized coordinates [0,1]
                                float yMin = boxes[0, i, 0];
                                float xMin = boxes[0, i, 1];
                                float yMax = boxes[0, i, 2];
                                float xMax = boxes[0, i, 3];
                                
                                // Convert to pixel coordinates
                                int x = (int)(xMin * originalWidth);
                                int y = (int)(yMin * originalHeight);
                                int width = (int)((xMax - xMin) * originalWidth);
                                int height = (int)((yMax - yMin) * originalHeight);
                                
                                // Ensure valid rectangle
                                x = Math.Max(0, x);
                                y = Math.Max(0, y);
                                width = Math.Min(width, originalWidth - x);
                                height = Math.Min(height, originalHeight - y);
                                
                                // Extract face for recognition
                                var faceRectangle = new Rectangle(x, y, width, height);
                                var faceEmbedding = await ExtractFaceEmbeddingAsync(imagePath, faceRectangle);
                                
                                faces.Add(new DetectedFace
                                {
                                    Rectangle = faceRectangle,
                                    Embedding = faceEmbedding,
                                    Confidence = score
                                });
                            }
                        }
                    }
                }
                
                return faces;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error detecting faces in image: {imagePath}");
                return new List<DetectedFace>();
            }
        }
        
        private async Task<byte[]> ExtractFaceEmbeddingAsync(string imagePath, Rectangle faceRect)
        {
            try
            {
                // Load image and crop face
                using (var originalImage = System.Drawing.Image.FromFile(imagePath))
                using (var faceImage = new Bitmap(faceRect.Width, faceRect.Height))
                {
                    using (var g = Graphics.FromImage(faceImage))
                    {
                        g.DrawImage(originalImage, new Rectangle(0, 0, faceRect.Width, faceRect.Height),
                            faceRect, GraphicsUnit.Pixel);
                    }
                    
                    // Save to temporary file
                    var tempPath = Path.GetTempFileName();
                    faceImage.Save(tempPath);
                    
                    // Load with ImageSharp for preprocessing
                    using (var image = SixLabors.ImageSharp.Image.Load<Rgb24>(tempPath))
                    {
                        // ArcFace expects 112x112 RGB images
                        image.Mutate(x => x.Resize(112, 112));
                        
                        // Convert to tensor
                        var input = new DenseTensor<float>(new[] { 1, 3, 112, 112 });
                        
                        // Normalize according to ArcFace requirements
                        for (int y = 0; y < image.Height; y++)
                        {
                            for (int x = 0; x < image.Width; x++)
                            {
                                var pixel = image[x, y];
                                // ArcFace typically expects [0,1] normalized inputs with RGB channels
                                input[0, 0, y, x] = pixel.R / 255.0f;
                                input[0, 1, y, x] = pixel.G / 255.0f;
                                input[0, 2, y, x] = pixel.B / 255.0f;
                            }
                        }
                        
                        // Run face recognition model
                        var inputs = new List<NamedOnnxValue>
                        {
                            NamedOnnxValue.CreateFromTensor("data", input)
                        };
                        
                        using (var outputs = _faceRecognitionModel.Run(inputs))
                        {
                            // Extract face embedding
                            var embedding = outputs.First().AsTensor<float>();
                            
                            // Convert to byte array for storage
                            var embeddingSize = embedding.Length;
                            var embeddingBytes = new byte[embeddingSize * sizeof(float)];
                            Buffer.BlockCopy(embedding.ToArray(), 0, embeddingBytes, 0, embeddingBytes.Length);
                            
                            // Clean up
                            File.Delete(tempPath);
                            
                            return embeddingBytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting face embedding");
                return new byte[0];
            }
        }
        
        public async Task<string> GenerateImageDescriptionAsync(string imagePath)
        {
            try
            {
                // Get object detection results
                var detectedObjects = await DetectObjectsAsync(imagePath);
                
                // Create simple description based on detected objects
                if (detectedObjects.Count == 0)
                {
                    return "This image does not contain any recognisable objects.";
                }
                
                // Group by label and count occurrences
                var objectGroups = detectedObjects
                    .GroupBy(obj => obj.Label)
                    .Select(group => new { Label = group.Key, Count = group.Count() })
                    .OrderByDescending(group => group.Count)
                    .ToList();
                
                // Generate description
                var description = new System.Text.StringBuilder();
                description.Append("This image contains ");
                
                for (int i = 0; i < objectGroups.Count; i++)
                {
                    var group = objectGroups[i];
                    
                    if (i > 0)
                    {
                        if (i == objectGroups.Count - 1)
                        {
                            description.Append(" and ");
                        }
                        else
                        {
                            description.Append(", ");
                        }
                    }
                    
                    if (group.Count > 1)
                    {
                        description.Append($"{group.Count} {group.Label}s");
                    }
                    else
                    {
                        // Use correct article
                        var article = "a";
                        if ("aeiou".Contains(group.Label[0].ToString().ToLower()))
                        {
                            article = "an";
                        }
                        description.Append($"{article} {group.Label}");
                    }
                }
                
                description.Append(".");
                
                // Use LLM to enhance the description
                var enhancedDescription = await EnhanceDescriptionWithLLM(description.ToString(), detectedObjects);
                return enhancedDescription;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error generating image description: {imagePath}");
                return "Unable to generate description for this image.";
            }
        }
        
        private async Task<string> EnhanceDescriptionWithLLM(string basicDescription, List<(string Label, float Confidence, Rectangle BoundingBox)> objects)
        {
            try
            {
                if (!_modelsLoaded)
                {
                    return basicDescription;
                }
                
                // Prepare prompt for LLM
                var objectsList = string.Join(", ", objects.Select(o => o.Label).Distinct());
                
                var prompt = $@"You are an AI assistant that creates detailed image descriptions based on detected objects. Use UK English spelling.

Objects detected in this image: {objectsList}

Basic description: {basicDescription}

Please enhance this description to make it more detailed and natural sounding. Include possible relationships between objects, their positioning, and context. Keep it concise but informative:";
                
                // Generate response
                var dialogueContext = _llamaContext.CreateChatContext();
                dialogueContext.Message = new LLamaSharp.ChatMessages.ChatHistory();
                dialogueContext.Message.AddSystemMessage("You are a helpful assistant that creates detailed image descriptions. Use UK English spelling.");
                dialogueContext.Message.AddUserMessage(prompt);
                
                var response = dialogueContext.Chat(new LLamaSharp.ChatCompletionOptions
                {
                    MaxTokens = 512,
                    Temperature = 0.7f,
                    TopP = 0.95f
                });
                
                // Extract and clean response
                string enhancedDescription = response.Message;
                
                // Remove any quotation marks that might be in the response
                enhancedDescription = enhancedDescription.Replace("\"", "").Trim();
                
                // Ensure the description isn't too long
                if (enhancedDescription.Length > 500)
                {
                    enhancedDescription = enhancedDescription.Substring(0, 497) + "...";
                }
                
                return enhancedDescription;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error enhancing description with LLM");
                return basicDescription;
            }
        }
        
        public async Task<string> GetChatResponseAsync(string imagePath, List<ChatMessage> conversation)
        {
            try
            {
                if (!_modelsLoaded)
                {
                    return "Sorry, the chat model is not loaded properly.";
                }
                
                // Get object detection results for context
                var detectedObjects = await DetectObjectsAsync(imagePath);
                var objectsList = string.Join(", ", detectedObjects.Select(o => o.Label).Distinct());
                
                // Get face detection results if any
                var detectedFaces = await DetectFacesAsync(imagePath);
                var hasFaces = detectedFaces.Count > 0;
                
                // Create prompt with image context and conversation history
                var dialogueContext = _llamaContext.CreateChatContext();
                dialogueContext.Message = new LLamaSharp.ChatMessages.ChatHistory();
                
                // Add system message
                var systemPrompt = $@"You are an AI assistant that can answer questions about an image. 
Use UK English spelling in all your responses.
The image contains the following objects: {objectsList}.
{(hasFaces ? $"The image contains {detectedFaces.Count} human face(s)." : "The image does not contain any human faces.")}
Answer questions based on this information. If you don't know the answer, say so honestly.";
                
                dialogueContext.Message.AddSystemMessage(systemPrompt);
                
                // Add conversation history
                foreach (var message in conversation)
                {
                    if (message.Role.ToLower() == "user")
                    {
                        dialogueContext.Message.AddUserMessage(message.Content);
                    }
                    else if (message.Role.ToLower() == "assistant")
                    {
                        dialogueContext.Message.AddAssistantMessage(message.Content);
                    }
                }
                
                // Generate response
                var response = dialogueContext.Chat(new LLamaSharp.ChatCompletionOptions
                {
                    MaxTokens = 512,
                    Temperature = 0.7f,
                    TopP = 0.95f
                });
                
                return response.Message.Trim();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating chat response");
                return "I apologise, but I'm having trouble processing your question. Please try again.";
            }
        }
        
        private List<(Rectangle bbox, float confidence, int classId)> NonMaximumSuppression(
            List<(Rectangle bbox, float confidence, int classId)> boxes, float iouThreshold)
        {
            var result = new List<(Rectangle bbox, float confidence, int classId)>();
            
            // Group by class
            var groups = boxes.GroupBy(x => x.classId).ToList();
            
            foreach (var group in groups)
            {
                var candidates = group.OrderByDescending(x => x.confidence).ToList();
                
                while (candidates.Count > 0)
                {
                    // Take the box with highest confidence
                    var current = candidates[0];
                    result.Add(current);
                    candidates.RemoveAt(0);
                    
                    // Remove boxes with high IoU
                    candidates = candidates.Where(box => CalculateIoU(current.bbox, box.bbox) < iouThreshold).ToList();
                }
            }
            
            return result;
        }
        
        private float CalculateIoU(Rectangle box1, Rectangle box2)
        {
            // Calculate intersection area
            int intersectionX = Math.Max(box1.Left, box2.Left);
            int intersectionY = Math.Max(box1.Top, box2.Top);
            int intersectionWidth = Math.Min(box1.Right, box2.Right) - intersectionX;
            int intersectionHeight = Math.Min(box1.Bottom, box2.Bottom) - intersectionY;
            
            if (intersectionWidth <= 0 || intersectionHeight <= 0)
                return 0.0f;
            
            float intersectionArea = intersectionWidth * intersectionHeight;
            
            // Calculate union area
            float box1Area = box1.Width * box1.Height;
            float box2Area = box2.Width * box2.Height;
            float unionArea = box1Area + box2Area - intersectionArea;
            
            return intersectionArea / unionArea;
        }
        
        private string GetCocoLabel(int classId)
        {
            // COCO dataset labels
            string[] cocoLabels = new string[]
            {
                "person", "bicycle", "car", "motorcycle", "aeroplane", "bus", "train", "truck", "boat",
                "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
                "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
                "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
                "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket",
                "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
                "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake",
                "chair", "sofa", "potted plant", "bed", "dining table", "toilet", "tv", "laptop",
                "mouse", "remote", "keyboard", "mobile phone", "microwave", "oven", "toaster", "sink",
                "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
            };
            
            if (classId >= 0 && classId < cocoLabels.Length)
            {
                return cocoLabels[classId];
            }
            
            return "unknown";
        }
    }
}