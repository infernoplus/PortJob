using System;
using System.Text;
using System.Threading;

/// <summary>
/// An ASCII progress bar
/// </summary>
namespace PortJob {
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double> {
        private const int _blockCount = 10;
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string _animation = @"|/-\";

        private readonly Timer _timer;

        private string _task;
        private int _line;
        private int _top;
        private int _left;
        private double _currentProgress = 0;
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

        public void Report(double value) {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _currentProgress, value);
        }

        private void TimerHandler(object state) {
            lock (_timer) {
                if (_disposed) return;

                int progressBlockCount = (int)(_currentProgress * _blockCount);
                int percent = (int)(_currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', _blockCount - progressBlockCount),
                    percent,
                    _animation[_animationIndex++ % _animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text) {
            lock (_consoleWriterLock) {
                // Get length of common portion
                int commonPrefixLength = 0;
                int commonLength = Math.Min(_currentText.Length, text.Length);
                while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength]) {
                    commonPrefixLength++;
                }
                // Backtrack to the first differing character
                StringBuilder outputBuilder = new();
                outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

                // Output new suffix
                outputBuilder.Append(text.Substring(commonPrefixLength));

                // If the new text is shorter than the old one: delete overlapping characters
                int overlapCount = _currentText.Length - text.Length;
                if (overlapCount > 0) {
                    outputBuilder.Append(' ', overlapCount);
                    outputBuilder.Append('\b', overlapCount);
                }
                Console.Write(outputBuilder);
                _currentText = text;
            }

        }

        private void ResetTimer() {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose() {
            lock (_timer) {
                _disposed = true;
                UpdateText(string.Empty + "\n");
            }
        }

    }
    //public class ProgressBar : IDisposable, IProgress<double> {
    //    private const int BLOCK_COUNT = 10;
    //    private readonly TimeSpan ANIMATION_INTERVAL = TimeSpan.FromSeconds(1.0 / 8);
    //    private const string ANIMATION = @"|/-\";

    //    private readonly Timer _timer;

    //    private double _currentProgress = 0;
    //    private string _currentText = string.Empty;
    //    private bool _disposed = false;
    //    private int _animationIndex = 0;
    //    private readonly int _line;
    //    private readonly string _task = "";
    //    private static readonly object _consoleWriterLock = new();

    //    public ProgressBar(string task, int line) {
    //        lock (_consoleWriterLock) {
    //            _line = line;
    //            _timer = new Timer(TimerHandler);

    //            // A progress bar is only for temporary display in a console window.
    //            // If the console output is redirected to a file, draw nothing.
    //            // Otherwise, we'll end up with a lot of garbage in the target file.
    //            if (!Console.IsOutputRedirected) {
    //                resetTimer();
    //            }
    //            //_currentText = _task;
    //            _task = task;
    //            updateText("");
    //        }
    //    }

    //    public void Report(double value) {
    //        // Make sure value is in [0..1] range
    //        value = Math.Max(0, Math.Min(1, value));
    //        Interlocked.Exchange(ref _currentProgress, value);
    //    }

    //    private void TimerHandler(object state) {
    //        lock (_timer) {
    //            if (_disposed) return;

    //            int progressBlockCount = (int)(_currentProgress * BLOCK_COUNT);
    //            int percent = (int)(_currentProgress * 100);
    //            string text = string.Format("[{0}{1}] {2,3}% {3}",
    //                new string('#', progressBlockCount), new string('-', BLOCK_COUNT - progressBlockCount),
    //                percent,
    //                ANIMATION[_animationIndex++ % ANIMATION.Length]);
    //            updateText(text);

    //            resetTimer();
    //        }
    //    }

    //    private void updateText(string text) {
    //        // Get length of common portion
    //        int commonPrefixLength = 0;
    //        int commonLength = Math.Min(_currentText.Length, text.Length);
    //        while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength]) {
    //            commonPrefixLength++;
    //        }

    //        // Backtrack to the first differing character
    //        StringBuilder outputBuilder = new();
    //        outputBuilder.Append(_task);
    //        outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

    //        // Output new suffix
    //        outputBuilder.Append(text.Substring(commonPrefixLength));

    //        // If the new text is shorter than the old one: delete overlapping characters
    //        int overlapCount = _currentText.Length - text.Length;
    //        if (overlapCount > 0) {
    //            outputBuilder.Append(' ', overlapCount);
    //            outputBuilder.Append('\b', overlapCount);
    //        }

    //        lock (_consoleWriterLock) {
    //            Console.SetCursorPosition(0, _line);
    //            Console.Write(outputBuilder);
    //            _currentText = text;
    //        }
    //    }

    //    private void resetTimer() {
    //        _timer.Change(ANIMATION_INTERVAL, TimeSpan.FromMilliseconds(-1));
    //    }

    //    public void Dispose() {
    //        lock (_timer) {
    //            _disposed = true;
    //            updateText(string.Empty);
    //        }
    //    }

    //}

}

