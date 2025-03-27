
using FluentFin.Core.WebSockets.Messages;

namespace FluentFin.Core.WebSockets;

public class GeneralCommandMessage : SocketMessage<GeneralCommand>
{
	public override SessionMessageType MessageType => SessionMessageType.GeneralCommand;
}

public class PlayStateMessage : SocketMessage<PlaystateRequest>
{
	public override SessionMessageType MessageType => SessionMessageType.Playstate;
}

public class UserDataChangeMessage : SocketMessage<UserDataChangeInfo>
{
	public override SessionMessageType MessageType => SessionMessageType.UserDataChanged;
}

public class SyncPlayCommandMessage : SocketMessage<SyncPlaySendCommand>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayCommand;
}

public class PlayQueueUpdateMessage : SocketMessage<GroupUpdate<PlayQueueUpdate>>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class UserJoinedUpdateMessage : SocketMessage<GroupUpdate<string>>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class UserLeftUpdateMessage : SocketMessage<GroupUpdate<string>>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class GroupJoinedUpdateMessage : SocketMessage<GroupUpdate<GroupJoinedUpdate>>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}

public class GroupLeftUpdateMessage : SocketMessage<GroupUpdate<string>>
{
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}