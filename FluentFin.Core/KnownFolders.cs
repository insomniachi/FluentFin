namespace FluentFin.Core;

public class KnownFolders
{
	public string ApplicationData { get; }
	public string Updates { get; }
	public string Logs { get; }

	public KnownFolders()
	{
		ApplicationData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FluentFin");
		Updates = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FluentFin\Updates");
		Logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FluentFin\Logs");

		Directory.CreateDirectory(ApplicationData);
		Directory.CreateDirectory(Logs);
		Directory.CreateDirectory(Updates);
	}
}
