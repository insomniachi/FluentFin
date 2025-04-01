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
			_keySubscriptions.Add(_listener.Subscribe(Key.Space, async () => await TogglePlayPlause()).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Left, async() => await SeekTo(_controller.Position - _smallSeek)).Id);
			_keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Left], async () => await SeekTo(_controller.Position - _bigSeek)).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.Right, async () => await SeekTo(_controller.Position + _smallSeek)).Id);
			_keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Right], async () => await SeekTo(_controller.Position + _bigSeek)).Id);

			_keySubscriptions.Add(_listener.Subscribe(Key.S, async () => await _skipSegment()).Id);

			if (_toggleFullscreen is not null)
			{
				_keySubscriptions.Add(_listener.Subscribe(Key.F, _toggleFullscreen).Id);
			}

			_keySubscriptions.Add(_listener.Subscribe(Key.Up, () => _controller.Volume += 1).Id);
			_keySubscriptions.Add(_listener.Subscribe(Key.Down, () => _controller.Volume -= 1).Id);
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
