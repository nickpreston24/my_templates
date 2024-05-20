using System.Diagnostics;
using System.Globalization;
using CodeMechanic.Diagnostics;
using CodeMechanic.Systemd.Daemons;
using CodeMechanic.Todoist;
using CodeMechanic.Types;
using Coravel.Invocable;

namespace worker1;

public class InvocableTodoistBumper : IInvocable
{
    private readonly ITodoistSchedulerService todoist;

    public InvocableTodoistBumper(ITodoistSchedulerService svc)
    {
        todoist = svc;
    }

    public async Task Invoke()
    {
        string message = $"Attempting todoist processing ({DateTime.Now.ToString(CultureInfo.InvariantCulture)})";
        await MySQLExceptionLogger.LogInfo(message, nameof(worker1));

        await BumpLabeledTasks(7);
    }

    private async Task TestDeletionById(TodoistTask created_todo)
    {
        var deleted_task = await todoist.DeleteTodo(created_todo.id);
        Console.WriteLine($"deleted todo {deleted_task.content} with id:{deleted_task.id}");
    }

    private async Task BumpLabeledTasks(int days)
    {
        var watch = Stopwatch.StartNew();
        var rescheduled_todos = await todoist.BumpTasks(14);
        watch.Stop();

        string update_message =
            $"Rescheduling complete. {rescheduled_todos.Count} todos were bumped.\n Completed in {watch.ElapsedMilliseconds} milliseconds.";
        Console.WriteLine(update_message);
        await MySQLExceptionLogger.LogInfo(update_message, nameof(worker1));
    }
}