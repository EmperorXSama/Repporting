namespace RepportingApp.ViewModels.BaseServices;

public interface ILoadableViewModel
{
    Task LoadDataIfFirstVisitAsync();
}