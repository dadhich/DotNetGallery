// App.xaml.cs - Main application entry point
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ModernGallery.Services;
using ModernGallery.ViewModels;
using ModernGallery.Views;
using Serilog;

namespace ModernGallery
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            // Configure Serilog for logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/gallery-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Register core services
            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IAIService, LocalAIService>();
            services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
            services.AddSingleton<ISearchService, SearchService>();
            
            // Register view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<ImageViewerViewModel>();
            services.AddTransient<SearchViewModel>();
            services.AddTransient<SettingsViewModel>();
            
            // Register views
            services.AddTransient<MainWindow>();
            services.AddTransient<ImageViewerWindow>();
            services.AddTransient<SettingsWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                Log.Information("Starting ModernGallery application");
                
                // Initialize database and services
                var dbService = _serviceProvider.GetRequiredService<IDatabaseService>();
                dbService.InitializeDatabase();
                
                var aiService = _serviceProvider.GetRequiredService<IAIService>();
                aiService.LoadModels();
                
                // Show main window
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application startup failed");
                MessageBox.Show($"An error occurred during startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Shutting down ModernGallery application");
            Log.CloseAndFlush();
            _serviceProvider.Dispose();
            base.OnExit(e);
        }
    }
}