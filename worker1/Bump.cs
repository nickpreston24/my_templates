namespace worker1;

public record Bump
{
    public bool enabled { get; set; } = false;
    public int wait_minutes { get; set; } = 5;
    public int wait_seconds { get; set; } = 10;
}