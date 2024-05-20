using System.Text.RegularExpressions;
using CodeMechanic.Types;

namespace CodeMechanic.Youtube;

public class YoutubeRegexPattern : Enumeration
{
    public static YoutubeRegexPattern Link = new YoutubeRegexPattern(1, nameof(Link), @"\s*https://www.youtube.com.*");

    public YoutubeRegexPattern(int id, string name, string pattern) : base(id, name)
    {
        this.CompiledPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        this.RawPattern = pattern;
    }

    public string RawPattern { get; }

    public Regex CompiledPattern { get; }
}