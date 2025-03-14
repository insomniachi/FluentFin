using DeftSharp.Windows.Input.Keyboard;
using FluentFin.Core.Contracts.Services;
using System.Windows.Input;

namespace FluentFin.Services
{
    public sealed partial class KeyboardMediaPlayerController
    {
        private readonly IMediaPlayerController _controller;
        private readonly Action _skipSegment;
        private readonly Action? _toggleFullscreen;
        private readonly KeyboardListener _listener = new();
        private readonly List<Guid> _keySubscriptions = [];
        private readonly TimeSpan _smallSeek = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _bigSeek = TimeSpan.FromSeconds(10);

        public KeyboardMediaPlayerController(IMediaPlayerController controller,
                                             Action skipSegment,
                                             Action? toggleFullscreen)
        {
            _controller = controller;
            _skipSegment = skipSegment;
            _toggleFullscreen = toggleFullscreen;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _keySubscriptions.Add(_listener.Subscribe(Key.Space, _controller.TogglePlayPlause).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.Left, () => _controller.SeekBackward(_smallSeek)).Id);
            _keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Left], () => _controller.SeekBackward(_bigSeek)).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.Right, () => _controller.SeekForward(_smallSeek)).Id);
            _keySubscriptions.Add(_listener.SubscribeCombination([Key.LeftCtrl | Key.RightCtrl, Key.Right], () => _controller.SeekForward(_bigSeek)).Id);
            
            _keySubscriptions.Add(_listener.Subscribe(Key.S, _skipSegment).Id);
            
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
    }
}
