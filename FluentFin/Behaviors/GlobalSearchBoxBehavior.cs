using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
namespace FluentFin.Behaviors;

public partial class GlobalSearchBoxBehavior : Behavior<AutoSuggestBox>
{
	private IJellyfinClient _jellyfinClient = App.GetService<IJellyfinClient>();
	private IDisposable? _disposable;
	private readonly Subject<string> _textChangedSubject = new();

	protected override void OnAttached()
	{
		base.OnAttached();


		AssociatedObject.TextChanged += AssociatedObject_TextChanged;
		AssociatedObject.SuggestionChosen += AssociatedObject_SuggestionChosen;

		_disposable = _textChangedSubject
			.DistinctUntilChanged()
			.Throttle(TimeSpan.FromMilliseconds(500))
			.SelectMany(_jellyfinClient.Search)
			.WhereNotNull()
			.Subscribe(result =>
			{
				AssociatedObject.DispatcherQueue.TryEnqueue(() =>
				{
					AssociatedObject.ItemsSource = result.Items;
				});
			});
	}

	private void AssociatedObject_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if(args.SelectedItem is not BaseItemDto dto)
		{
			return;
		}

		sender.Text = string.Empty;

		App.Commands.DisplayDto(dto);
	}

	private void AssociatedObject_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if(args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
		{
			return;
		}

		if(string.IsNullOrEmpty(sender.Text))
		{
			return;
		}

		if(sender.Text is { Length : < 3})
		{
			return;
		}

		_textChangedSubject.OnNext(sender.Text);
	}

	protected override void OnDetaching()
	{
		_disposable?.Dispose();
		AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
		AssociatedObject.SuggestionChosen -= AssociatedObject_SuggestionChosen;
		base.OnDetaching();
	}
}
