
namespace UnarchivedStreamRecorder.Utilities.Extensions;

using Newtonsoft.Json.Linq;

public static class JsonExtensions
{
    public static JObject AddIfNotNull(this JObject obj, string propertyName, object? value)
    {
        if (value != null)
        {
            obj.Add(propertyName, JToken.FromObject(value));
        }

        return obj;
    }
}
