using CodeMechanic.Diagnostics;
using CodeMechanic.Systemd.Daemons;
using CodeMechanic.Types;
using Coravel.Invocable;

namespace worker1;

public class MyFirstInvocable : IInvocable
{
    public async Task Invoke()
    {
        Console.WriteLine($"Hello from {nameof(worker1)}! (updated at {DateTime.Now.Hour})");
        /// Sample MySQL logging (requires MYSQL_* .env variables to be set in your new .env).
        if (Environment.GetEnvironmentVariable("MYSQLPASSWORD").Dump("what's the password?").NotEmpty())
        {
            Console.WriteLine("Writing to railway logs ....");
            int rows = await MySQLExceptionLogger.LogInfo("Invoking from /srv/", $"{nameof(worker1)}!");
        }
    }
}