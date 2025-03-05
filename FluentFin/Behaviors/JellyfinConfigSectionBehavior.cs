using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.Xaml.Interactivity;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.Behaviors;

public partial class JellyfinConfigSectionBehavior : Behavior<StackPanel>
{
    [GeneratedDependencyProperty]
    public partial List<JellyfinConfigItemViewModel>? Items { get; set; }

    protected override void OnAttached()
    {
        this.WhenAnyValue(x => x.Items)
            .Where(x => x is { Count: > 0 })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(CreateItems);
    }

    private void CreateItems(List<JellyfinConfigItemViewModel>? list)
    {
        if(list is null)
        {
            return;
        }

        AssociatedObject.Children.Clear();

        foreach (var item in list)
        {
            AssociatedObject.Children.Add(CreateControl(item));
        }
    }
    
    private static UIElement CreateControl(JellyfinConfigItemViewModel model)
    {
        return model switch
        {
            JellyfinTextBlockConfigItemViewModel str => CreateTextBlock(str),
            JellyfinSelectableConfigItemViewModel cb => CreateComboBox(cb),
            JellyfinGroupedConfigItemViewModel group => CreateGroup(group),
            JellyfinConfigItemViewModel<double> number => CreateNumberBox(number),
            JellyfinConfigItemViewModel<bool> boolean => CreateToggleSwitch(boolean),
            JellyfinConfigItemViewModel<string> str => CreateTextBox(str),
            _ => throw new NotImplementedException()
        };
    }

    private static SettingsExpander CreateGroup(JellyfinGroupedConfigItemViewModel model)
    {
        var expander = new SettingsExpander
        {
            Header = model.DisplayName,
            Description = model.Description,
            IsExpanded = true
        };

        foreach (var item in model.Items)
        {
            expander.Items.Add(CreateControl(item));
        }
        return expander;
    }

    private static SettingsCard CreateTextBlock(JellyfinTextBlockConfigItemViewModel model)
    {
        var control = new TextBlock
        {
            Text = model.Value
        };

        return CreateSettingsCard(control, model);
    }

    private static SettingsCard CreateTextBox(JellyfinConfigItemViewModel<string> model)
    {
        var control = new TextBox();

        BindingOperations.SetBinding(control, TextBox.TextProperty, new Binding
        {
            Path = new PropertyPath(nameof(model.Value)),
            Source = model,
            Mode = BindingMode.TwoWay
        });

        return CreateSettingsCard(control, model);
    }

    private static SettingsCard CreateToggleSwitch(JellyfinConfigItemViewModel<bool> model)
    {
        var control = new ToggleSwitch();

        BindingOperations.SetBinding(control, ToggleSwitch.IsOnProperty, new Binding
        {
            Path = new PropertyPath(nameof(model.Value)),
            Source = model,
            Mode = BindingMode.TwoWay
        });

        return CreateSettingsCard(control, model);
    }

    private static SettingsCard CreateNumberBox(JellyfinConfigItemViewModel<double> model)
    {
        var control = new NumberBox();

        BindingOperations.SetBinding(control, NumberBox.ValueProperty, new Binding
        {
            Path = new PropertyPath(nameof(model.Value)),
            Source = model,
            Mode = BindingMode.TwoWay
        });

        return CreateSettingsCard(control, model);
    }

    private static SettingsCard CreateComboBox(JellyfinSelectableConfigItemViewModel model)
    {
        var control = new ComboBox()
        {
            ItemsSource = model.Values
        };

        if(!string.IsNullOrEmpty(model.DisplayMemberPath))
        {
            control.DisplayMemberPath = model.DisplayMemberPath;
        }

        BindingOperations.SetBinding(control, Selector.SelectedItemProperty, new Binding
        {
            Path = new PropertyPath(nameof(model.SelectedValue)),
            Source = model,
            Mode = BindingMode.TwoWay
        });

        return CreateSettingsCard(control, model);
    }

    private static SettingsCard CreateSettingsCard(object content, JellyfinConfigItemViewModel model)
    {
        return new SettingsCard
        {
            Header = model.DisplayName,
            Description = model.Description,
            Content = content
        };
    }
}