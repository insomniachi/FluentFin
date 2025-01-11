
using FluentFin.Core.WebSockets.Messages;

namespace FluentFin.Core.WebSockets;

public class GeneralCommandMessage : InboundSocketMessage<GeneralCommand>
{
	public override SessionMessageType MessageType => SessionMessageType.GeneralCommand;
}

public class PlayStateMessage : InboundSocketMessage<PlaystateRequest>
{
	public override SessionMessageType MessageType => SessionMessageType.Playstate;
}

public class UserDataChangeMessage : InboundSocketMessage<UserDataChangeInfo>
{
	public override SessionMessageType MessageType => SessionMessageType.UserDataChanged;
}