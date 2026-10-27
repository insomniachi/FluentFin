using FluentFin.Core;
using FluentFin.Core.Services;

namespace FluentFin.Core.Tests;

public sealed class LocalSettingsServiceTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FluentFinTests", Guid.NewGuid().ToString("N"));
	private string SettingsFile => Path.Combine(_root, "LocalSettings.json");

	public void Dispose() => Directory.Delete(_root, true);

	[Fact]
	public void SavedValue_SurvivesRestart_AndLeavesNoTempFile()
	{
		new LocalSettingsService(new KnownFolders(_root)).SaveSetting("Volume", 42);

		var reloaded = new LocalSettingsService(new KnownFolders(_root));

		Assert.Equal(42, reloaded.ReadSetting("Volume", 0));
		Assert.False(File.Exists(SettingsFile + ".tmp"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("{\"Servers\": [ {\"Disp")]
	[InlineData("[1, 2]")]
	public void UnreadableFile_StartsWithDefaults_AndKeepsCopy(string content)
	{
		Directory.CreateDirectory(_root);
		File.WriteAllText(SettingsFile, content);

		var service = new LocalSettingsService(new KnownFolders(_root));

		Assert.Equal(7, service.ReadSetting("Volume", 7));
		Assert.Equal(content, File.ReadAllText(SettingsFile + ".corrupt"));
	}
}
