using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicMS.Web.Services.Api;

/// <summary>camelCase JSON for bridging server-fetched API data into page-inline &lt;script&gt; blocks
/// consumed by the existing (camelCase-authored) feature JS files.</summary>
public static class ViewJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
