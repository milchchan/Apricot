using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool sessionEnding = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Agent agent = null;

            Script.Instance.Load();

            foreach (Character character in Script.Instance.Characters)
            {
                Agent a = new Agent(character.Name);

                if (agent == null)
                {
                    agent = a;
                }
                else
                {
                    a.Owner = agent;
                }

                a.Show();
            }

            Script.Instance.Enabled = true;
            Script.Instance.Tick(DateTime.Today);
            Script.Instance.Update(true);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (!this.sessionEnding)
            {
                Script.Instance.Save();
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            this.MainWindow.Close();

            Script.Instance.Save();

            this.sessionEnding = true;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Exception.ToString());
        }
    }
}
