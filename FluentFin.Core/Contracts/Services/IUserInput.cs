namespace FluentFin.Core.Contracts.Services;

public interface IUserInput<T>
{
	Task<T?> GetValue();
}

public static class UserInputs
{
	public const string LibraryNewName = "LibraryNewName";
	public const string MessageToSession = "MessageToSession";
}
