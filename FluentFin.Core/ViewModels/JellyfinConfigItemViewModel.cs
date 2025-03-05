using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentFin.Core.ViewModels;

public abstract class JellyfinConfigItemViewModel : ObservableObject
{
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Glyph { get; set; } = "";
    public bool IsVisible { get; set; } = true;

    public abstract void Save();
    public abstract void Reset();
}

public class JellyfinConfigItemViewModel<T> : JellyfinConfigItemViewModel
{
    private readonly Action<T> _setValue;
    private readonly Func<T> _getValue;

    public T Value
    {
        get => field;
        set
        {
            if (field?.Equals(value) == true)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public JellyfinConfigItemViewModel(Func<T> getValue, Action<T> setValue)
    {
        _setValue = setValue;
        _getValue = getValue;

        Value = getValue();
    }

    public override void Save() => _setValue(Value);
    public override void Reset() => Value = _getValue();
}

public partial class JellyfinSelectableConfigItemViewModel : JellyfinConfigItemViewModel
{
    private readonly Action<object?> _setValue;
    private readonly Func<object?> _getValue;

    public object? SelectedValue
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            OnPropertyChanged();
        }
    }
    public IEnumerable<object> Values { get; set; } = [];

    public string DisplayMemberPath { get; set; }

    public JellyfinSelectableConfigItemViewModel(Func<object?> getValue, Action<object?> setValue, IEnumerable<object> values, string displayMemberPath = "")
    {
        _setValue = setValue;
        _getValue = getValue;

        Values = values;
        SelectedValue = getValue();
        DisplayMemberPath = displayMemberPath;
    }

    public override void Save() => _setValue(SelectedValue);
    public override void Reset() => SelectedValue = _getValue();
}

public class JellyfinTextBlockConfigItemViewModel(Func<string> getValue, Action<string> setValue) : JellyfinConfigItemViewModel<string>(getValue, setValue) { }

public class JellyfinGroupedConfigItemViewModel : JellyfinConfigItemViewModel
{
    public List<JellyfinConfigItemViewModel> Items { get; set; } = [];

    public override void Save()
    {
        foreach (var child in Items)
        {
            child.Save();
        }
    }

    public override void Reset()
    {
        foreach (var child in Items)
        {
            child.Reset();
        }
    }
}
