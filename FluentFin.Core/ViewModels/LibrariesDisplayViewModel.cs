using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesDisplayViewModel(IJellyfinClient jellyfinClient) : ServerConfigurationViewModel(jellyfinClient), INavigationAware
{
    private Metadata? _metadata;

    public List<string> DateAddedBehaviors { get; set; } = ["User file creation date", "Use date scanned into the library"];

    protected override List<JellyfinConfigItemViewModel> CreateItems(ServerConfiguration config)
    {

        List<JellyfinConfigItemViewModel> items =
        [
            new JellyfinConfigItemViewModel<bool>(() => config.EnableFolderView ?? false, value => config.EnableFolderView = value)
            {
                DisplayName = "Display a folder view to show plain media folders",
                Description = "Display folders alongside your other media libraries. This can be useful if you'd like to have a plain folder view."
            },
			new JellyfinConfigItemViewModel<bool>(() => config.DisplaySpecialsWithinSeasons ?? false, value => config.DisplaySpecialsWithinSeasons = value)
            {
                DisplayName = "Display specials within seasons they aired in",
            },
            new JellyfinConfigItemViewModel<bool>(() => config.EnableGroupingIntoCollections ?? false, value => config.EnableGroupingIntoCollections = value)
            {
                DisplayName = "Group movies into collections",
                Description = "Movies in a collection will be displayed as one grouped item when displaying movie lists."
            },
            new JellyfinConfigItemViewModel<bool>(() => config.EnableExternalContentInSuggestions ?? false, value => config.EnableExternalContentInSuggestions = value)
            {
                DisplayName = "Enable external content in suggestions", 
                Description = "Allow internet trailers and live TV programs to be included within suggested content."
            }
        ];

        if(_metadata is not null)
        {
            var item = new JellyfinSelectableConfigItemViewModel(() => _metadata.UseFileCreationTimeForDateAdded ? DateAddedBehaviors[0] : DateAddedBehaviors[1],
                                                                 value => _metadata.UseFileCreationTimeForDateAdded = value?.Equals(DateAddedBehaviors[0]) == true,
                                                                 DateAddedBehaviors)
            {
                DisplayName = "Date added behavior for new content",
                Description = "If a metadata value is present, it will always be used before either of these options."
            };

            items.Insert(0, item);
        }

        return items;
    }

    async Task INavigationAware.OnNavigatedTo(object parameter)
    {
        _metadata = await _jellyfinClient.GetMetadata();

        await OnNavigatedTo(parameter);
    }
}
