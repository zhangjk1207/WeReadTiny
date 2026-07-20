using System;
using System.Threading;
using System.Windows.Forms;

namespace WeReadStealth
{
    internal static class Program
    {
        private const string MutexName = "Local\\WeReadStealth.Application.30f6a6bd";
        private const string ActivationEventName = "Local\\WeReadStealth.Activate.30f6a6bd";

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    SignalExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                try
                {
                    using (EventWaitHandle activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName))
                    using (MainForm mainForm = new MainForm(new SettingsStore()))
                    {
                        Thread activationThread = new Thread(new ThreadStart(delegate
                        {
                            try
                            {
                                while (!mainForm.IsDisposed)
                                {
                                    if (!activationEvent.WaitOne(500))
                                    {
                                        continue;
                                    }

                                    if (!mainForm.IsDisposed && mainForm.IsHandleCreated)
                                    {
                                        mainForm.BeginInvoke(new Action(mainForm.ShowFromTray));
                                    }
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                // Application shutdown disposes the event while the background listener exits.
                            }
                        }))
                        {
                            IsBackground = true,
                            Name = "Single-instance activation listener"
                        };
                        activationThread.Start();
                        Application.Run(mainForm);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        "应用启动失败。\r\n\r\n" + exception.Message,
                        "WeRead Stealth",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static void SignalExistingInstance()
        {
            try
            {
                using (EventWaitHandle activationEvent = EventWaitHandle.OpenExisting(ActivationEventName))
                {
                    activationEvent.Set();
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // The first process is still starting and will show itself shortly.
            }
        }
    }
}
