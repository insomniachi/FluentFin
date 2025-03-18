using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


namespace FluentFin.Controls;

#nullable disable

public sealed partial class LazyLoadedImage : UserControl
{
    public LazyLoadedImage()
    {
        InitializeComponent();

        ImageFadeIn.Completed += (object sender, object e) =>
        {
            EnableBlurHash = false;
            BlurHashImageSource = null;
        };
    }

    [GeneratedDependencyProperty]
    public partial ImageSource ImageSource { get; set; }

    [GeneratedDependencyProperty]
    public partial bool EnableBlurHash { get; set; }

    [GeneratedDependencyProperty]
    public partial WriteableBitmap BlurHashImageSource { get; set; }

    [GeneratedDependencyProperty(DefaultValue = Stretch.UniformToFill)]
    public partial Stretch Stretch { get; set; }

    private void ImageOpened(object sender, RoutedEventArgs e) => ImageFadeIn.Begin();
}

#nullable restore