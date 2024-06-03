namespace worker1;

public record BumpTime
{
    public string unit { get; set; } = string.Empty;
    public int value { get; set; }

    public int days => (years * 365) + (months * 30) + (weeks * 7) + (unit.Equals("d") ? value : 0);
    public int weeks => unit.Equals("w") ? value : 0;
    public int months => unit.Equals("mo") ? value : 0;
    public int years => unit.Equals("y") ? value : 0;
}