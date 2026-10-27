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

	public static void ShowError(string title, string message)
	{
		const uint MB_ICONERROR = 0x10;
		MessageBox(IntPtr.Zero, message, title, MB_ICONERROR);
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

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
