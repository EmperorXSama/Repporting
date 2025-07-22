namespace RepportingApp.ViewModels.Components;

// SelectableEmailGroup wrapper class
public partial class SelectableEmailGroup : ObservableObject
{
    public EmailGroup Group { get; set; }
    
    [ObservableProperty] private bool _isSelected;
    
    public string GroupName => Group.GroupName;
    public int EmailCount => Group.EmailCount;
    public int? GroupId => Group.GroupId;

    public event Action<SelectableEmailGroup, bool>? SelectionChanged;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke(this, value);
    }
}