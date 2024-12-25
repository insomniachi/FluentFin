using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public interface IHandleClose
{
	bool CanClose { get; set; }
}

public interface IBaseItemDialogViewModel
{
	Task Initialize(BaseItemDto item);
}
