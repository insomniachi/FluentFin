using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets.Messages;

public class PlaystateRequest
{
	[JsonConverter(typeof(JsonStringEnumConverter<PlaystateCommand>))]
	public PlaystateCommand Command { get; set; }

	public long? SeekPositionTicks { get; set; }

	public string? ControllingUserId { get; set; }
}

public enum PlaystateCommand
{
	/// <summary>
	/// The stop.
	/// </summary>
	Stop,

	/// <summary>
	/// The pause.
	/// </summary>
	Pause,

	/// <summary>
	/// The unpause.
	/// </summary>
	Unpause,

	/// <summary>
	/// The next track.
	/// </summary>
	NextTrack,

	/// <summary>
	/// The previous track.
	/// </summary>
	PreviousTrack,

	/// <summary>
	/// The seek.
	/// </summary>
	Seek,

	/// <summary>
	/// The rewind.
	/// </summary>
	Rewind,

	/// <summary>
	/// The fast forward.
	/// </summary>
	FastForward,

	/// <summary>
	/// The play pause.
	/// </summary>
	PlayPause
}
