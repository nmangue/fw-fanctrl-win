using System.ComponentModel.DataAnnotations;

namespace FrameworkFanControl;

public record FanControlCurvePoint(float Temp, int Speed);

public class FanControlSettings
{
    public const string SectionName = "FanControl";

    [Range(typeof(TimeSpan), "00:00:01", "00:00:30")]
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

    [Range(1, 64)]
    public int MovingAverageWidth { get; set; } = 4;

    [Required]
    public required List<FanControlCurvePoint> ControlCurve { get; set; }
}
