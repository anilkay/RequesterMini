using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RequesterMini.ViewModels;

public record RequestCollectionDto(string Name, List<OldRequestDto> Requests);

public class CollectionItem
{
    public CollectionItem(string name, IEnumerable<OldRequestDto>? requests = null)
    {
        Name = name;
        Requests = requests is null ? [] : new ObservableCollection<OldRequestDto>(requests);
    }

    public string Name { get; }

    public ObservableCollection<OldRequestDto> Requests { get; }

    internal RequestCollectionDto ToDto() => new(Name, Requests.ToList());
}
