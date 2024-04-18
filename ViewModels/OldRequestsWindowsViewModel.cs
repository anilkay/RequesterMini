using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace RequesterMini.ViewModels;

public class OldRequestsWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> OldRequests { get;} = new ObservableCollection<string>();
    public  OldRequestsWindowViewModel()
    {
        MessageBus.Current.Listen<string>("newrequest").Subscribe(value =>
        {
            if (value == null)
            {
                return;
            }
            OldRequests.Add(value);
        });
    }

    



   
}