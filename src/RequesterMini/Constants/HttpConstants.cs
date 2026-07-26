using System.Collections.Generic;

namespace RequesterMini.Constants
{
    internal static class HttpConstants
    {
        internal static readonly List<string> BodyTypeValues = ["Json", "Xml", "Form", "Text"];
        internal static readonly List<string> MethodValues = ["GET", "POST", "PUT", "DELETE", "PATCH", "QUERY"];
        internal static readonly List<string> AuthTypeValues = ["None", "Basic"];
        internal static readonly string SelectedMethod = "GET";
        internal static readonly string SelectedBodyType = "Json";
        internal static readonly string SelectedAuthType = "None";
        internal static readonly string StartUrl = "https://jsonplaceholder.typicode.com/posts/1";
    }
}
