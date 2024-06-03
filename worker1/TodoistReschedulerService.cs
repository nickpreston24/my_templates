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
    /// Bumps any tasks marked with 'bump' to the specified days in the description.
    /// </summary>
    /// <returns></returns>
    public async Task<List<TodoistTask>> BumpTasks(int days = 7)
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
                                        .FirstOrNullObject(
                                            new BumpTime { unit = "d", value = days }
                                                .Dump("falling back to bump time")
                                        )
                                ).days.Dump("days")
                            )
                            .ToString("o")
                            .Dump("due_date")
                }
            ).ToList();

        Console.WriteLine($"performing {actual_updates.Count} updates");

        if (debug)
            actual_updates.Dump("Actual updates");

        if (debug)
            actual_updates.Select(x => x?.due_string).Dump("new due dates");

        var updated_tasks = await UpdateTodos(actual_updates);

        return updated_tasks;
    }

    public async Task<List<TodoistTask>> SearchTodos(TodoistTaskSearch search)
    {
        if (search == null) throw new ArgumentNullException(nameof(search));

        string joined_ids = string.Join(",", search.ids);
        string label = search.label.Dump("labels");
        string filter = search.filter;
        string project_id = search.project_id;

        // string uri = "https://api.todoist.com/rest/v2/tasks";
        string uri = new StringBuilder("https://api.todoist.com/rest/v2/tasks?")
            .AppendIf((_) => joined_ids.NotEmpty() && joined_ids.Length >= 1, $"todos={joined_ids}&")
            .AppendIf((_) => label.NotEmpty(), $"label={label}&")
            .AppendIf((_) => filter.NotEmpty(), $"filter={filter}&")
            .AppendIf((_) => project_id.NotEmpty(), $"project_id={project_id}&")
            .RemoveFromEnd(1)
            .ToString();

        Console.WriteLine($"{nameof(SearchTodos)}() uri " + uri);
        var content = await GetContentAsync(uri, api_key, debug: false);
        var todos = JsonConvert.DeserializeObject<List<TodoistTask>>(content);
        Console.WriteLine("total todos:>> " + todos.Count);
        return todos;

        // string sample_json = JsonConvert.SerializeObject(new TodoistUpdates() { labels = "fullday".AsArray() });

        // var ids = todos.SelectMany(t => t.id);
        // string joined_ids = string.Join(",", ids);
        //    string uri = !(todos.Length > 0)
        //        ? "https://api.todoist.com/rest/v2/tasks": 
        //        $"https://api.todoist.com/rest/v2/tasks?todos={joined_ids}&label={label}";
        //    Console.WriteLine("uri :>> " + uri);
    }

    private async Task<string> GetContentAsync(
        string uri
        , string bearer_token
        , string json = ""
        , bool debug = false)
    {
        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer_token);

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (json.NotEmpty())
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        var response = await http.SendAsync(request);
        // var response = await http.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        // if (debug)
        //     Console.WriteLine("content :>> " + content);
        return content;
    }

    public async Task<List<TodoistTask>> UpdateTodos(List<TodoistUpdates> todoist_updates, int delay_in_seconds = 5)
    {
        try
        {
            if (todoist_updates.Count == 0) return Enumerable.Empty<TodoistTask>().ToList();
            var Q = new SerialQueue();

            var updated_todos = new List<TodoistTask>();
            Stopwatch sw = Stopwatch.StartNew();
            var tasks = todoist_updates
                .Select(update => Q
                    .Enqueue(async () =>
                        {
                            var updates = await PerformUpdate(update);
                            updated_todos.AddRange(updates);
                        }
                    ));

            await Task.WhenAll(tasks);

            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            // todo: aggregate all the awaited tasks and their results, then return them.
            return updated_todos;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<TodoistTask> CreateTodo(TodoistUpdates todo)
    {
        try
        {
            bool debug = true;
            string json = JsonConvert.SerializeObject(todo);
            Console.WriteLine("raw json updates :>> " + json);

            string uri = "https://api.todoist.com/rest/v2/tasks";
            Console.WriteLine("update uri :>> " + uri);

            var content = await RunPost(uri, json);
            if (debug)
                Console.WriteLine("content :>> " + content);
            // var result = JsonConvert.DeserializeObject<TodoistTask>(content);
            // todo :Fix the faulty deserialization.
            return new TodoistTask(); // temporary
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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

        var content = await RunPost(uri, json);
        if (debug)
            Console.WriteLine("content :>> " + content);
        // response.Dump(nameof(response));
        // return JsonConvert.DeserializeObject<List<TodoistTask>>(content);
        return new List<TodoistTask>();
    }


    public async Task<TodoistTask> DeleteTodo(string id)
    {
        try
        {
            bool debug = true;
            if (id.IsEmpty()) throw new ArgumentNullException(nameof(id));
            if (!id.IsNumeric())
                throw new ArgumentException("a Todoist task id must be numeric");

            var doomed_todos = await GetTodosById(id);

            if (debug)
                doomed_todos.Dump(nameof(doomed_todos));

            string uri = $"https://api.todoist.com/rest/v2/tasks/{id}";

            using HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);
            var request = new HttpRequestMessage(HttpMethod.Delete, uri);
            var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // var deleted_todo = JsonConvert.DeserializeObject<TodoistTask>(content);
            // return deleted_todo;
            return new TodoistTask().With(tt => { tt.id = id; });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<TodoistTask>> GetTodosById(string id)
    {
        string uri = $"https://api.todoist.com/rest/v2/tasks/{id}";
        string json = await RunGet(uri);
        Console.WriteLine("found todo: " + json);
        // TODO: fix deser...
        // return JsonConvert.DeserializeObject<List<TodoistTask>>(json) ?? new List<TodoistTask>(0);
        return new TodoistTask() { id = id }.AsList();
    }

    private async Task<string> RunGet(string uri)
    {
        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = await http.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    private async Task<string> RunPost(string uri, string json, bool debug = false)
    {
        using HttpClient http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await http.SendAsync(request);
        if (debug)
            response.Dump(nameof(response));

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return content;
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

public interface ITodoistSchedulerService
{
    Task<List<TodoistTask>> BumpTasks(int days = 7);

    Task<List<TodoistTask>> SearchTodos(TodoistTaskSearch search);
    Task<TodoistTask?> CreateTodo(TodoistUpdates todo);
    Task<TodoistTask> DeleteTodo(string id);
    Task<List<TodoistTask>> GetTodosById(string id);
    Task<List<TodoistTask>> UpdateTodos(List<TodoistUpdates> todoist_updates, int delay_in_seconds = 5);
}