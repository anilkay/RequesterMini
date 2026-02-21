using Avalonia.Controls;
using RequesterMini.ViewModels;
namespace RequesterMini.Views;
public partial class JsonVisualizerWindow :  UserControl
{
    public JsonVisualizerWindow()
    {
        InitializeComponent();
        DataContext = new JsonVisualizerWindowViewModel();
    }
}