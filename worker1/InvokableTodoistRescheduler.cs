using System.Diagnostics;
using System.Text.RegularExpressions;
using CodeMechanic.Async;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Todoist;
using CodeMechanic.Types;
using Coravel.Invocable;
using Newtonsoft.Json;

namespace worker1;

public class InvokableTodoistRescheduler : IInvocable
{
    private readonly ITodoistSchedulerService todoist;

    public InvokableTodoistRescheduler(ITodoistSchedulerService svc)
    {
        todoist = svc;
    }

    public async Task Invoke()
    {
        Console.WriteLine("Starting Rescheduler Invoke");


        try
        {
            // string log_message = "Beginning Invoke at '" + DateTime.Now.ToString("o") + "'";
            // File.AppendAllText("rescheduler.log", log_message);

            var rescheduling_options =
                ConfigReader.LoadConfig<ReschedulingOptions>("rescheduling_options.json",
                    fallback: new ReschedulingOptions());
            rescheduling_options.Dump(nameof(rescheduling_options));

            var Q = new SerialQueue();
            int delay_after_task = 250;

            Stopwatch sw = Stopwatch.StartNew();
            var tasks = rescheduling_options.Reschedules
                .Where(rs => rs.enabled)
                .Select(reschedule => Q
                    .Enqueue(async () =>
                        {
                            var changed = await AutoRescheduleFilteredTasks(reschedule);
                            // Console.WriteLine("changed " + changed.Count);
                            Thread.Sleep(delay_after_task);
                            //// other crap
                        }
                    ));

            await Task.WhenAll(tasks);

            // await AutoRescheduleFilteredTasks("!recurring");  // 591
            // await AutoRescheduleFilteredTasks("overdue & !recurring"); // 45
            // await AutoRescheduleFilteredTasks("overdue & recurring"); // 85
            // await AutoRescheduleFilteredTasks("overdue"); // 130
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// AutoRescheduleFilteredTasks to a given size.
    /// Never have overdue tasks again.
    /// </summary>
    /// <param name="daily_task_limit">How large I want each day to be for reschudeled overdue tasks.  Too large, and I'll get overwhelmed.  Too small, and I'm procrastinating</param>
    /// <returns></returns>
    private async Task<List<TodoistTask>> AutoRescheduleFilteredTasks(
        Reschedule options,
        bool debug = false
    )
    {
        // int request_limit = 30;
        // int delay_after_limit = 5 * 1000;
            try
            {
                // options.Dump();
                if (!options.enabled)
                {
                    return new List<TodoistTask>(0);
                }

                if (options.filter.IsEmpty()) throw new ArgumentNullException(nameof(options));

                debug = options.debug;

                var candidates = await todoist.SearchTodos(new TodoistTaskSearch()
                {
                    filter = options.filter.AsParameterizedString(),
                });

                bool include_non_recurring =
                    options.filter.Contains(
                        "!recurring"); // if filter contains the ! before recurring, then don't allow recurring.  The API designers forgot to not mess this up.

                // if this is set, then we want all non-recurring tasks, regardless of label or overdue status...
                // bool exactly_equals_non_recurring = options.filter.Equals("!recurring");

                Console.WriteLine("Include non recurring? " + include_non_recurring);

                var any_recurring = candidates
                    .Where(x => x.due != null && !x.due.is_recurring.ToBoolean())
                    .ToList();

                // if (!include_non_recurring)
                // Console.WriteLine("TOTAL NON-RECURRING TASKS FOUND: " + any_recurring.Count);

                var today = DateTime.Now;

                // create batches based on how old each task is. If there's a tie, sort by priority.
                var filtered_candidates = candidates
                    // there should never be a null date time, but if there is, set the new due date to today, so nothing is hurt.
                    .OrderBy(x =>
                        x.due.ToMaybe().Case(some: due => due.date.ToDateTime(today), none: () => today)
                    )
                    // order by priority (fixed so 4 from the API means 1 to me... API devs... I know, right?)
                    .OrderBy(x => x.priority.FixPriorityBug().Id)
                    // I shouldn't have to filter by 'is_recurring'.
                    // I REALLY shouldn't have to...
                    // but I am b/c the Todoist API team forgot to check !recurring & overdue use case (see: https://api.todoist.com/rest/v2/tasks?todos=&label=&filter=overdue%20&%20recurring).
                    // so here it is:
                    .If(include_non_recurring // && !exactly_equals_non_recurring
                        , tasks => tasks
                            .Where(x => x.due != null
                                        && !x.due.is_recurring.ToBoolean()
                                        || x.due == null
                            )
                    )
                    // .Take(request_limit)
                    .ToList();

                // if (debug) 
                Console.WriteLine("TOTAL Filtered candidates: " + filtered_candidates.Count);
                if (filtered_candidates.Count == 0)
                {
                    Console.WriteLine("Nothing to do, so returning....");
                    return new List<TodoistTask>();
                }

                var batches = filtered_candidates.Batch(options.daily_limit);

                if (debug) Console.WriteLine("Batches made : " + batches.Count());
                List<TodoistUpdates> actual_updates = new(0);
                foreach ((var todo_batch, int index) in batches.WithIndex())
                {
                    foreach (var todo in todo_batch)
                    {
                        if (debug) Console.WriteLine($"old due date for task {todo.content}: {todo.due.date}");
                        if (debug) Console.WriteLine($"adding {index + 1} days!");
                        var updates = new TodoistUpdates()
                        {
                            id = todo.id,
                            content = todo.content,
                            description = todo.description,
                            priority = todo.priority,
                            labels = todo.labels,
                            due_date = today.AddDays(index + 1).ToString("o")
                        };

                        if (debug)
                        {
                            Console.WriteLine("new due date set to :" + updates.due_date);
                            Console.WriteLine("for task w/ priority :" + updates.priority.FixPriorityBug());
                        }

                        actual_updates.Add(updates);
                    }
                }

                // string json = JsonConvert.SerializeObject(actual_updates);
                // string logfile = "reschedule_" + today.ToString("yy-MM-dd") + ".json";
                // string current_text = File.ReadAllText(logfile);
                // if (current_text.Length >= 10000)
                //     File.Delete(logfile);
                // File.AppendAllText(logfile, json);

                // actual_updates.Take(2).Dump("sample updates for filter " + options.filter);

                Console.WriteLine($"Total updated tasks: {filtered_candidates.Count}");
                // Console.WriteLine("Total batches:" + batches.Count() );

                var updated_tasks = new List<TodoistTask>();
                // await todoist.UpdateTodos(actual_updates);
                return updated_tasks;
                // return default;
                // return candidates;
            }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // var createme = new TodoistUpdates()
    // {
    //     content = "Buy Milk zzz",
    //     due_string = "tomorrow at 12:00",
    //     priority = "4".FixPriorityBug().ToString()
    // };
    //
    // var created_todo = await todoist.CreateTodo(createme);
    // Console.WriteLine($"created todo {created_todo.content} with id:{created_todo.id}");

    // await TestDeletionById(created_todo);
}

public class ReschedulingOptions
{
    public Reschedule[] Reschedules { get; set; }
}

public class Reschedule
{
    public bool debug { get; set; } = false;
    public bool enabled { get; set; } = false;

    public string
        filter { get; set; } // e.g., 'overdue'.  see: https://todoist.com/help/articles/introduction-to-filters-V98wIH

    public int daily_limit { get; set; } = 2;
    // public bool dry_run { get; set; } = true;
}

public static class TodoistExtensions
{
    public static string AsParameterizedString(this string uri)
    {
        var updated_uri = Regex.Replace(uri, " ", "%20");
        Console.WriteLine("fixed uri\n" + updated_uri);
        return updated_uri;
    }
}