using CodeMechanic.FileSystem;
using CodeMechanic.Systemd.Daemons;
using Coravel;
using Coravel.Invocable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace blarg7;

public class MyFirstInvocable : IInvocable
{
    public async Task Invoke()
    {
        Console.WriteLine("This is my first invocable!");
        string message = "blarg7 can now invoke! from /srv!";
        int rows = await MySQLExceptionLogger.LogInfo(message, nameof(blarg7));
        // return Task.CompletedTask;
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        DotEnv.Load(debug: true);

        // await RunAsCoravelScheduler(args);

        Console.WriteLine("cool. I loaded .env");


        IHost host = CreateHostBuilder(args)
            .UseSystemd()
            .Build();

        host.Services.UseScheduler(scheduler =>
        {
            // Yes, it's this easy!
            scheduler
                .Schedule<MyFirstInvocable>()
                .EveryFiveSeconds();

            Console.WriteLine("cool. I loaded the host w/o dying...");
        });


        host.Run();
    }


    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            // services.AddHostedService<Worker>()  // from : https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux
            .ConfigureServices(services =>
            {
                services.AddScheduler();
                
                services.AddTransient<MyFirstInvocable>();

            });
}

//     private static async Task RunAsCoravelScheduler(string[] args)
//     {
//         // Changed to return the IHost
//         // builder before running it.
//         IHost host = CreateHostBuilder(args).Build();
//         host.Services.UseScheduler(scheduler =>
//             {
//                 // scheduler.Schedule<MyFirstInvocable>()
//                 //     .EveryTenSeconds()
//                 //     // .Weekday()
//                 //     ;
//
//                 // more invokables ...
//             })
//             // .OnError((exception) =>
//             //     LogException(exception)
//             // )
//             ;
//         host.Run();
//     }
//
//
//     public static IHostBuilder CreateHostBuilder(string[] args) =>
//         Host.CreateDefaultBuilder(args)
//             // .UseSystemd() // src:  https://devblogs.microsoft.com/dotnet/net-core-and-systemd/?WT.mc_id=ondotnet-c9-cephilli
//             .ConfigureServices((hostContext, services) =>
//             {
//                 services.AddScheduler();
//
//                 // services.AddSingleton<ICachedArgsService>(new CachedArgsService(args));
//
//                 // Add this 👇
//                 // services.AddTransient<MyFirstInvocable>();
//             })
//     // .ConfigureLogging((hostingContext, logging) =>
//     // {
//     //     logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
//     //     logging.AddConsole();
//     // })
//     ;
// }

/*

class Program
{
    static async Task Main(
        string[] args)
    {
        LoadEnv(debug: false);

        
        
        // int sleep = 7000;
        // if (args.Length > 0)
        // {
        //     int.TryParse(args[0], out sleep);
        // }
        //
        // while (true)
        // {
        //     Console.WriteLine($"Working, pausing for {sleep} ms");
        //     Thread.Sleep(sleep);
        //     string pwd = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
        //     // Console.WriteLine("mysql password =  " + pwd);
        //
        //     if (pwd.NotEmpty())
        //     {
        //         int rows_affected = await MySQLExceptionLogger.LogInfo("hello from " + nameof(blarg7));
        //         Console.WriteLine($"Something bad happened, so we logged {rows_affected} messasges.");
        //     }
        //     else
        //     {
        //         Console.WriteLine(
        //             "No .env file found with variable 'MYSQLPASSWORD'.  Update your local .env file with a valid MYSQLPASSWORD and restart.");
        //     }
        // }
    }

    private static void LoadEnv(bool debug = false)
    {
        bool env_exists = File.Exists(".env");
        string install_directory = Path.Combine("/srv/", nameof(blarg7));
        string env_path = Path.Combine(install_directory, ".env");
        if (debug) Console.WriteLine($"Current install directory: '{install_directory}'");
        if (debug) Console.WriteLine($".env file exists? (looking in {env_path} " + env_exists);
        var env_settings = DotEnv.Load(env_path, debug: true);
        if (debug) Console.WriteLine("env settings found in .env" + env_settings.Count);
        if (env_settings.Count > 0) env_exists.Dump(nameof(env_settings));
    }
}
*/