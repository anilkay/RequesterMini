using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace RequesterMini.ViewModels;


public record OldRequestDto(string Method, string Url, string Body, string ResponseStatusCode, string ResponseBody)
{
   
}


public class OldRequestsWindowViewModel : ViewModelBase
{
    public ObservableCollection<OldRequestDto> OldRequests { get;} = new ObservableCollection<OldRequestDto>();
    public  OldRequestsWindowViewModel()
    {
        MessageBus.Current.Listen<string>(Constants.MessageBusConstants.NewRequest).Subscribe(value =>
        {
            if (value == null)
            {
                return;
            }
            try
            {
                var oldRequestJson = Newtonsoft.Json.Linq.JToken.Parse(value);

                OldRequestDto oldRequestDto = new OldRequestDto(oldRequestJson.Value<string>("Method"), oldRequestJson.Value<string>("Url"),"","","");

                OldRequests.Add(oldRequestDto);
            }
            catch(Exception ex)
            {
                var dto = new OldRequestDto("Get",ex.ToString(),"","","");
                OldRequests.Add(dto);
            }
        });
    }

    



   
}