namespace RequesterMini.Web.Models;

/// <summary>One editable row in the Headers tab.</summary>
public sealed class HeaderRow
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
