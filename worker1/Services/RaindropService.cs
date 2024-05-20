using CodeMechanic.FileSystem;

namespace CodeMechanic.Youtube;

public class RaindropService : IRaindropService
{
    public async Task<List<Grepper.GrepResult>> ReadRaindropCSVs(string directory)
    {
        string cwd = Directory.GetCurrentDirectory();
        string rootdir = Path.GetRelativePath(cwd, directory);

        var youtube_grepper = new Grepper()
        {
            RootPath = rootdir,
            FileSearchMask = "*.csv",
            FileNamePattern = "raindrop"
        };


        var grep_task = Task.Run(
            () =>
            {
                var results = youtube_grepper.GetMatchingFiles().ToList();
                return results;
            });

        var files = await grep_task;
        return files;
    }

    public async Task<List<RaindropBookmark>> ImportBookmarksFromCSV(string csv_dir)
    {
        var grep_results = await ReadRaindropCSVs(csv_dir);

        //todo: finish
        var bookmarks = new List<RaindropBookmark>();
        return bookmarks;
    }
}

public interface IRaindropService
{
    public Task<List<RaindropBookmark>> ImportBookmarksFromCSV(string csv_dir);
}

public class RaindropBookmark
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public string note { get; set; } = string.Empty;
    public string excerpt { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;

    public string folder { get; set; } = string.Empty;

    public string tags { get; set; } = string.Empty;
    public string created { get; set; } = string.Empty;
    public string cover { get; set; } = string.Empty;
    public string highlights { get; set; } = string.Empty;
    public string favorite { get; set; } = string.Empty;
}