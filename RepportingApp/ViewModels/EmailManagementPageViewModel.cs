namespace RepportingApp.ViewModels;

public partial class EmailManagementPageViewModel: ViewModelBase
{
    private readonly IApiConnector _apiConnector;
    [ObservableProperty]
    private string _emailInput; 
    [ObservableProperty]
    private string _errorMessage;
    public EmailManagementPageViewModel(IMessenger messenger,IApiConnector apiConnector) : base(messenger)
    {
        _apiConnector = apiConnector;
    }
    
    [RelayCommand]
    private async Task DeleteEmailsAsync()
    {
        try
        {
            ErrorMessage = String.Empty;
            await _apiConnector.PostDataObjectAsync<object>(ApiEndPoints.DeleteEmails,EmailInput);
            EmailInput = string.Empty;
        }
        catch (Exception e)
        {
          ErrorMessage = $"Error: {e.Message}";
        }
      
    }
}