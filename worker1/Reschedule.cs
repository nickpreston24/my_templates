namespace worker1;

public class Reschedule
{
    public string name { get; set; } = string.Empty;
    public int task_limit { get; set; } = 5; // the maximum I wanna read from the API.
    public bool debug { get; set; } = false;
    public bool enabled { get; set; } = false;

    public string
        filter { get; set; } // e.g., 'overdue'.  see: https://todoist.com/help/articles/introduction-to-filters-V98wIH

    public int daily_limit { get; set; } = 2;

    public bool dry_run { get; set; } // if set, we don't commit the updates at all.
    public bool use_cache { get; set; } = false; // if set, load the locally cached .json file if there exists one.
}