namespace FluentFin.Core.WebSockets
{
	public interface IWebSocketMessageHandler
	{
		Task HandleMessage(IInboundSocketMessage message);
	}
}
