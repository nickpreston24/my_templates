using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CodeMechanic.Advanced.Regex;
using CodeMechanic.Async;
using CodeMechanic.Diagnostics;
using CodeMechanic.Todoist;
using CodeMechanic.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace worker1;

public class TodoistSchedulerService : ITodoistSchedulerService
{
    private readonly string api_key = string.Empty;

    public TodoistSchedulerService()
    {
        api_key = Environment.GetEnvironmentVariable("TODOIST_API_KEY");
    }


    /// <summary>
    /// Bumps any tasks marked with 'bump' to the specified day.
    /// </summary>
    /// <returns></returns>
    public async Task<List<TodoistTask>> BumpTasks(DateTime bump_date)
    {
        bool debug = false;
        var today = DateTime.Now;

        var bump_search = new TodoistTaskSearch()
        {
            label = "bump"
        };

        var todos_marked_bump = (await SearchTodos(bump_search)).ToArray();

        if (todos_marked_bump.Length == 0)
        {
            Console.WriteLine("nothing needs bumping. Returning.");
            return new List<TodoistTask>();
        }

        if (debug)
            todos_marked_bump.Select(x => x.labels).Dump("current labels");


        // var now = DateTime.Now;
        // var tomorrow = now.AddDays(1);
        // Console.WriteLine("old due date :>> " + first?.due?.date);
        // string updated_date = bump_date.ToString("yyyy-MM-dd");
        // string humanized_date = tomorrow.Humanize();
        // Console.WriteLine("humanized date :>> " + humanized_date);


        // Console.WriteLine("friendly_date :>> " + friendly_date);

        // todos.Select(x => x.id.Dump("id")).Dump("all ids");
        var actual_updates = todos_marked_bump
            .Select(todo => new TodoistUpdates()
                {
                    id = todo.id,
                    description = todo.description,
                    labels = todo.labels.Where(l => !l.Equals("bump", StringComparison.OrdinalIgnoreCase)).ToArray(),

                    due_date =
                        DateTime.Now
                            .AddDays(
                                (
                                    todo.description
                                        .Extract<BumpTime>(@"bump:(?<value>\d+)(?<unit>\w{1,5})")
                                        .FirstOrDefault().Dump("bump time") ?? new() { unit = "d", value = 5 }
                                ).days.Dump("days")
                            )
                            .ToString("o")
                            .Dump("due_date")

                    //"2024-09-01T12:00:00.000000Z"
                    // due_string = //todo?.due?.due_string ??

                    //         .Dump("days added")
                    //         ).ToFriendlyDateString()
                    //         .Dump("friendly ds")
                }
                // .With(update =>
                // {
                //     // need to do this after bump time is set:
                //     // string friendly_date = (update.bump_time.value.NotEmpty() && update.bump_time.unit.NotEmpty())
                //     //     ? GetBumpedTime(update.bump_time, today).ToFriendlyDateString()
                //     //     : bump_date.ToFriendlyDateString();
                //
                //     // Console.WriteLine("Bumping todos to date " + friendly_date);
                //
                //     // GetBumpedTime(update.bump_time);
                //     update.bump_time.Dump("calc");
                //
                //     update.due_string = todo?.due?.due_string ?? bump_date.ToFriendlyDateString();
                // })
            ).ToList();

        Console.WriteLine($"performing {actual_updates.Count} updates");

        if (debug)
            actual_updates.Dump("Actual updates");

        if (debug)
            actual_updates.Select(x => x?.due_string).Dump("new due dates");

        var updated_tasks = await UpdateTodos(actual_updates);

        return updated_tasks;
    }

    // private DateTime GetBumpedTime(BumpTime updateBumpTime)
    // {
    //     
    //     
    //     
    //     // switch (updateBumpTime.unit)
    //     // var weeks = new string[] { "w", "week", "weeks" };
    //     // updateBumpTime.Dump("bump time");
    //     // if (weeks.Contains(updateBumpTime.unit, StringComparer.InvariantCultureIgnoreCase))
    //     // {
    //     //     return today.AddDays(7 * updateBumpTime.value.ToInt());
    //     //     // case "d" or "days" or "day":
    //     //     // default:
    //     // }
    //
    //     // return today.AddDays(updateBumpTime.value.ToInt());
    // }

    public async Task<List<TodoistTask>> SearchTodos(TodoistTaskSearch search)
    {
        // var ids = todos.SelectMany(t => t.id);
        // string joined_ids = string.Join(",", ids);
        //    string uri = !(todos.Length > 0)
        //        ? "https://api.todoist.com/rest/v2/tasks": 
        //        $"https://api.todoist.com/rest/v2/tasks?todos={joined_ids}&label={label}";
        //    Console.WriteLine("uri :>> " + uri);

        string joined_ids = string.Join(",", search.ids);
        string label = search.label;

        string uri = $"https://api.todoist.com/rest/v2/tasks?todos={joined_ids}&label={label}";

        var content = await GetContentAsync(uri, api_key, false);
        var todos = JsonConvert.DeserializeObject<List<TodoistTask>>(content);
        Console.WriteLine("total todos:>> " + todos.Count);
        return todos;
    }

    private async Task<string> GetContentAsync(string uri, string bearer_token, bool debug = false)
    {
        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer_token);
        var response = await http.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        // if (debug)
        //     Console.WriteLine("content :>> " + content);
        return content;
    }

    private async Task<List<TodoistTask>> UpdateTodos(List<TodoistUpdates> todoist_updates, int delay_in_seconds = 5)
    {
        if (todoist_updates.Count == 0) return Enumerable.Empty<TodoistTask>().ToList();
        var Q = new SerialQueue();

        Stopwatch sw = Stopwatch.StartNew();
        var tasks = todoist_updates
            .Select(update => Q
                .Enqueue(async () => { await PerformUpdate(update); }
                ));

        await Task.WhenAll(tasks);

        sw.Stop();
        Console.WriteLine(sw.Elapsed);

        return default;
    }

    private async Task<List<TodoistTask>> PerformUpdate(TodoistUpdates todo)
    {
        bool debug = true;
        if (todo.id.IsEmpty())
            throw new ArgumentNullException(nameof(todo.id) + " must not be empty!");

        string json = JsonConvert.SerializeObject(todo);
        Console.WriteLine("raw json updates :>> " + json);

        string uri = "https://api.todoist.com/rest/v2/tasks/$task_id".Replace("$task_id", todo.id);
        Console.WriteLine("update uri :>> " + uri);

        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await http.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        if (debug)
            Console.WriteLine("content :>> " + content);
        // response.Dump(nameof(response));
        return JsonConvert.DeserializeObject<List<TodoistTask>>(content);
    }

    sealed class ExcludeCalculatedResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = _ => ShouldSerialize(member);
            return property;
        }

        internal static bool ShouldSerialize(MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            if (propertyInfo == null)
            {
                return false;
            }

            if (propertyInfo.SetMethod != null)
            {
                return true;
            }

            var getMethod = propertyInfo.GetMethod;
            return Attribute.GetCustomAttribute(getMethod, typeof(CompilerGeneratedAttribute)) != null;
        }
    }

    sealed class WritablePropertiesOnlyResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => p.Writable).ToList();
        }
    }
}

public record BumpTime
{
    public string unit { get; set; } = string.Empty;
    public int value { get; set; }

    public int days => (years * 365) + (months * 30) + (weeks * 7) + (unit.Equals("d") ? value : 0);
    public int weeks => unit.Equals("w") ? value : 0;
    public int months => unit.Equals("mo") ? value : 0;
    public int years => unit.Equals("y") ? value : 0;
}

public class TodoistTaskSearch
{
    public string[] ids { get; set; } = Array.Empty<string>();
    public string label { get; set; } = string.Empty;
}

public class TodoistUpdates
{
    public string due_date { set; get; } = string.Empty;
    public string[] labels { get; set; } = Array.Empty<string>();
    public string id { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string due_string { get; set; } = null;
}

public interface ITodoistSchedulerService
{
    Task<List<TodoistTask>> BumpTasks(DateTime bump_date);

    // Task<List<TodoistTask>> UpdateTodos(List<TodoistTask> todos);
    Task<List<TodoistTask>> SearchTodos(TodoistTaskSearch search);
}