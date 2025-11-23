namespace FrameworkFanControl.Domain;

public sealed record Percentage
{
	public int Value { get; }

	public Percentage(int value)
	{
		Value = Math.Max(0, Math.Min(100, value));
	}

	public static Percentage FromFraction(double fraction) => new((int)Math.Round(fraction * 100));

	public double ToFraction() => Value / 100.0;

	public override string ToString() => $"{Value}%";

	public static implicit operator int(Percentage p) => p.Value;

	public static implicit operator Percentage(int value) => new(value);
}
