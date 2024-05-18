using Coravel.Invocable;

namespace sample_coravel_daemon;

public class MyFirstInvocable : IInvocable
{
    public Task Invoke()
    {
        Console.WriteLine("This is my first invocable!");
        return Task.CompletedTask;
    }
}