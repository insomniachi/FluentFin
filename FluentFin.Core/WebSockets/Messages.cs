
using FluentFin.Core.WebSockets.Messages;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.WebSockets;

public class GeneralCommandMessage : WebSocketMessage<GeneralCommand>
{
	public override SessionMessageType MessageType => SessionMessageType.GeneralCommand;
}

public class PlayStateMessage : WebSocketMessage<PlaystateRequest>
{
	public override SessionMessageType MessageType => SessionMessageType.Playstate;
}

public class UserDataChangeMessage : WebSocketMessage<UserDataChangeInfo>
{
	public override SessionMessageType MessageType => SessionMessageType.UserDataChanged;
}

public class SyncPlayCommandMessage : WebSocketMessage<SyncPlaySendCommand>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayCommand;
}

public class PlayQueueUpdateMessage : WebSocketMessage<GroupUpdate<PlayQueueUpdate>>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class UserJoinedUpdateMessage : WebSocketMessage<GroupUpdate<string>>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class UserLeftUpdateMessage : WebSocketMessage<GroupUpdate<string>>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class GroupJoinedUpdateMessage : WebSocketMessage<GroupUpdate<GroupJoinedUpdate>>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class GroupLeftUpdateMessage : WebSocketMessage<GroupUpdate<string>>
{
	public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class SessionInfoMessage : WebSocketMessage<List<SessionInfoDto>>
{
	public override SessionMessageType MessageType => SessionMessageType.Sessions;
}

// Outbound
public class SessionsStartMessage : WebSocketMessage<string>
{
	public override SessionMessageType MessageType => SessionMessageType.SessionsStart;
}

public class SessionsStopMessage : WebSocketMessage
{
	public override SessionMessageType MessageType => SessionMessageType.SessionsStop;
}

public class KeepAliveMessage : WebSocketMessage
{
	public override SessionMessageType MessageType => SessionMessageType.KeepAlive;
}