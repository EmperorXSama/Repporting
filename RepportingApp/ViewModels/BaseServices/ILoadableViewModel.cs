namespace RepportingApp.ViewModels.BaseServices;

public interface ILoadableViewModel
{
    bool IsLoadNext { get; }
    Task LoadDataIfFirstVisitAsync(bool ignoreCach = false);
}   