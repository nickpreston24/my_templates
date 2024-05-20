using System.Diagnostics;
using System.Globalization;
using CodeMechanic.Diagnostics;
using CodeMechanic.Systemd.Daemons;
using CodeMechanic.Types;
using Coravel.Invocable;

namespace worker1;

public class TodoistInvocable : IInvocable
{
    private readonly ITodoistSchedulerService todoist;

    public TodoistInvocable(ITodoistSchedulerService svc)
    {
        todoist = svc;
    }

    public async Task Invoke()
    {
        string message = $"Attempting todoist processing ({DateTime.Now.ToString(CultureInfo.InvariantCulture)})";
        await MySQLExceptionLogger.LogInfo(message, nameof(worker1));

        // await BumpLabeledTasks(7);
        await CreateFullWeek();
    }

    private async Task CreateFullWeek()
    {
        var candidates = await todoist.SearchTodos(new TodoistTaskSearch()
        {
        });
        candidates
            .Where(x => !x?.due?.is_recurring.ToBoolean(false) ?? false)
            .Take(7)
            .Dump("candidates for full week")
            ;
    }

    private async Task BumpLabeledTasks(int days)
    {
        var watch = Stopwatch.StartNew();
        var bump_to = DateTime.Now.AddDays(days);
        var rescheduled_todos = await todoist.BumpTasks(bump_to);
        watch.Stop();

        string update_message =
            $"Rescheduling complete. {rescheduled_todos.Count} todos were bumped.\n Completed in {watch.ElapsedMilliseconds} milliseconds.";
        Console.WriteLine(update_message);
        await MySQLExceptionLogger.LogInfo(update_message, nameof(worker1));
    }
}