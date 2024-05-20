using CodeMechanic.FileSystem;
using CodeMechanic.Types;
using Coravel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            prod_maybe.Case<string>(some: _ =>
            {
                int seconds = Environment.GetEnvironmentVariable("SECONDS").ToInt(30);

                scheduler
                    .Schedule<TodoistInvocable>()
                    .EverySeconds(seconds);

                return _;
            }, none: () =>
            {
                int minutes = Environment.GetEnvironmentVariable("MINUTES").ToInt(15);
                scheduler
                    .Schedule<TodoistInvocable>()
                    .EverySeconds(60 * minutes);

                return "";
            });

            // scheduler
            //     .Schedule<TodoistInvocable>()
            //     .EverySeconds(30);
        });

        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITodoistSchedulerService, TodoistSchedulerService>();

                services.AddScheduler();
                services.AddTransient<MyFirstInvocable>();
                services.AddTransient<TodoistInvocable>();
            });
}