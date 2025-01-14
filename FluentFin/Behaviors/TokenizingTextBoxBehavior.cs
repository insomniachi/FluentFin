using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace FluentFin.Behaviors;

public class TokenizingTextBoxBehavior : Behavior<TokenizingTextBox>
{

	public IEnumerable<string> Suggestions
	{
		get { return (IEnumerable<string>)GetValue(SuggestionsProperty); }
		set { SetValue(SuggestionsProperty, value); }
	}

	public static readonly DependencyProperty SuggestionsProperty =
		DependencyProperty.Register("Suggestions", typeof(IEnumerable<string>), typeof(TokenizingTextBoxBehavior), new PropertyMetadata(null, OnSuggestionsChanged));

	private static void OnSuggestionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is not IEnumerable<string> s)
		{
			return;
		}

		var behavior = (TokenizingTextBoxBehavior)d;
		behavior.AssociatedObject.SuggestedItemsSource = s;
	}

	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject.TextChanged += AssociatedObject_TextChanged;
	}

	protected override void OnDetaching()
	{
		AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
		base.OnDetaching();
	}

	private void AssociatedObject_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
	{
		var text = sender.Text;

		if (string.IsNullOrEmpty(text))
		{
			AssociatedObject.SuggestedItemsSource = Suggestions;
			return;
		}

		AssociatedObject.SuggestedItemsSource = Suggestions.Where(x => x.Contains(text, StringComparison.OrdinalIgnoreCase));
	}
}
