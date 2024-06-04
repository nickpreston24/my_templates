using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;

namespace CodeMechanic.Youtube;

public class YoutubeService : IYoutubeService
{
    public YoutubeService()
    {
        Console.WriteLine(nameof(YoutubeService));
    }

    public async Task<Dictionary<string, List<Grepper.GrepResult>>> FindAllYoutubeLinks(
        string base_directory,
        bool debug_mode = false,
        params string[] subfolder_patterns)
    {
        if (debug_mode)
            Console.WriteLine("Looking in base directory :>> " + base_directory);
        var base_directory_di = base_directory.AsDirectory();

        var grepResults = new ConcurrentDictionary<string, List<Grepper.GrepResult>>();

        foreach (var pattern in subfolder_patterns)
        {
            var rgx = new Regex(pattern);
            await foreach (var dir in base_directory_di
                               .DiscoverDirectories(rgx))
            {
                if (debug_mode)
                    Console.WriteLine("regex root dir:>>" + dir);

                var youtube_grepper = new Grepper()
                {
                    RootPath = dir,
                    FileSearchLinePattern = YoutubeRegexPattern.Link.RawPattern
                };

                var files = youtube_grepper.GetMatchingFiles().ToList();
                // grepResults.Add(dir, files);

                grepResults.TryAdd(dir, files);
            }
        }

        if (debug_mode)
            grepResults
                .Select(r => r.Key)
                .Dump("all subfolders found by regex");

        return grepResults.ToDictionary();
    }

    public async Task<Dictionary<string, List<Grepper.GrepResult>>> FindAllYoutubeLinks(string base_directory,
        params string[] subfolder_patterns)
    {
        return await FindAllYoutubeLinks(base_directory, false, subfolder_patterns);
    }

    public async IAsyncEnumerable<List<YoutubeLink>> ExtractAllYoutubeLinks(string[] lines)
    {
        foreach (var line in lines)
        {
            yield return line.Extract<YoutubeLink>(YoutubeRegexPattern.Link.CompiledPattern);
        }
    }
}

public interface IYoutubeService
{
    Task<Dictionary<string, List<Grepper.GrepResult>>> FindAllYoutubeLinks(
        string base_directory
        , bool debug_mode
        , params string[] subfolder_patterns);

    Task<Dictionary<string, List<Grepper.GrepResult>>> FindAllYoutubeLinks(
        string base_directory
        , params string[] subfolder_patterns);

    IAsyncEnumerable<List<YoutubeLink>> ExtractAllYoutubeLinks(string[] lines);
}