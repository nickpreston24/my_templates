using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.RazorHAT.Services;
using CodeMechanic.Types;
using Coravel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace worker1;

public class Program
{
    public static async Task Main(string[] args)
    {
        DotEnv.Load();
        IHost host = CreateHostBuilder(args)
            .UseSystemd()
            .Build();

        host.Services.UseScheduler(scheduler =>
            {
                var prod_maybe = Environment.GetEnvironmentVariable("MODE").ToMaybe();

                var settings = ConfigReader
                    .LoadConfig<WorkerSettings>("worker_settings.json"
                        , fallback: new WorkerSettings());
                // settings.Dump(nameof(settings));

                /** SMART OVERDUE TASKS RESCHEDULER **/

                scheduler
                    .Schedule<InvokableTodoistRescheduler>()
                    // .EveryMinute()
                    .Cron("00 9,13,20 * * *")
                    .RunOnceAtStart()
                    .PreventOverlapping(nameof(InvocableTodoistBumper));

                if (settings.bump.enabled)
                    /** AUTO BUMPER */

                    prod_maybe.Case<string>(some: _ =>
                    {
                        scheduler
                            .Schedule<InvocableTodoistBumper>()
                            .EverySeconds(settings.bump.wait_seconds)
                            .PreventOverlapping(nameof(InvokableTodoistRescheduler));
                        return _;
                    }, none: () =>
                    {
                        scheduler
                            .Schedule<InvocableTodoistBumper>()
                            .EverySeconds(60 * settings.bump.wait_minutes)
                            .PreventOverlapping(nameof(InvokableTodoistRescheduler))
                            ;

                        return "";
                    });
            })
            .OnError((exception) => LogExceptionToDB(exception));

        host.Run();
    }

    private static async Task LogExceptionToDB(Exception exception)
    {
        await TemporaryExceptionLogger.LogException(exception);
    }


    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITodoistSchedulerService, TodoistSchedulerService>();

                services.AddScheduler();
                services.AddTransient<MyFirstInvocable>();
                services.AddTransient<InvocableTodoistBumper>();
                services.AddTransient<InvokableTodoistRescheduler>();
            });
}

public class WorkerSettings
{
    public Bump bump { get; set; }
}

public record Bump
{
    public bool enabled { get; set; } = false;
    public int wait_minutes { get; set; } = 5;
    public int wait_seconds { get; set; } = 10;
}

public static class ConfigReader
{
    public static T LoadConfig<T>(string filename, T fallback)
    {
        string cwd = Directory.GetCurrentDirectory();
        string file_path = Path.Combine(cwd, filename);
        // Console.WriteLine("file path : " + file_path);
        Console.WriteLine($"config for {typeof(T).Name} " + file_path);
        string json = file_path.NotEmpty() ? File.ReadAllText(file_path) : string.Empty;
        Console.WriteLine("raw json: " + json);
        var settings = json.Length > 0 ? JsonConvert.DeserializeObject<T>(json) : fallback;
        return settings;
    }
}