using DeftSharp.Windows.Input.Keyboard;
using FluentFin.Core.Contracts.Services;
using System.Windows.Input;

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
            _keySubscriptions.Add(_listener.Subscribe(Key.Space, TogglePlayPlause).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.Left, () => SeekTo(_controller.Position - _smallSeek)).Id);
            _keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Left], () => SeekTo(_controller.Position - _bigSeek)).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.Right, () => SeekTo(_controller.Position + _smallSeek)).Id);
            _keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Right], () => SeekTo(_controller.Position + _bigSeek)).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.S, async () => await _skipSegment()).Id);
            
            if(_toggleFullscreen is not null)
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

        public void TogglePlayPlause()
        {
            if (_controller is null)
            {
                return;
            }

            if (_controller.State is MediaPlayerState.Playing)
            {
                _jellyfinClient.SignalPauseForSyncPlay();
                _controller.Pause();
            }
            else if (_controller.State is MediaPlayerState.Paused)
            {
                _jellyfinClient.SignalUnpauseForSyncPlay();
                _controller.Play();
            }
        }

        public void SeekTo(TimeSpan position)
        {
            if (_controller is null)
            {
                return;
            }
            _jellyfinClient.SignalSeekForSyncPlay(position);
            _controller.SeekTo(position);
        }
    }
}
