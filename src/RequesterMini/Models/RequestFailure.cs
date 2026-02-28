using System;

namespace RequesterMini.Models;

public sealed record RequestFailure(string Message, Exception? Exception = null);
