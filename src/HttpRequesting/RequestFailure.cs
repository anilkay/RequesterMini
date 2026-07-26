namespace HttpRequesting;

public sealed record RequestFailure(string Message, Exception? Exception = null);
