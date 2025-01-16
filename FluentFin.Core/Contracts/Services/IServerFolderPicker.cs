namespace FluentFin.Core.Contracts.Services;

public interface IServerFolderPicker
{
	Task<string> PickFolder();
}
