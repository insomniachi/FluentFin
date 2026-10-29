using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FluentFin.Core.Tests;

public sealed class PlaybackSessionManagerTests
{
	private readonly Mock<IJellyfinClient> _client = new();
	private readonly PlaybackSessionManager _manager;

	public PlaybackSessionManagerTests()
	{
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).ReturnsAsync(true);
		_client.Setup(x => x.StopEncoding(It.IsAny<string>())).ReturnsAsync(true);
		_manager = new PlaybackSessionManager(_client.Object, NullLogger<PlaybackSessionManager>.Instance);
	}

	private static MediaResponse Media(string playSessionId, PlaybackProgressInfo_PlayMethod method = PlaybackProgressInfo_PlayMethod.Transcode)
		=> new(new Uri("http://localhost/video"), method, playSessionId, "source", new MediaSourceInfo());

	private static PlaybackState State(long ticks = 100, bool paused = false) => new(ticks, paused, false, null, null);

	[Fact]
	public async Task Stop_SendsPlaySessionIdItemAndMediaSource()
	{
		var itemId = Guid.NewGuid();
		var session = _manager.Begin(itemId, Media("play-1"));

		await _manager.StopAsync(session, 42);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i =>
			i.PlaySessionId == "play-1" && i.ItemId == itemId && i.MediaSourceId == "source" && i.PositionTicks == 42)), Times.Once);
	}

	[Fact]
	public async Task Stop_BeforeFirstProgressReport_StillSendsStop()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "play-1")), Times.Once);
	}

	[Fact]
	public async Task Stop_WithoutKnownPosition_IsReportedAsFailedSoWatchStateIsKept()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "play-1" && i.Failed == true)), Times.Once);
	}

	[Fact]
	public async Task Stop_WithPosition_UpdatesWatchState()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session, 5);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.Failed == false && i.PositionTicks == 5)), Times.Once);
	}

	[Fact]
	public async Task Stop_WithoutUpdatingWatchState_IsReportedAsFailed()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session, 5, updateWatchState: false);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.Failed == true)), Times.Once);
	}

	[Fact]
	public async Task Stop_UsesLastReportedPositionWhenNothingElseIsKnown()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));
		await _manager.ReportProgressAsync(session, State(321));

		await _manager.StopAsync(session);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PositionTicks == 321 && i.Failed == false)), Times.Once);
	}

	[Fact]
	public async Task Stop_UsesPositionProviderWhenNoPositionIsGiven()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));
		session.PositionProvider = () => 777;

		await _manager.StopAsync(session);

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PositionTicks == 777)), Times.Once);
	}

	[Fact]
	public async Task Stop_CalledRepeatedlyAndConcurrently_SendsOneRequest()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => _manager.StopAsync(session))));
		await _manager.StopAsync(session);

		_client.Verify(x => x.Stop(It.IsAny<PlaybackStopInfo>()), Times.Once);
	}

	[Fact]
	public async Task Stop_OfOldSession_DoesNotAffectNewSession()
	{
		var itemId = Guid.NewGuid();
		var old = _manager.Begin(itemId, Media("old"));
		var current = _manager.Begin(itemId, Media("new", PlaybackProgressInfo_PlayMethod.DirectPlay));

		await _manager.StopAsync(old);
		await _manager.ReportProgressAsync(current, State());

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "old")), Times.Once);
		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "new")), Times.Never);
		_client.Verify(x => x.Progress(It.Is<PlaybackProgressInfo>(i => i.PlaySessionId == "new")), Times.Once);
	}

	[Fact]
	public async Task Progress_AfterStop_IsNotSent()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session);
		await _manager.ReportProgressAsync(session, State());
		await _manager.ReportStartAsync(session, State());

		_client.Verify(x => x.Progress(It.IsAny<PlaybackProgressInfo>()), Times.Never);
		_client.Verify(x => x.Playing(It.IsAny<PlaybackStartInfo>()), Times.Never);
	}

	[Fact]
	public async Task Progress_UsesPlaySessionIdAndLeavesSessionIdToTheServer()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.ReportProgressAsync(session, State(500, paused: true));

		_client.Verify(x => x.Progress(It.Is<PlaybackProgressInfo>(i =>
			i.PlaySessionId == "play-1" && i.SessionId == null && i.IsPaused == true && i.PositionTicks == 500
			&& i.PlayMethod == PlaybackProgressInfo_PlayMethod.Transcode)), Times.Once);
	}

	[Fact]
	public async Task Start_SendsPlaySessionIdAndPlayMethod()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.ReportStartAsync(session, State(0));

		_client.Verify(x => x.Playing(It.Is<PlaybackStartInfo>(i =>
			i.PlaySessionId == "play-1" && i.MediaSourceId == "source" && i.PlayMethod == PlaybackStartInfo_PlayMethod.Transcode)), Times.Once);
	}

	[Fact]
	public async Task Pause_DoesNotStopTheSession()
	{
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.ReportProgressAsync(session, State(paused: true));
		await _manager.ReportProgressAsync(session, State(paused: false));

		_client.Verify(x => x.Stop(It.IsAny<PlaybackStopInfo>()), Times.Never);
		_client.Verify(x => x.StopEncoding(It.IsAny<string>()), Times.Never);
		_client.Verify(x => x.Progress(It.Is<PlaybackProgressInfo>(i => i.PlaySessionId == "play-1")), Times.Exactly(2));
	}

	[Fact]
	public async Task Stop_WhenReportFails_FallsBackToExplicitEncodingStop()
	{
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).ReturnsAsync(false);
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session);

		_client.Verify(x => x.StopEncoding("play-1"), Times.Once);
	}

	[Fact]
	public async Task Stop_WhenReportFailsForDirectPlay_DoesNotStopEncoding()
	{
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).ReturnsAsync(false);
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1", PlaybackProgressInfo_PlayMethod.DirectPlay));

		await _manager.StopAsync(session);

		_client.Verify(x => x.StopEncoding(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task Stop_WhenClientThrows_DoesNotThrow()
	{
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).ThrowsAsync(new HttpRequestException());
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		await _manager.StopAsync(session);

		Assert.True(session.IsStopping);
	}

	[Fact]
	public async Task StopAll_StopsEveryActiveSessionOnce()
	{
		var a = _manager.Begin(Guid.NewGuid(), Media("a"));
		_manager.Begin(Guid.NewGuid(), Media("b"));
		await _manager.StopAsync(a);

		await _manager.StopAllAsync(TimeSpan.FromSeconds(5));
		await _manager.StopAllAsync(TimeSpan.FromSeconds(5));

		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "a")), Times.Once);
		_client.Verify(x => x.Stop(It.Is<PlaybackStopInfo>(i => i.PlaySessionId == "b")), Times.Once);
	}

	[Fact]
	public async Task StopAll_DoesNotWaitLongerThanTheTimeout()
	{
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).Returns(async () => { await Task.Delay(TimeSpan.FromSeconds(30)); return true; });
		_manager.Begin(Guid.NewGuid(), Media("a"));

		var started = DateTime.UtcNow;
		await _manager.StopAllAsync(TimeSpan.FromMilliseconds(200));

		Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task Stop_WaitsForInFlightProgressReport()
	{
		var release = new TaskCompletionSource();
		var order = new List<string>();
		_client.Setup(x => x.Progress(It.IsAny<PlaybackProgressInfo>())).Returns(async () => { order.Add("progress-start"); await release.Task; order.Add("progress-end"); });
		_client.Setup(x => x.Stop(It.IsAny<PlaybackStopInfo>())).ReturnsAsync(() => { order.Add("stop"); return true; });
		var session = _manager.Begin(Guid.NewGuid(), Media("play-1"));

		var progress = _manager.ReportProgressAsync(session, State());
		var stop = _manager.StopAsync(session);
		release.SetResult();
		await Task.WhenAll(progress, stop);

		Assert.Equal(["progress-start", "progress-end", "stop"], order);
	}
}
