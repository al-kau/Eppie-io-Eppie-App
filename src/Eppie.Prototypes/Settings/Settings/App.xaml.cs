using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Uno.Resizetizer;
using Windows.UI.ViewManagement;

namespace Settings;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();
#if DEBUG
        MainWindow.UseStudio();
#endif


        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        ConfigureMinimumWindowSize();

        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }

    private void ConfigureMinimumWindowSize()
    {
        // The minimum window size is the size of the Xbox app, excluding the TV-safe zone.
        // https://learn.microsoft.com/en-us/windows/apps/design/devices/designing-for-tv#scale-factor-and-adaptive-layout
        // https://learn.microsoft.com/en-us/windows/apps/design/devices/designing-for-tv#tv-safe-area
        // (1920 x 1080 pixels) in 200% scale excluding (48px,27px,48px,27px) area

        const int MinWidth = 864;       // 960 - 48 - 48 = 864
        const int MinHeight = 486;      // 540 - 27 - 27 = 486

#if HAS_UNO

        var view = ApplicationView.GetForCurrentView();
        view.SetPreferredMinSize(new Windows.Foundation.Size(MinWidth, MinHeight));

        // SetPreferredMinSize method doesn't work in Linux OS
        if (OperatingSystem.IsLinux() && MainWindow?.AppWindow is not null)
        {
            // Way #0
            MainWindow.SizeChanged += async (s, e) =>
            {
                if (e.Size.Width < MinWidth && e.Size.Height < MinHeight)
                {
                    await Task.Delay(100).ConfigureAwait(true);
                    MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MinHeight });
                }
                else if (e.Size.Width < MinWidth)
                {
                    await Task.Delay(100).ConfigureAwait(true);
                    MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MainWindow.AppWindow.Size.Height });
                }
                else if (e.Size.Height < MinHeight)
                {
                    await Task.Delay(100).ConfigureAwait(true);
                    MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MainWindow.AppWindow.Size.Width, Height = MinHeight });
                }
            };

            // Way #1
            //MainWindow.SizeChanged += (s, e) =>
            //{
            //    if (MainWindow.AppWindow.Size.Width < MinWidth && MainWindow.AppWindow.Size.Height < MinHeight)
            //    {
            //        await Task.Delay(100).ConfigureAwait(true);
            //        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MinHeight });
            //    }
            //    else if (MainWindow.AppWindow.Size.Width < MinWidth)
            //    {
            //        await Task.Delay(100).ConfigureAwait(true);
            //        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MainWindow.AppWindow.Size.Height });
            //    }
            //        await Task.Delay(100).ConfigureAwait(true);
            //    else if (MainWindow.AppWindow.Size.Height < MinHeight)
            //    {
            //        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MainWindow.AppWindow.Size.Width, Height = MinHeight });
            //    }
            //};

            // Way #2
            //MainWindow.AppWindow.Changed += (s, e) =>
            //{
            //    if (e.DidSizeChange)
            //    {
            //        if (MainWindow.AppWindow.Size.Width < MinWidth && MainWindow.AppWindow.Size.Height < MinHeight)
            //        {
            //            await Task.Delay(100).ConfigureAwait(true);
            //            MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MinHeight });
            //        }
            //        else if (MainWindow.AppWindow.Size.Width < MinWidth)
            //        {
            //            await Task.Delay(100).ConfigureAwait(true);
            //            MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MinWidth, Height = MainWindow.AppWindow.Size.Height });
            //        }
            //        else if (MainWindow.AppWindow.Size.Height < MinHeight)
            //        {
            //            await Task.Delay(100).ConfigureAwait(true);
            //            MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = MainWindow.AppWindow.Size.Width, Height = MinHeight });
            //        }
            //    }
            //};
        }

#else

        OverlappedPresenter presenter = OverlappedPresenter.Create();
        presenter.PreferredMinimumWidth = MinWidth;
        presenter.PreferredMinimumHeight = MinHeight;
        MainWindow?.AppWindow.SetPresenter(presenter);

#endif
    }
}
