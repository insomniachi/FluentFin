using System.Windows.Input;

namespace FluentFin.Core;

public static class CommandFactory
{
	private static readonly Dictionary<string, ICommand> _commands = [];

	static CommandFactory()
	{
		var globalCommands = Locator.GetService<GlobalCommands>();
		RegisterCommand("Unpin", globalCommands.UnPinFromSideBarCommand);
	}

	public static void RegisterCommand(string name, ICommand command)
	{
		if(_commands.ContainsKey(name))
		{
			return;
		}

		_commands.Add(name, command);
	}

	public static ICommand? FindByName(string name)
	{
		if (_commands.TryGetValue(name, out var command))
		{
			return command;
		}

		return null;
	}
}
