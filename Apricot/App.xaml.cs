using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool sessionEnding = false;
        [System.Composition.ImportMany]
        public IEnumerable<IExtension> Extensions { get; set; }

        public App()
        {
            Configuration config1 = null;
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            if (Directory.Exists(directory))
            {
                string filename = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly) where filename.Equals(Path.GetFileNameWithoutExtension(s)) select s)
                {
                    ExeConfigurationFileMap exeConfigurationFileMap = new ExeConfigurationFileMap();

                    exeConfigurationFileMap.ExeConfigFilename = s;
                    config1 = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
                }
            }

            if (config1 == null)
            {
                config1 = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config1.AppSettings.Settings["Scripts"] != null)
                {
                    ScriptOptions scriptOptions = ScriptOptions.Default.WithReferences(System.Reflection.Assembly.GetExecutingAssembly());
                    List<Task<ScriptState<object>>> taskList = new List<Task<ScriptState<object>>>();

                    foreach (string filename in Directory.EnumerateFiles(Path.IsPathRooted(config1.AppSettings.Settings["Scripts"].Value) ? config1.AppSettings.Settings["Scripts"].Value : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["Scripts"].Value), "*.csx", SearchOption.TopDirectoryOnly))
                    {
                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            taskList.Add(CSharpScript.RunAsync(sr.ReadToEnd(), scriptOptions));
                        }
                    }

                    Task<ScriptState<object>>.WaitAll(taskList.ToArray());
                }

                if (config1.AppSettings.Settings["Extensions"] != null)
                {
                    List<System.Reflection.Assembly> assemblyList = new List<System.Reflection.Assembly>();

                    foreach (string filename in Directory.EnumerateFiles(Path.IsPathRooted(config1.AppSettings.Settings["Extensions"].Value) ? config1.AppSettings.Settings["Extensions"].Value : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["Extensions"].Value), "*.dll", SearchOption.TopDirectoryOnly))
                    {
                        assemblyList.Add(System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(filename));
                    }

                    if (assemblyList.Count > 0)
                    {
                        using (System.Composition.Hosting.CompositionHost container = new System.Composition.Hosting.ContainerConfiguration().WithAssemblies(assemblyList).CreateContainer())
                        {
                            this.Extensions = container.GetExports<IExtension>();
                        }

                        Script.Instance.Start += new EventHandler<EventArgs>(delegate
                        {
                            foreach (IExtension extension in this.Extensions)
                            {
                                extension.Attach();
                            }
                        });
                        Script.Instance.Stop += new EventHandler<EventArgs>(delegate
                        {
                            foreach (IExtension extension in this.Extensions)
                            {
                                extension.Detach();
                            }
                        });
                    }
                }
            }
            else
            {
                Configuration config2 = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config1.AppSettings.Settings["Scripts"] == null)
                {
                    if (config2.AppSettings.Settings["Scripts"] != null)
                    {
                        if (Path.IsPathRooted(config2.AppSettings.Settings["Scripts"].Value))
                        {
                            ScriptOptions scriptOptions = ScriptOptions.Default.WithReferences(System.Reflection.Assembly.GetExecutingAssembly());
                            List<Task<ScriptState<object>>> taskList = new List<Task<ScriptState<object>>>();

                            foreach (string filename in Directory.EnumerateFiles(config2.AppSettings.Settings["Scripts"].Value, "*.csx", SearchOption.TopDirectoryOnly))
                            {
                                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (StreamReader sr = new StreamReader(fs))
                                {
                                    taskList.Add(CSharpScript.RunAsync(sr.ReadToEnd(), scriptOptions));
                                }
                            }

                            Task<ScriptState<object>>.WaitAll(taskList.ToArray());
                        }
                        else
                        {
                            string path = Path.Combine(directory, config2.AppSettings.Settings["Scripts"].Value);
                            ScriptOptions scriptOptions = ScriptOptions.Default.WithReferences(System.Reflection.Assembly.GetExecutingAssembly());
                            List<Task<ScriptState<object>>> taskList = new List<Task<ScriptState<object>>>();

                            foreach (string filename in Directory.EnumerateFiles(Directory.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config2.AppSettings.Settings["Scripts"].Value), "*.csx", SearchOption.TopDirectoryOnly))
                            {
                                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (StreamReader sr = new StreamReader(fs))
                                {
                                    taskList.Add(CSharpScript.RunAsync(sr.ReadToEnd(), scriptOptions));
                                }
                            }

                            Task<ScriptState<object>>.WaitAll(taskList.ToArray());
                        }
                    }
                }
                else if (Path.IsPathRooted(config1.AppSettings.Settings["Scripts"].Value))
                {
                    ScriptOptions scriptOptions = ScriptOptions.Default.WithReferences(System.Reflection.Assembly.GetExecutingAssembly());
                    List<Task<ScriptState<object>>> taskList = new List<Task<ScriptState<object>>>();

                    foreach (string filename in Directory.EnumerateFiles(config1.AppSettings.Settings["Scripts"].Value, "*.csx", SearchOption.TopDirectoryOnly))
                    {
                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            taskList.Add(CSharpScript.RunAsync(sr.ReadToEnd(), scriptOptions));
                        }
                    }

                    Task<ScriptState<object>>.WaitAll(taskList.ToArray());
                }
                else
                {
                    string path = Path.Combine(directory, config1.AppSettings.Settings["Scripts"].Value);
                    ScriptOptions scriptOptions = ScriptOptions.Default.WithReferences(System.Reflection.Assembly.GetExecutingAssembly());
                    List<Task<ScriptState<object>>> taskList = new List<Task<ScriptState<object>>>();

                    foreach (string filename in Directory.EnumerateFiles(Directory.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["Scripts"].Value), "*.csx", SearchOption.TopDirectoryOnly))
                    {
                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            taskList.Add(CSharpScript.RunAsync(sr.ReadToEnd(), scriptOptions));
                        }
                    }

                    Task<ScriptState<object>>.WaitAll(taskList.ToArray());
                }

                if (config1.AppSettings.Settings["Extensions"] == null)
                {
                    if (config2.AppSettings.Settings["Extensions"] != null)
                    {
                        List<System.Reflection.Assembly> assemblyList = new List<System.Reflection.Assembly>();

                        if (Path.IsPathRooted(config2.AppSettings.Settings["Extensions"].Value))
                        {
                            foreach (string filename in Directory.EnumerateFiles(config2.AppSettings.Settings["Extensions"].Value, "*.dll", SearchOption.TopDirectoryOnly))
                            {
                                assemblyList.Add(System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(filename));
                            }
                        }
                        else
                        {
                            string path = Path.Combine(directory, config2.AppSettings.Settings["Extensions"].Value);

                            foreach (string filename in Directory.EnumerateFiles(Directory.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config2.AppSettings.Settings["Extensions"].Value), "*.dll", SearchOption.TopDirectoryOnly))
                            {
                                assemblyList.Add(System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(filename));
                            }
                        }

                        if (assemblyList.Count > 0)
                        {
                            using (System.Composition.Hosting.CompositionHost container = new System.Composition.Hosting.ContainerConfiguration().WithAssemblies(assemblyList).CreateContainer())
                            {
                                this.Extensions = container.GetExports<IExtension>();
                            }

                            Script.Instance.Start += new EventHandler<EventArgs>(delegate
                            {
                                foreach (IExtension extension in this.Extensions)
                                {
                                    extension.Attach();
                                }
                            });
                            Script.Instance.Stop += new EventHandler<EventArgs>(delegate
                            {
                                foreach (IExtension extension in this.Extensions)
                                {
                                    extension.Detach();
                                }
                            });
                        }
                    }
                }
                else
                {
                    List<System.Reflection.Assembly> assemblyList = new List<System.Reflection.Assembly>();

                    if (Path.IsPathRooted(config1.AppSettings.Settings["Extensions"].Value))
                    {
                        foreach (string filename in Directory.EnumerateFiles(config1.AppSettings.Settings["Extensions"].Value, "*.dll", SearchOption.TopDirectoryOnly))
                        {
                            assemblyList.Add(System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(filename));
                        }
                    }
                    else
                    {
                        string path = Path.Combine(directory, config1.AppSettings.Settings["Extensions"].Value);

                        foreach (string filename in Directory.EnumerateFiles(Directory.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["Extensions"].Value), "*.dll", SearchOption.TopDirectoryOnly))
                        {
                            assemblyList.Add(System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(filename));
                        }
                    }

                    if (assemblyList.Count > 0)
                    {
                        using (System.Composition.Hosting.CompositionHost container = new System.Composition.Hosting.ContainerConfiguration().WithAssemblies(assemblyList).CreateContainer())
                        {
                            this.Extensions = container.GetExports<IExtension>();
                        }

                        Script.Instance.Start += new EventHandler<EventArgs>(delegate
                        {
                            foreach (IExtension extension in this.Extensions)
                            {
                                extension.Attach();
                            }
                        });
                        Script.Instance.Stop += new EventHandler<EventArgs>(delegate
                        {
                            foreach (IExtension extension in this.Extensions)
                            {
                                extension.Detach();
                            }
                        });
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

    public interface IExtension
    {
        public void Attach();
        public void Detach();
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
