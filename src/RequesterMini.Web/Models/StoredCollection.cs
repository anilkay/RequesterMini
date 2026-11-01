namespace RequesterMini.Web.Models;

/// <summary>
/// A named group of saved requests. Unlike history, nothing evicts these — a collection is the
/// place a request goes when it is worth keeping.
/// </summary>
public sealed record StoredCollection(string Name, List<StoredRequest> Requests);
