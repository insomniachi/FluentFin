using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FluentFin.Helpers;

internal static class NativeMethods
{

	public static void PreventSleep()
	{
		SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsDisplayRequired);
	}

	public static void AllowSleep()
	{
		SetThreadExecutionState(ExecutionState.EsContinuous);
	}

	public static bool IsAppForeground()
	{
		var fg = GetForegroundWindow();
		if (fg == IntPtr.Zero) return false;
		GetWindowThreadProcessId(fg, out uint pid);
		return pid == (uint)Process.GetCurrentProcess().Id;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[Flags]
	private enum ExecutionState : uint
	{
		EsAwaymodeRequired = 0x00000040,
		EsContinuous = 0x80000000,
		EsDisplayRequired = 0x00000002,
		EsSystemRequired = 0x00000001
	}
}
