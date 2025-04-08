namespace FluentFin.Core.Contracts.Services;

public interface IContentDialogServiceCore
{
	Task ShowMessage(string title, string message, TimeSpan? timeout = null);
	void Growl(string title, string message, TimeSpan waitTime);
	Task<bool> QuestionYesNo(string title, string message);
}
