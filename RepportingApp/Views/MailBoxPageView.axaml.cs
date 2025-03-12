using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RepportingApp.Views;

public partial class MailBoxPageView : UserControl
{
    public MailBoxPageView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MailBoxPageViewModel>();
    }
}