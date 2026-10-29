using System.Globalization;
using System.Xml.Linq;

namespace FluentFin.Core.Tests;

/// <summary>
/// Static check of the scrims that keep icons and text readable on top of images and video (issue #25).
/// The image behind a scrim is unknown, so the worst case for a light foreground, a white image, is assumed.
/// Reads the XAML as XML, so it needs neither WinUI nor a running app.
/// </summary>
public class OverlayContrastTests
{
	// WCAG 2.1: 1.4.11 non-text contrast (icons) and 1.4.3 text contrast
	private const double IconContrast = 3.0;
	private const double TextContrast = 4.5;

	private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
	private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

	private static readonly Rgb WhiteImage = new(255, 255, 255);

	// Dark theme values of TextFillColorPrimaryBrush / TextFillColorSecondaryBrush, which RequestedTheme="Dark" selects
	private static readonly (Rgb Color, double Alpha) PrimaryForeground = (new Rgb(255, 255, 255), 1.0);
	private static readonly (Rgb Color, double Alpha) SecondaryForeground = (new Rgb(255, 255, 255), 0xC5 / 255.0);

	private static readonly string[] MediaInfoPages =
	[
		"FluentFin/Views/MoviePage.xaml",
		"FluentFin/Views/SeriesPage.xaml",
		"FluentFin/Views/SeasonPage.xaml",
		"FluentFin/Views/EpisodePage.xaml",
	];

	[Fact]
	public void Contrast_MatchesWcagReferenceValues()
	{
		Assert.Equal(21.0, Contrast(new Rgb(255, 255, 255), new Rgb(0, 0, 0)), 3);
		Assert.Equal(1.0, Contrast(new Rgb(10, 20, 30), new Rgb(10, 20, 30)), 3);
		// #767676 on white is the well known smallest grey that passes 4.5:1
		Assert.InRange(Contrast(new Rgb(0x76, 0x76, 0x76), new Rgb(255, 255, 255)), 4.5, 4.6);
	}

	[Theory]
	[InlineData("PlayerControlsScrimBrush", IconContrast)]
	[InlineData("PlayerControlsScrimBrush", TextContrast)]
	[InlineData("MediaInfoScrimBrush", IconContrast)]
	[InlineData("MediaInfoScrimBrush", TextContrast)]
	[InlineData("Black65O", IconContrast)] // play button drawn over card images
	public void Scrim_KeepsPrimaryForegroundReadableOverWhiteImage(string brushKey, double minimum)
	{
		var ratio = ContrastOnScrim(brushKey, PrimaryForeground);

		Assert.True(ratio >= minimum, $"{brushKey}: primary foreground has {ratio:F2}:1 over a white image, needs {minimum}:1");
	}

	[Theory]
	[InlineData("PlayerControlsScrimBrush")]
	[InlineData("MediaInfoScrimBrush")]
	public void Scrim_KeepsSecondaryTextReadableOverWhiteImage(string brushKey)
	{
		var ratio = ContrastOnScrim(brushKey, SecondaryForeground);

		Assert.True(ratio >= TextContrast, $"{brushKey}: secondary text has {ratio:F2}:1 over a white image, needs {TextContrast}:1");
	}

	[Fact]
	public void TransportControls_AreDarkThemedAndUseScrim()
	{
		var doc = Load("FluentFin/Controls/TransportControls.xaml");

		// everything drawn over the video is dark themed: control bar, skip button, title and clock
		foreach (var name in new[] { "Bar", "DynamicSkipIntroButton", "TitleSection", "TxtTitleTime" })
		{
			Assert.Contains(FindByName(doc, name).AncestorsAndSelf(), e => (string?)e.Attribute("RequestedTheme") == "Dark");
		}

		Assert.Equal("{StaticResource PlayerControlsScrimBrush}", (string?)FindByName(doc, "Bar").Attribute("Background"));
	}

	[Fact]
	public void LoginPage_PanelOverSplashScreenIsDarkThemedWithScrim()
	{
		var panel = Load("FluentFin/Views/LoginPage.xaml").Descendants(Presentation + "StackPanel")
			.Single(e => (string?)e.Attribute("Background") == "{StaticResource MediaInfoScrimBrush}");

		Assert.Equal("Dark", (string?)panel.Attribute("RequestedTheme"));
	}

	[Fact]
	public void BareButtonStyle_UsedForPlayButtonOverImages_HasLightForeground()
	{
		var style = Load("FluentFin/Styles/Button.xaml").Descendants(Presentation + "Style")
			.Single(e => (string?)e.Attribute(Xaml + "Key") == "BareButtonStyle");
		var foreground = style.Elements(Presentation + "Setter").Single(e => (string?)e.Attribute("Property") == "Foreground");

		Assert.Equal("White", (string?)foreground.Attribute("Value"));
	}

	[Fact]
	public void TransportControls_SkipButtonUsesScrim()
	{
		var skip = FindByName(Load("FluentFin/Controls/TransportControls.xaml"), "DynamicSkipIntroButton");

		Assert.Equal("{StaticResource PlayerControlsScrimBrush}", (string?)skip.Attribute("Background"));
	}

	[Theory]
	[MemberData(nameof(MediaInfoPageData))]
	public void MediaInfoPage_IsDarkThemedWithScrimBehindTitleStripAndBody(string page)
	{
		var doc = Load(page);

		// the whole page is dark themed, not only the title strip: the body text sits on the same backdrop
		var strip = FindByName(doc, "ButtonStrip");
		Assert.Contains(strip.AncestorsAndSelf(), e => (string?)e.Attribute("RequestedTheme") == "Dark");
		Assert.Equal("Dark", (string?)doc.Root!.Elements().Single(e => e.Name.LocalName == "Grid").Attribute("RequestedTheme"));

		const string scrim = "{StaticResource MediaInfoScrimBrush}";

		// title strip: the Border bottom aligned in the top rows
		Assert.Contains(doc.Descendants(Presentation + "Border"),
			e => (string?)e.Attribute("Background") == scrim && (string?)e.Attribute("VerticalAlignment") == "Bottom");

		// body: the layer right after the blurred backdrop
		var blurred = doc.Descendants().Single(e => e.Name.LocalName == "BlurEffect").Ancestors(Presentation + "Grid").First();
		var overlay = blurred.ElementsAfterSelf().First();
		Assert.Equal("Border", overlay.Name.LocalName);
		Assert.Equal(scrim, (string?)overlay.Attribute("Background"));
		Assert.Equal("2", (string?)overlay.Attribute("Grid.Row"));
	}

	[Theory]
	[MemberData(nameof(ScrimmedFileData))]
	public void ScrimmedFiles_DoNotDeclareTheirOwnTranslucentBrushes(string file)
	{
		var translucent = Load(file)
			.Descendants(Presentation + "SolidColorBrush")
			.Where(b => double.Parse((string?)b.Attribute("Opacity") ?? "1", CultureInfo.InvariantCulture) < 1)
			.Select(b => (string?)b.Attribute(Xaml + "Key") ?? "(inline)")
			.ToList();

		Assert.True(translucent.Count == 0,
			$"{file} declares translucent brush(es) {string.Join(", ", translucent)}; define them in FluentFin/Styles/Button.xaml so the contrast tests cover them");
	}

	public static TheoryData<string> MediaInfoPageData() => [.. MediaInfoPages];

	public static TheoryData<string> ScrimmedFileData() => [.. MediaInfoPages, "FluentFin/Controls/TransportControls.xaml", "FluentFin/Views/LoginPage.xaml"];

	private static double ContrastOnScrim(string brushKey, (Rgb Color, double Alpha) foreground)
	{
		var (color, opacity) = LoadBrush(brushKey);

		var background = Blend(color, opacity, WhiteImage);
		var text = Blend(foreground.Color, foreground.Alpha, background);

		return Contrast(text, background);
	}

	private static (Rgb Color, double Opacity) LoadBrush(string key)
	{
		var brush = Load("FluentFin/Styles/Button.xaml")
			.Descendants(Presentation + "SolidColorBrush")
			.Single(b => (string?)b.Attribute(Xaml + "Key") == key);

		return (ParseColor((string)brush.Attribute("Color")!),
				double.Parse((string?)brush.Attribute("Opacity") ?? "1", CultureInfo.InvariantCulture));
	}

	private static Rgb ParseColor(string value) => value switch
	{
		"Black" => new Rgb(0, 0, 0),
		"White" => new Rgb(255, 255, 255),
		['#', .. var hex] when hex.Length == 6 => new Rgb(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..], 16)),
		_ => throw new NotSupportedException($"Colour '{value}' is not supported; use Black, White or #RRGGBB in Button.xaml")
	};

	private static XElement FindByName(XDocument doc, string name) =>
		doc.Descendants().Single(e => (string?)e.Attribute(Xaml + "Name") == name);

	private static XDocument Load(string repoRelativePath) => XDocument.Load(Path.Combine(RepoRoot, repoRelativePath));

	private static string RepoRoot { get; } = FindRepoRoot();

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FluentFin.sln")))
		{
			dir = dir.Parent;
		}

		return dir?.FullName ?? throw new DirectoryNotFoundException("FluentFin.sln not found above " + AppContext.BaseDirectory);
	}

	private readonly record struct Rgb(double R, double G, double B);

	private static Rgb Blend(Rgb top, double alpha, Rgb bottom) => new(
		top.R * alpha + bottom.R * (1 - alpha),
		top.G * alpha + bottom.G * (1 - alpha),
		top.B * alpha + bottom.B * (1 - alpha));

	private static double Luminance(Rgb c)
	{
		static double Channel(double v)
		{
			var s = v / 255.0;
			return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
		}

		return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
	}

	private static double Contrast(Rgb a, Rgb b)
	{
		var (hi, lo) = (Luminance(a), Luminance(b));
		if (hi < lo)
		{
			(hi, lo) = (lo, hi);
		}

		return (hi + 0.05) / (lo + 0.05);
	}
}
