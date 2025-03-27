using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets;


public abstract class WebSocketMessage
{
	[JsonConverter(typeof(JsonStringEnumConverter<SessionMessageType>))]
	public abstract SessionMessageType MessageType { get; }

	public string? ServerId { get; set; }
}

public abstract class SocketMessage<T> : WebSocketMessage, IInboundSocketMessage
{
	[JsonConverter(typeof(JsonNullableGuidConverter))]
	public Guid? MessageId { get; set; }
	public T? Data { get; set; }
}

public interface IInboundSocketMessage { }
