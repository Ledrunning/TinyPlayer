// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using TinyPlayer.Core;
using TinyPlayer.Core.Abstractions;
using TinyPlayer.Desktop.View;
using TinyPlayer.Desktop.ViewModel;
using MessageBox = System.Windows.MessageBox;

namespace TinyPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .UseSerilog((context, config) =>        // ← вот это
            {
                config
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        path: "logs/tinyplayer-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .WriteTo.Debug();
            })
            .ConfigureAppConfiguration(c => { c.SetBasePath(basePath: Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) 
                ?? throw new InvalidOperationException("Unable to determine assembly location")); })
            .ConfigureServices((context, services) =>
            {

                // Views and ViewModels
                services.AddSingleton<IVideoPlayerFactory, VideoPlayerFactory>();
                services.AddScoped<MainWindow>();
                services.AddScoped<MainViewModel>();
            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T? GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                _host.Start();
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                Current.MainWindow = mainWindow;
                Log.Information("Application started");
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start");
                MessageBox.Show($"Failed to start: {ex.Message}", "TinyPlayer",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            try
            {
                Log.Information("Application is shutting down");
                await _host.StopAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during shutdown");
            }
            finally
            {
                Log.CloseAndFlush();
                _host.Dispose();
            }
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled exception");
            MessageBox.Show($"Unexpected error: {e.Exception.Message}", "TinyPlayer",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
