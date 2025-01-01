namespace FluentFin.Core.Contracts.Services;

public interface IUserInput<T>
{
	Task<T?> GetValue();
}
