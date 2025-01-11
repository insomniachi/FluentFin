using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets;


public abstract class WebSocketMessage
{
	[JsonConverter(typeof(JsonStringEnumConverter<SessionMessageType>))]
	public abstract SessionMessageType MessageType { get; }
	
	[JsonIgnore]
	public string? ServerId { get; set; }
}

public abstract class InboundSocketMessage<T> : WebSocketMessage, IInboundSocketMessage
{
	[JsonConverter(typeof(JsonGuidConverter))]
	public Guid MessageId { get; set; }
	public T? Data { get; set; }
}

public interface IInboundSocketMessage { }
