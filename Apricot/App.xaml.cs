using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Scripting.Hosting;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ScriptRuntime runtime = null;
        private bool sessionEnding = false;

        public App()
        {
            Configuration config = null;
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            if (Directory.Exists(directory))
            {
                string fileName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config") where String.Equals(fileName, Path.GetFileNameWithoutExtension(s)) select s)
                {
                    ExeConfigurationFileMap exeConfigurationFileMap = new ExeConfigurationFileMap();

                    exeConfigurationFileMap.ExeConfigFilename = s;
                    config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
                }
            }

            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                directory = null;
            }

            if (config.AppSettings.Settings["Scripts"] != null)
            {
                this.runtime = new ScriptRuntime(ScriptRuntimeSetup.ReadConfiguration());

                foreach (string fileName in Directory.GetFiles(directory == null ? config.AppSettings.Settings["Scripts"].Value : Path.Combine(directory, config.AppSettings.Settings["Scripts"].Value)))
                {
                    ScriptEngine engine;

                    if (this.runtime.TryGetEngineByFileExtension(Path.GetExtension(fileName), out engine))
                    {
                        ScriptSource source = engine.CreateScriptSourceFromFile(fileName);
                        CompiledCode code = source.Compile();

                        code.Execute(this.runtime.CreateScope());
                    }
                }
            }
        }

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

                if (this.runtime != null)
                {
                    this.runtime.Shutdown();
                }
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            this.MainWindow.Close();

            Script.Instance.Save();

            if (this.runtime != null)
            {
                this.runtime.Shutdown();
            }

            this.sessionEnding = true;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Exception.ToString());
        }
    }
}
