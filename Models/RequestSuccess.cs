using System;

namespace RequesterMini.Models;

public sealed record RequestSuccess(string StatusCode, string ResponseBody, DateTime? FinishedTimeUtc, TimeSpan? RequestTime);
