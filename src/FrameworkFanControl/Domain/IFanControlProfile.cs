namespace FrameworkFanControl.Domain;

public interface IFanControlProfile
{
	Percentage Get(ComputerState state);
}
