namespace worker1;

public class TodoistUpdates
{
    public string priority { get; set; } = string.Empty;
    public string content { set; get; } = string.Empty;
    public string due_date { set; get; } = string.Empty;
    public string[] labels { get; set; } = Array.Empty<string>();
    public string id { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string due_string { get; set; } = null;
}