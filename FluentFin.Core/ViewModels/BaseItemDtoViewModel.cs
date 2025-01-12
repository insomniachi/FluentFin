using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk.Generated.Models;
using Riok.Mapperly.Abstractions;

namespace FluentFin.Core.ViewModels;

[ObservableObject]
public partial class BaseItemDtoViewModel : BaseItemDto
{
	[ObservableProperty]
	public partial UserItemDataDto? DynamicUserData { get; set; }
}

[Mapper]
public static partial class BaseItemDtoMapper
{
	[MapProperty(nameof(BaseItemDto.UserData), nameof(BaseItemDtoViewModel.DynamicUserData))]
	public static partial BaseItemDtoViewModel MapToViewModel(BaseItemDto dto);
}
