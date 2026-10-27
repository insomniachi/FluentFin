using System.Threading.Tasks;
using System.Windows.Input;
using DeftSharp.Windows.Input.Keyboard;
using FluentFin.Core.Contracts.Services;
using FluentFin.Helpers;

namespace FluentFin.Services
{
	public sealed partial class KeyboardMediaPlayerController
	{
		private readonly IMediaPlayerController _controller;
		private readonly IJellyfinClient _jellyfinClient;
		private readonly Func<Task> _skipSegment;
		private readonly Action? _toggleFullscreen;
		private readonly KeyboardListener _listener = new();
		private readonly List<Guid> _keySubscriptions = [];
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
			_keySubscriptions.Add(_listener.Subscribe(Key.Space, async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await TogglePlayPlause();
			}).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Left, async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await SeekTo(_controller.Position - _smallSeek);
			}).Id);

			_keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Left], async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await SeekTo(_controller.Position - _bigSeek);
			}).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Right, async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await SeekTo(_controller.Position + _smallSeek);
			}).Id);

			_keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Right], async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await SeekTo(_controller.Position + _bigSeek);
			}).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.S, async () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				await _skipSegment();
			}).Id);

			if (_toggleFullscreen is not null)
			{
				_keySubscriptions.Add(_listener.Subscribe(Key.F, () =>
				{
					if (!NativeMethods.IsAppForeground())
					{
						return;
					}

					_toggleFullscreen();
				}).Id);
			}

			_keySubscriptions.Add(_listener.Subscribe(Key.Up, () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				_controller.Volume += 1;
			}).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Down, () =>
			{
				if (!NativeMethods.IsAppForeground())
				{
					return;
				}

				_controller.Volume -= 1;
			}).Id);
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
