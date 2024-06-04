using System.Text.RegularExpressions;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Types;

namespace worker1;

public static class TodoistExtensions
{
    public static string AsParameterizedString(this string filter)
    {
        var replacements = new Dictionary<string, string>()
        {
            [" "] = "%20",
            [@"\("] = "%28",
            [@"\)"] = "%29",
            // ["|"] = "%7C",
            ["@"] = "%40",
            ["#"] = "%23",
        };

        var updated_filter = filter.AsArray().ReplaceAll(replacements).FlattenText();
        // var updated_filter = Regex.Replace(filter, " ", "%20");
        Console.WriteLine("parameterized filter\n" + updated_filter);
        return updated_filter;
    }
}