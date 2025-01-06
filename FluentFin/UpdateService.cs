using FluentFin.Core;
using Flurl.Http;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace FluentFin;

#nullable disable
public class WindowsUpdateService(KnownFolders knownFolders) : IHostedService
{
	private VersionInfo _current;
	private CancellationTokenSource _cts;
	private readonly CompositeDisposable _disposable = new();
	private readonly HttpClient _httpClient = new();

	private static async Task<string> TryGetStreamAsync()
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

	public async ValueTask<VersionInfo> GetCurrentVersionInfo()
	{
		if (_current is not null)
		{
			return _current;
		}

		var url = $"https://api.github.com/repositories/insomniachi/fluentfin/releases/tags/{Assembly.GetEntryAssembly().GetName().Version.ToString(3)}";
		var response = await _httpClient.GetAsync(url);

		if (response.IsSuccessStatusCode)
		{
			var jsonNode = JsonNode.Parse(await response.Content.ReadAsStreamAsync());
			_current = new VersionInfo()
			{
				Version = new Version(jsonNode["tag_name"].ToString()),
				Url = (string)jsonNode["assets"][0]["browser_download_url"].AsValue(),
				Body = jsonNode?["body"]?.ToString()
			};
		}
		return _current;
	}

	public async Task<VersionInfo> DownloadUpdate(VersionInfo versionInfo)
	{
		var ext = Path.GetExtension(versionInfo.Url);
		var fileName = $"Totoro_{versionInfo.Version}{ext}";
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

		_cts = new();
		using var client = new HttpClient();
		using var s = await client.GetStreamAsync(versionInfo.Url);
		using var fs = new FileStream(fullPath, FileMode.OpenOrCreate);

		try
		{
			await s.CopyToAsync(fs, _cts.Token);
		}
		catch (Exception)
		{
			fs.Dispose();
			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
			}
		}

		return versionInfo;
	}

	public static void InstallUpdate(VersionInfo versionInfo)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = $"{versionInfo.FilePath}",
		});
		Process.GetCurrentProcess().CloseMainWindow();
	}

	public void ShutDown() => _cts?.Cancel();

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Observable
				.Timer(TimeSpan.Zero, TimeSpan.FromHours(1))
				.ObserveOn(RxApp.TaskpoolScheduler)
				.SelectMany(_ => TryGetStreamAsync())
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x => JsonNode.Parse(x))
				.Select(jsonNode => new VersionInfo()
				{
					Version = new Version(jsonNode["tag_name"].ToString()),
					Url = (string)jsonNode["assets"][0]["browser_download_url"].AsValue(),
					Body = jsonNode?["body"]?.ToString()
				})
				.Where(vi =>
				{
					var current = Assembly.GetEntryAssembly().GetName().Version;
					return vi.Version > current;
				})
				.Throttle(TimeSpan.FromSeconds(3))
				.SelectMany(DownloadUpdate)
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