using CodeMechanic.FileSystem;
using Coravel;
using personal_daemon;
using sample_coravel_daemon;

public class Program
{
    public static async Task Main(string[] args)
    {
        DotEnv.Load(debug: true);

        await RunAsCoravelScheduler(args);
    }

    private static async Task RunAsCoravelScheduler(string[] args)
    {
        // Changed to return the IHost
        // builder before running it.
        IHost host = CreateHostBuilder(args).Build();
        host.Services.UseScheduler(scheduler =>
            {
                scheduler.Schedule<MyFirstInvocable>()
                    .EveryTenSeconds()
                    // .Weekday()
                    ;

                // more invokables ...
            })
            // .OnError((exception) =>
            //     LogException(exception)
            // )
            ;
        host.Run();
    }


    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSystemd() // src:  https://devblogs.microsoft.com/dotnet/net-core-and-systemd/?WT.mc_id=ondotnet-c9-cephilli
            .ConfigureServices((hostContext, services) =>
            {
                services.AddScheduler();

                services.AddSingleton<ICachedArgsService>(new CachedArgsService(args));

                // Add this 👇
                services.AddTransient<MyFirstInvocable>();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });
}