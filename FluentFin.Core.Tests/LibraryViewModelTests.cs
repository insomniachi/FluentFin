using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Moq;

namespace FluentFin.Core.Tests;

public sealed class LibraryViewModelTests
{
	static LibraryViewModelTests() => ReactiveUI.Builder.RxAppBuilder.CreateReactiveUIBuilder().BuildApp();

	[Fact]
	public async Task ChangingSort_ShowsCorrectlyOrderedPages()
	{
		var random = new Random(1);
		var dtos = Enumerable.Range(0, 250)
			.Select(i => new BaseItemDto { Id = Guid.NewGuid(), Name = $"Item {i:D3}", CommunityRating = random.Next(0, 4) })
			.ToList();

		var client = new Mock<IJellyfinClient>();
		client.Setup(x => x.GetItems(It.IsAny<BaseItemDto>(), It.IsAny<bool>())).ReturnsAsync(new BaseItemDtoQueryResult { Items = dtos });
		var vm = new LibraryViewModel(client.Object);
		await vm.OnNavigatedTo(new BaseItemDto());

		vm.SelectedPage = 2;
		vm.UpdateSortBy(ItemSortBy.CommunityRating);
		AssertPage(vm, dtos, 0);

		vm.SelectedPage = 1;
		AssertPage(vm, dtos, 1);

		vm.UpdateSortOrder(SortOrder.Descending);
		AssertPage(vm, dtos, 0, descending: true);
		Assert.Equal(3, vm.NumberOfPages);
	}

	private static void AssertPage(LibraryViewModel vm, List<BaseItemDto> dtos, int page, bool descending = false)
	{
		var sorted = descending
			? dtos.OrderByDescending(x => x.CommunityRating).ThenBy(x => x.Name)
			: dtos.OrderBy(x => x.CommunityRating).ThenBy(x => x.Name);

		Assert.Equal(page, vm.SelectedPage);
		Assert.Equal(sorted.Skip(page * 100).Take(100).Select(x => x.Name), vm.Items.Select(x => x.Name));
	}
}
