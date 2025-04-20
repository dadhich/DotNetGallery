# Windows Photo Gallery

A powerful gallery application for Windows desktop with local AI capabilities for image analysis, face recognition, and natural language search.

## Features

- **Local AI Processing**: All AI operations run locally on your device, ensuring privacy and security.
- **Image Organization**: Scan folders for images and display them in a customizable grid view.
- **Object Recognition**: Automatically identify objects in images using advanced computer vision.
- **Face Detection & Recognition**: Detect faces in images and recognize people across your photo collection.
- **Natural Language Search**: Find images using natural language queries like "find all images with Samantha in it" or "find all pictures with a horse in it".
- **Interactive Viewing**: View images with zoom functionality and an AI-powered chat panel for asking questions about the image.

## System Requirements

- Windows 10 or higher
- .NET 8.0 Runtime
- 8GB RAM minimum (16GB recommended)
- 4GB free disk space for application and AI models
- GPU acceleration supported but not required

## Installation

1. Download the installer from the releases page.
2. Run the installer and follow the on-screen instructions.
3. On first launch, the application will download required AI models (approximately 2GB).

## Quick Start

1. Launch the application
2. Click "Select Folder" to choose a directory containing images
3. Click "Scan" to process the images with AI
4. Browse your images in the gallery view
5. Double-click any image to view it in detail with AI descriptions
6. Use the search bar for natural language queries

## Security Features

- All processing happens locally on your device
- No data is sent to cloud services
- Password protection using industry-standard hashing
- Optional encryption for sensitive metadata

## Development

### Building from Source

```
git clone https://github.com/username/modern-gallery.git
cd modern-gallery
dotnet restore
dotnet build
```

### Running Tests

```
dotnet test
```

## Core Principles

- **Modularity**: Structure the application into distinct, loosely coupled modules (UI, Core Logic, Data Access, AI Services, Infrastructure) to promote maintainability, testability, and independent development.
- **Testability**: Integrate automated testing (unit, integration) from the outset as a fundamental part of the development workflow.
- **Security**: Embed security best practices throughout the design and implementation (secure file handling, input validation, dependency management, secure logging).
- **Local-First AI**: All AI processing (inference) must occur entirely on the user's machine. No reliance on cloud-based AI APIs or services is permitted.

## Core Components

### 1. User Interface Layer
- **Main Window**: Grid-view gallery display with sorting options
- **Image Viewer**: Full-size image display with zoom functionality
- **Chat Panel**: AI-powered interface for image descriptions and Q&A
- **Search Interface**: Natural language search capabilities
- **Settings Panel**: Application configuration

### 2. Application Layer
- **Image Manager**: Scanning, indexing, and organizing images
- **AI Service Orchestrator**: Manages all AI operations locally
- **Search Service**: Processes natural language queries
- **Face Recognition Service**: Identifies, clusters, and manages face data
- **Database Service**: Handles storage and retrieval operations

### 3. Data Layer
- **Image Database**: Stores image metadata, paths, and AI-generated attributes
- **Face Database**: Stores facial vectors, clusters, and user-assigned names
- **User Settings**: Stores user preferences and application settings

## Technical Stack

### Frontend
- **Framework**: WPF (Windows Presentation Foundation) with MVVM architecture
- **UI Components**: Custom gallery controls, zoom-enabled image viewer
- **Styling**: Modern Windows UI design with customizable themes

### Backend
- **Language**: C# (.NET 8.0)
- **Database**: SQLite for local storage (no cloud dependencies)
- **Image Processing**: System.Drawing.Common and ImageSharp
- **AI Models**: 
  - ONNX Runtime for running pre-trained models locally
  - Local transformers for natural language processing
  - Local computer vision models for image understanding

### AI Components
- **Image Classification**: YOLOv8 or EfficientNet (converted to ONNX format)
- **Face Detection**: BlazeFace or RetinaFace
- **Face Recognition**: ArcFace or FaceNet
- **Natural Language Processing**: DistilBERT or similar compact models
- **Chat Interface**: Small-footprint LLM like LLAMA 3 (8B) or Phi-3

## Data Flow Architecture

1. **Image Processing Pipeline**:
   ```
   Scan Files → Extract Metadata → Generate Thumbnails → AI Classification → Store in Database
   ```

2. **Face Recognition Pipeline**:
   ```
   Detect Faces → Extract Face Embeddings → Cluster Similar Faces → Allow User Tagging → Update Face Database
   ```

3. **Search Pipeline**:
   ```
   Natural Language Query → Parse Intent and Entities → Search Database → Rank Results → Display Matching Images
   ```

4. **Chat Interaction Pipeline**:
   ```
   Load Image → Generate Description → Process User Questions → Extract Relevant Image Features → Generate Responses
   ```

## Security Considerations
- Local processing eliminates external API risks
- Proper error handling throughout the application
- No cloud services used, preserving privacy
- All data stored locally with optional encryption
- Sanitization of user inputs for search queries

## Performance Optimizations
- Asynchronous loading of thumbnails and full-size images
- Background processing for AI operations
- Efficient caching of frequently accessed images
- Lazy loading of AI models based on active features
- Multi-threading for processing images in batches

## Testing Strategy
- Unit tests for core functionality
- Integration tests for component interactions
- UI automation tests for user flows
- Performance benchmarks for AI operations
- Security tests for input validation

## Extensibility
- Plugin architecture for adding new AI models
- Customizable search queries and filters
- Export/import capabilities for user data
- Theming support for UI customization


## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- YOLO for object detection
- BlazeFace for face detection
- ArcFace for face recognition
- LLAMA for natural language processing