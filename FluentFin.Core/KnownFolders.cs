namespace FluentFin.Core;

public class KnownFolders
{
	public string ApplicationData { get; }
	public string Updates { get; }
	public string Logs { get; }

	public KnownFolders() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FluentFin")) { }

	public KnownFolders(string root)
	{
		ApplicationData = root;
		Updates = Path.Combine(root, "Updates");
		Logs = Path.Combine(root, "Logs");

		Directory.CreateDirectory(ApplicationData);
		Directory.CreateDirectory(Logs);
		Directory.CreateDirectory(Updates);
	}
}
