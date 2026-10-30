using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using FluentFin.Core;
using FluentFin.Services;
using Flurl.Http;
using Microsoft.Extensions.Hosting;
using ReactiveUI;

namespace FluentFin;

#nullable disable
public class WindowsUpdateService(KnownFolders knownFolders,
								  IContentDialogService contentDialogService) : IHostedService
{
	private readonly CompositeDisposable _disposable = [];

	private static async Task<string> TryGetStreamAsync()
	{
		try
		{
			var response = await "https://api.github.com/repos/insomniachi/fluentfin/releases/latest"
				.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.81 Safari/537.36 Edg/104.0.1293.54")
				.AllowAnyHttpStatus()
				.GetAsync();

			if (response.StatusCode > 300)
			{
				return "";
			}

			return await response.GetStringAsync();
		}
		catch
		{
			return "";
		}
	}

	public async Task<VersionInfo> DownloadUpdate(VersionInfo versionInfo)
	{
		var ext = Path.GetExtension(versionInfo.Url);
		var fileName = $"FluentFin_{versionInfo.Version}{ext}";
		var fullPath = Path.Combine(knownFolders.Updates, fileName);
		versionInfo.FilePath = fullPath;

		foreach (var file in Directory.GetFiles(knownFolders.Updates).Where(x => x != fullPath))
		{
			File.Delete(file);
		}

		if (File.Exists(fullPath)) // already download before
		{
			return versionInfo;
		}

		// download to a temp name so an interrupted download is never mistaken for a complete installer
		var partPath = fullPath + ".part";
		using (var client = new HttpClient())
		using (var s = await client.GetStreamAsync(versionInfo.Url))
		using (var fs = new FileStream(partPath, FileMode.Create))
		{
			await s.CopyToAsync(fs);
		}

		File.Move(partPath, fullPath, true);
		return versionInfo;
	}

	// installers end with the architecture, e.g. FluentFin_x64.exe, FluentFin_arm64.exe.
	// OSArchitecture, not ProcessArchitecture: an x64 build emulated on ARM64 Windows should move to the native build.
	private static string SelectAssetUrl(JsonArray assets)
	{
		var suffix = $"_{RuntimeInformation.OSArchitecture}";
		var urls = assets.Select(a => (string)a["browser_download_url"].AsValue()).ToList();
		return urls.FirstOrDefault(u => Path.GetFileNameWithoutExtension(u).EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ?? urls[0];
	}

	private async Task<VersionInfo> CheckForUpdate()
	{
		var json = await TryGetStreamAsync();
		if (string.IsNullOrEmpty(json))
		{
			return null;
		}

		var jsonNode = JsonNode.Parse(json);
		var versionInfo = new VersionInfo()
		{
			Version = new Version(jsonNode["tag_name"].ToString()),
			Url = SelectAssetUrl(jsonNode["assets"].AsArray()),
			Body = jsonNode?["body"]?.ToString()
		};

		if (versionInfo.Version <= Assembly.GetEntryAssembly().GetName().Version)
		{
			return null;
		}

		return await DownloadUpdate(versionInfo);
	}

	public async void InstallUpdate(VersionInfo versionInfo)
	{
		var canUpdate = await contentDialogService.QuestionYesNo($"New version {versionInfo.Version}", "Install update now?");

		if (!canUpdate)
		{
			return;
		}

		Process.Start(new ProcessStartInfo
		{
			FileName = $"{versionInfo.FilePath}",
		});
		Process.GetCurrentProcess().CloseMainWindow();
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Observable
				.Timer(TimeSpan.Zero, TimeSpan.FromHours(1))
				.ObserveOn(RxSchedulers.TaskpoolScheduler)
				// a failed check (no network, release without assets yet) skips this round instead of crashing the app
				.SelectMany(_ => Observable.FromAsync(CheckForUpdate).Catch(Observable.Return<VersionInfo>(null)))
				.Where(vi => vi?.FilePath is { } path && File.Exists(path))
				.ObserveOn(RxSchedulers.MainThreadScheduler)
				.Subscribe(InstallUpdate)
				.DisposeWith(_disposable);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_disposable.Dispose();
		return Task.CompletedTask;
	}
}

public class VersionInfo
{
	public Version Version { get; set; }
	public string Details { get; set; }
	public string Url { get; set; }
	public string FilePath { get; set; }
	public string Body { get; set; }
}

#nullable restore