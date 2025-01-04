using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public interface IBaseItemDialogViewModel
{
	Task Initialize(BaseItemDto item);
}

public interface IItemDialogViewModel
{
	Task Initialize(Guid? id);
}
