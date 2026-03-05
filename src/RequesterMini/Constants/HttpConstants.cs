using System.Collections.Generic;

namespace RequesterMini.Constants
{
    internal static class HttpConstants
    {
        internal static readonly List<string> BodyTypeValues = ["Json", "Xml", "Form", "Text"];
        internal static readonly List<string> MethodValues = ["GET", "POST", "PUT", "DELETE", "PATCH"];
        internal static readonly string SelectedMethod = "GET";
        internal static readonly string SelectedBodyType = "Json";
        internal static readonly string StartUrl = "https://jsonplaceholder.typicode.com/posts/1";

        internal static readonly List<string> AuthTypeValues = ["None", "Bearer Token", "Basic"];
        internal const string DefaultAuthType = "None";
        internal const string AuthTypeBearerToken = "Bearer Token";
        internal const string AuthTypeBasic = "Basic";
    }
}
