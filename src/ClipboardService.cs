using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class ClipboardService : IDisposable
    {
        private readonly SettingsService settings;
        private readonly Timer timer = new Timer();
        private string lastValue;
        private uint sequence;
        private bool disposed;
        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        public ClipboardService(SettingsService settingsService)
        {
            settings = settingsService;
            timer.Tick += ClearIfStillOwned;
        }

        internal int ClearSeconds { get { return Math.Max(5, settings.Current.ClipboardClearSeconds); } }
        internal string CopyNotice { get { return "Очистка текущего буфера через " + ClearSeconds + " сек. и при выходе из ProGo. История буфера не очищается."; } }

        public void CopySecret(string value)
        {
            if (disposed) throw new ObjectDisposedException("ClipboardService");
            if (String.IsNullOrEmpty(value)) return;
            Clipboard.SetText(value);
            // Record only after a successful write. Sequence detects later copies,
            // including a user's new copy of identical text.
            lastValue = value;
            sequence = GetClipboardSequenceNumber();
            timer.Stop();
            timer.Interval = ClearSeconds * 1000;
            timer.Start();
            SafeLog.Info("Secret copied to clipboard; auto-clear scheduled.");
        }

        internal void BindSecretCopy(Button button, Func<string> value, Label notice)
        {
            button.Click += delegate
            {
                try { CopySecret(value()); notice.Text = "Скопировано. " + CopyNotice; }
                catch (ExternalException) { notice.Text = "Буфер занят. Закройте использующее его приложение и попробуйте снова."; }
            };
        }

        private void ClearIfStillOwned(object sender, EventArgs e)
        {
            timer.Stop();
            try
            {
                if (!String.IsNullOrEmpty(lastValue) && sequence != 0 && GetClipboardSequenceNumber() == sequence &&
                    Clipboard.ContainsText() && Clipboard.GetText() == lastValue && GetClipboardSequenceNumber() == sequence)
                {
                    Clipboard.Clear();
                    SafeLog.Info("Clipboard cleared.");
                }
            }
            catch (ExternalException) { SafeLog.Info("Clipboard clear unavailable; clipboard may be busy."); }
            finally { lastValue = null; sequence = 0; }
        }

        public void Dispose()
        {
            if (disposed) return;
            ClearIfStillOwned(this, EventArgs.Empty);
            timer.Dispose(); disposed = true;
        }
    }
}
