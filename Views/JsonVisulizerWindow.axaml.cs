using Avalonia.Controls;
using RequesterMini.ViewModels;
namespace RequesterMini.Views;
public partial class JsonVisulizerWindow :  UserControl
{
    public JsonVisulizerWindow()
    {
        InitializeComponent();
        DataContext = new JsonVisulizerWindowViewModel();
    }
}