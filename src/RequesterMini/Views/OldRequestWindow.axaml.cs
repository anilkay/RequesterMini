using Avalonia.Controls;
using RequesterMini.ViewModels;
namespace RequesterMini.Views;
public partial class OldRequestWindow:  UserControl
{
    public OldRequestWindow()
    {
        InitializeComponent();
        DataContext = new OldRequestsWindowViewModel();
    }
}