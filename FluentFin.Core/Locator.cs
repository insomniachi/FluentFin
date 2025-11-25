using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Core;

public static class Locator
{
	private static IServiceProvider? _current;

	public static void SetServiceProvider(IServiceProvider sp) => _current = sp;

	public static T GetService<T>()
		where T : class
	{
		if (_current?.GetService(typeof(T)) is not T service)
		{
			throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
		}

		return service;
	}

	public static T GetKeyedService<T>(object key)
	where T : class
	{
		if (_current?.GetKeyedService<T>(key) is not T service)
		{
			throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
		}

		return service;
	}
}
