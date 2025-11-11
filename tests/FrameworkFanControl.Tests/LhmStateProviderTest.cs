using NFluent;

namespace FrameworkFanControl.Tests;

public class LhmStateProviderTest
{
	[Fact]
	public void SmokeTest()
	{
		using var provider = new LhmStateProvider();

		var state = provider.ReadState();

		Check.That(state).IsNotNull();
		Check.That(state.CoreAvgTemp).Not.IsNaN();
		Check.That(state.CoreMaxTemp).Not.IsNaN();
		Check.That(state.CoreTemps).ContainsOnlyElementsThatMatch(t => !float.IsNaN(t));
	}
}
