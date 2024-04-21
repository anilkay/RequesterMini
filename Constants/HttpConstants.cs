using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequesterMini.Constants
{
    internal static class HttpConstants
    {
        internal static readonly List<string> BodyTypeValues = new List<string> { "Json", "Xml", "Form", "Text" };
        internal static readonly List<string> MethodValues = new List<string> { "GET", "POST", "PUT", "DELETE", "PATCH" };
        internal static readonly string SelectedMethod = "GET";
        internal static readonly string SelectedBodyType = "Json";
        internal static readonly string StartUrl = "https://jsonplaceholder.typicode.com/posts/1";
    }
}
