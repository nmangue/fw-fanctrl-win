namespace FrameworkFanControl.Infrastructure;

public sealed class CrosEcException : Exception
{
	/// <summary>
	/// Code renvoyé par l'EC.
	/// </summary>
	public uint ResultCode { get; }

	public CrosEcException(uint resultCode)
		: base($"Cros EC error ({resultCode}).")
	{
		ResultCode = resultCode;
	}
}
