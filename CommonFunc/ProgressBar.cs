using System;
using System.Text;
using System.Threading;

/// <summary>
/// An ASCII progress bar
/// </summary>
namespace QuickConsoleTestNet6 {
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    /// Modified version of this: https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
    public class ProgressBar : IDisposable, IProgress<(double progress, string message)> {
        private const int _blockCount = 10;
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string _animation = @"|/-\";

        private readonly Timer _timer;

        private string _task;
        private int _top;
        private int _left;
        private double _currentProgress = 0;
        private string _currentMessage = string.Empty;
        private string _currentText = string.Empty;
        private bool _disposed = false;
        private int _animationIndex = 0;
        private static readonly object _consoleWriterLock = new();
        public ProgressBar(string task) {
            lock (_consoleWriterLock) {
                _task = task;
                _timer = new Timer(TimerHandler);

                // A progress bar is only for temporary display in a console window.
                // If the console output is redirected to a file, draw nothing.
                // Otherwise, we'll end up with a lot of garbage in the target file.
                if (!Console.IsOutputRedirected) {
                    ResetTimer();
                }

                if (!_task.StartsWith("\n"))
                    _task = $"\n{_task}";

                Console.Write(_task);
                _top = Console.CursorTop;
                _left = Console.CursorLeft;
            }
        }

        public void Report((double progress, string message) tuple) {
            // Make sure tuple is in [0..1] range
            tuple.progress = Math.Max(0, Math.Min(1, tuple.progress));
            Interlocked.Exchange(ref _currentProgress, tuple.progress);
            Interlocked.Exchange(ref _currentMessage, tuple.message);
        }

        private void TimerHandler(object state) {
            lock (_timer) {
                if (_disposed) return;

                int progressBlockCount = (int)Math.Floor(_currentProgress * _blockCount);
                int percent = (int)(_currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', _blockCount - progressBlockCount),
                    percent,
                    _animation[_animationIndex++ % _animation.Length]);
                UpdateText($"{text} {_currentMessage}");

                ResetTimer();
            }
        }

        private void UpdateText(string text) {
            lock (_consoleWriterLock) {
                Console.CursorTop = _top;
                Console.CursorLeft = _left;
                string clear = new(' ', _currentText.Length + 1);
                Console.Write(clear);

                Console.CursorLeft = _left;
                Console.Write(text);
                _currentText = text;
            }

        }

        private void ResetTimer() {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose() {

            lock (_consoleWriterLock) {
                Report((1, "Completed!"));
                TimerHandler(null);
                lock (_timer) {
                    _disposed = true;
                }
                Console.WriteLine("");
            }
        }

    }

}

