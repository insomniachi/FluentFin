using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DeftSharp.Windows.Input.Keyboard;
using FluentFin.Core.Contracts.Services;

namespace FluentFin.Services
{
	public sealed partial class KeyboardMediaPlayerController
	{
		private readonly IMediaPlayerController _controller;
		private readonly IJellyfinClient _jellyfinClient;
		private readonly Func<Task> _skipSegment;
		private readonly Action? _toggleFullscreen;
		private readonly KeyboardListener _listener = new();
		private readonly List<Guid> _keySubscriptions = new();
		private readonly TimeSpan _smallSeek = TimeSpan.FromSeconds(5);
		private readonly TimeSpan _bigSeek = TimeSpan.FromSeconds(10);

		public KeyboardMediaPlayerController(IMediaPlayerController controller,
											 IJellyfinClient jellyfinClient,
											 Func<Task> skipSegment,
											 Action? toggleFullscreen)
		{
			_controller = controller;
			_jellyfinClient = jellyfinClient;
			_skipSegment = skipSegment;
			_toggleFullscreen = toggleFullscreen;

			SubscribeEvents();
		}

		private void SubscribeEvents()
		{
			_keySubscriptions.Add(_listener.Subscribe(Key.Space, async () => { if (IsAppForeground()) await TogglePlayPlause(); }).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Left, async() => { if (IsAppForeground()) await SeekTo(_controller.Position - _smallSeek); }).Id);
			_keySubscriptions.Add(_listener.SubscribeCombination(new[] { Key.LeftCtrl | Key.RightCtrl, Key.Left }, async () => { if (IsAppForeground()) await SeekTo(_controller.Position - _bigSeek); }).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Right, async () => { if (IsAppForeground()) await SeekTo(_controller.Position + _smallSeek); }).Id);
			_keySubscriptions.Add(_listener.SubscribeCombination(new[] { Key.LeftCtrl | Key.RightCtrl, Key.Right }, async () => { if (IsAppForeground()) await SeekTo(_controller.Position + _bigSeek); }).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.S, async () => { if (IsAppForeground()) await _skipSegment(); }).Id);

			if (_toggleFullscreen is not null)
			{
				_keySubscriptions.Add(_listener.Subscribe(Key.F, () => { if (IsAppForeground() && _toggleFullscreen is not null) _toggleFullscreen(); }).Id);
			}

			_keySubscriptions.Add(_listener.Subscribe(Key.Up, () => { if (IsAppForeground()) _controller.Volume += 1; }).Id);
			_keySubscriptions.Add(_listener.Subscribe(Key.Down, () => { if (IsAppForeground()) _controller.Volume -= 1; }).Id);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		private static bool IsAppForeground()
		{
			var fg = GetForegroundWindow();
			if (fg == IntPtr.Zero) return false;
			GetWindowThreadProcessId(fg, out uint pid);
			return pid == (uint)Process.GetCurrentProcess().Id;
		}

		public void UnsubscribeEvents()
		{
			_listener.Unsubscribe(_keySubscriptions);
		}

		public async Task TogglePlayPlause()
		{
			await _controller.TogglePlayPlause(_jellyfinClient);
		}

		public async Task SeekTo(TimeSpan position)
		{
			await _controller.SeekTo(_jellyfinClient, position);
		}
	}
}
