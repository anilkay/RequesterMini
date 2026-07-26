namespace RequesterMini.Web.Models;

/// <summary>One editable row in the Query Params tab. Enabled rows are mirrored into the URL.</summary>
public sealed class QueryParamRow
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
