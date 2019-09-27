using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for Agent.xaml
    /// </summary>
    public partial class Agent : Window
    {
        public event EventHandler<EventArgs> DrawClipboard = null;
        private readonly double frameRate = 60;
        private Nullable<IntPtr> nextClipboardViewer = null;
        private Nullable<int> hotKeyID = null;
        private Balloon balloon = null;
        private string characterName = null;
        private Dictionary<string, BitmapImage> cachedBitmapImageDictionary = null;
        private List<Motion> cachedMotionList = null;
        private Dictionary<Storyboard, Window> fadeStoryboardDictionary = null;
        private Storyboard scaleStoryboard = null;
        private Storyboard mirrorStoryboard = null;
        private Dictionary<Image, Storyboard> imageStoryboardDictionary = null;
        private double opacity = 1;
        private double scale = 1;
        private Nullable<Point> mouseDownPosition = null;
        private bool isMute = false;
        private bool isVisible = true;
        private System.Collections.Queue queue = null;
        private Queue<Motion> motionQueue = null;
        private bool isFirst = true;
        private bool isLast = false;
        private DateTime baseDateTime;

        public Balloon Balloon
        {
            get
            {
                return this.balloon;
            }
        }

        public Agent(string name)
        {
            InitializeComponent();

            this.characterName = name;
            this.cachedBitmapImageDictionary = new Dictionary<string, BitmapImage>();
            this.cachedMotionList = new List<Motion>();
            this.fadeStoryboardDictionary = new Dictionary<Storyboard, Window>();
            this.imageStoryboardDictionary = new Dictionary<Image, Storyboard>();
            this.queue = new System.Collections.Queue();
            this.motionQueue = new Queue<Motion>();
            this.baseDateTime = DateTime.Now;
            this.ContextMenu = new ContextMenu();

            MenuItem opacityMenuItem = new MenuItem();
            MenuItem scalingMenuItem = new MenuItem();
            MenuItem refreshMenuItem = new MenuItem();
            MenuItem topmostMenuItem = new MenuItem();
            MenuItem showInTaskbarMenuItem = new MenuItem();
            MenuItem muteMenuItem = new MenuItem();
            MenuItem charactersMenuItem = new MenuItem();
            MenuItem sourcesMenuItem = new MenuItem();
            MenuItem updateMenuItem = new MenuItem();
            MenuItem exitMenuItem = new MenuItem();
            double opacity = 1;
            double scale = 2;

            opacityMenuItem.Header = Apricot.Resources.Opacity;

            do
            {
                MenuItem menuItem = new MenuItem();

                menuItem.Header = String.Concat(((int)Math.Floor(opacity * 100)).ToString(System.Globalization.CultureInfo.CurrentCulture), Apricot.Resources.Percent);
                menuItem.Tag = opacity;
                menuItem.Click += new RoutedEventHandler(delegate
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        Agent agent = window as Agent;

                        if (agent != null)
                        {
                            agent.opacity = (double)menuItem.Tag;

                            Storyboard storyboard1 = new Storyboard();
                            DoubleAnimation doubleAnimation1 = new DoubleAnimation(agent.Opacity, agent.opacity, TimeSpan.FromMilliseconds(500));

                            foreach (KeyValuePair<Storyboard, Window> kvp in agent.fadeStoryboardDictionary)
                            {
                                kvp.Key.Stop(kvp.Value);
                            }

                            agent.fadeStoryboardDictionary.Clear();

                            if (agent.Opacity < agent.opacity)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;
                                doubleAnimation1.EasingFunction = sineEase;
                            }
                            else if (agent.Opacity > agent.opacity)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseIn;
                                doubleAnimation1.EasingFunction = sineEase;
                            }

                            doubleAnimation1.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                            {
                                if (((Clock)s).CurrentState == ClockState.Filling)
                                {
                                    agent.Opacity = agent.opacity;
                                    storyboard1.Remove(agent);
                                    agent.fadeStoryboardDictionary.Remove(storyboard1);
                                }
                            });

                            storyboard1.Children.Add(doubleAnimation1);

                            Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(Window.OpacityProperty));

                            agent.fadeStoryboardDictionary.Add(storyboard1, agent);
                            agent.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, true);

                            if (agent.balloon.Opacity != 1)
                            {
                                Storyboard storyboard2 = new Storyboard();
                                DoubleAnimation doubleAnimation2 = new DoubleAnimation(agent.balloon.Opacity, 1, TimeSpan.FromMilliseconds(500));
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;

                                doubleAnimation2.EasingFunction = sineEase;
                                doubleAnimation2.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                {
                                    if (((Clock)s).CurrentState == ClockState.Filling)
                                    {
                                        agent.balloon.Opacity = 1;
                                        storyboard2.Remove(agent.balloon);
                                        agent.fadeStoryboardDictionary.Remove(storyboard2);
                                    }
                                });

                                storyboard2.Children.Add(doubleAnimation2);

                                Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Window.OpacityProperty));

                                agent.fadeStoryboardDictionary.Add(storyboard2, agent.balloon);
                                agent.balloon.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, true);
                            }
                        }
                    }
                });

                opacityMenuItem.Items.Add(menuItem);
                opacity -= 0.1;
            } while (Math.Floor(opacity * 100) > 0);

            scalingMenuItem.Header = Apricot.Resources.Scaling;

            do
            {
                MenuItem menuItem = new MenuItem();

                menuItem.Header = String.Concat(((int)Math.Floor(scale * 100)).ToString(System.Globalization.CultureInfo.CurrentCulture), Apricot.Resources.Percent);
                menuItem.Tag = scale;
                menuItem.Click += new RoutedEventHandler(delegate
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        Agent agent = window as Agent;

                        if (agent != null)
                        {
                            agent.scale = (double)menuItem.Tag;

                            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select character)
                            {
                                Storyboard storyboard = new Storyboard();
                                DoubleAnimation doubleAnimation1 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleX, agent.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation2 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleY, agent.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation3 = new DoubleAnimation(agent.LayoutRoot.Width, character.Size.Width * agent.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation4 = new DoubleAnimation(agent.LayoutRoot.Height, character.Size.Height * agent.scale, TimeSpan.FromMilliseconds(500));

                                if (agent.scaleStoryboard != null)
                                {
                                    agent.scaleStoryboard.Stop(agent.LayoutRoot);
                                }

                                if (agent.ZoomScaleTransform.ScaleX < agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation1.EasingFunction = sineEase;
                                }
                                else if (agent.ZoomScaleTransform.ScaleX > agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation1.EasingFunction = sineEase;
                                }

                                if (agent.ZoomScaleTransform.ScaleY < agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation2.EasingFunction = sineEase;
                                }
                                else if (agent.ZoomScaleTransform.ScaleY > agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation2.EasingFunction = sineEase;
                                }

                                if (agent.LayoutRoot.Width < character.Size.Width * agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation3.EasingFunction = sineEase;
                                }
                                else if (agent.LayoutRoot.Width > character.Size.Width * agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation3.EasingFunction = sineEase;
                                }

                                if (agent.LayoutRoot.Height < character.Size.Height * agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation4.EasingFunction = sineEase;
                                }
                                else if (agent.LayoutRoot.Height > character.Size.Height * agent.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation4.EasingFunction = sineEase;
                                }

                                storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                {
                                    if (((Clock)s).CurrentState == ClockState.Filling)
                                    {
                                        agent.ZoomScaleTransform.ScaleX = agent.scale;
                                        agent.ZoomScaleTransform.ScaleY = agent.scale;

                                        foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                                        {
                                            agent.LayoutRoot.Width = c.Size.Width * agent.scale;
                                            agent.LayoutRoot.Height = c.Size.Height * agent.scale;
                                        }

                                        storyboard.Remove(agent.LayoutRoot);
                                        agent.scaleStoryboard = null;
                                    }
                                });
                                storyboard.Children.Add(doubleAnimation1);
                                storyboard.Children.Add(doubleAnimation2);
                                storyboard.Children.Add(doubleAnimation3);
                                storyboard.Children.Add(doubleAnimation4);

                                Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                                Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                                Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(ContentControl.WidthProperty));
                                Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.HeightProperty));

                                agent.scaleStoryboard = storyboard;
                                agent.LayoutRoot.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                            }
                        }
                    }
                });

                scalingMenuItem.Items.Add(menuItem);
                scale -= 0.25;
            } while (Math.Floor(scale * 100) > 0);

            refreshMenuItem.Header = Apricot.Resources.Refresh;
            refreshMenuItem.Click += new RoutedEventHandler(delegate
            {
                foreach (Window window in Application.Current.Windows)
                {
                    Agent agent = window as Agent;

                    if (agent != null)
                    {
                        agent.Render();
                    }
                }
            });
            topmostMenuItem.Header = Apricot.Resources.Topmost;
            topmostMenuItem.IsCheckable = true;
            topmostMenuItem.Click += new RoutedEventHandler(delegate
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Agent && window == Application.Current.MainWindow)
                    {
                        window.Topmost = topmostMenuItem.IsChecked;
                    }
                }
            });
            showInTaskbarMenuItem.Header = Apricot.Resources.ShowInTaskbar;
            showInTaskbarMenuItem.IsCheckable = true;
            showInTaskbarMenuItem.Click += new RoutedEventHandler(delegate
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Agent)
                    {
                        window.ShowInTaskbar = showInTaskbarMenuItem.IsChecked;
                    }
                }
            });
            muteMenuItem.Header = Apricot.Resources.Mute;
            muteMenuItem.IsCheckable = true;
            muteMenuItem.Click += new RoutedEventHandler(delegate
            {
                foreach (Window window in Application.Current.Windows)
                {
                    Agent agent = window as Agent;

                    if (agent != null)
                    {
                        agent.isMute = muteMenuItem.IsChecked;
                    }
                }
            });
            charactersMenuItem.Header = Apricot.Resources.Characters;
            sourcesMenuItem.Header = Apricot.Resources.Sources;
            updateMenuItem.Header = Apricot.Resources.Update;
            updateMenuItem.Click += new RoutedEventHandler(delegate
            {
                Script.Instance.Update(true);
            });
            exitMenuItem.Header = Apricot.Resources.Exit;
            exitMenuItem.Click += new RoutedEventHandler(delegate
            {
                if (Script.Instance.Enabled)
                {
                    Script.Instance.Enabled = false;
                }
            });

            this.ContextMenu.Items.Add(opacityMenuItem);
            this.ContextMenu.Items.Add(scalingMenuItem);
            this.ContextMenu.Items.Add(refreshMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(topmostMenuItem);
            this.ContextMenu.Items.Add(showInTaskbarMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(muteMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(charactersMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(sourcesMenuItem);
            this.ContextMenu.Items.Add(updateMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(exitMenuItem);
            this.ContextMenu.Opened += new RoutedEventHandler(delegate
            {
                Agent agent = Application.Current.MainWindow as Agent;

                if (agent != null)
                {
                    const uint CF_UNICODETEXT = 13;

                    foreach (MenuItem menuItem in opacityMenuItem.Items)
                    {
                        menuItem.IsChecked = Math.Floor((double)menuItem.Tag * 100) == Math.Floor(agent.Opacity * 100);
                    }

                    foreach (MenuItem menuItem in scalingMenuItem.Items)
                    {
                        menuItem.IsChecked = Math.Floor((double)menuItem.Tag * 100) == Math.Floor(agent.ZoomScaleTransform.ScaleX * 100) && Math.Floor((double)menuItem.Tag * 100) == Math.Floor(agent.ZoomScaleTransform.ScaleY * 100);
                    }

                    topmostMenuItem.IsChecked = agent.Topmost;
                    showInTaskbarMenuItem.IsChecked = agent.ShowInTaskbar;
                    muteMenuItem.IsChecked = agent.isMute;

                    List<MenuItem> characterMenuItemList = new List<MenuItem>(charactersMenuItem.Items.Cast<MenuItem>());
                    HashSet<string> pathHashSet = new HashSet<string>();
                    LinkedList<Tuple<Character, string>> characterLinkedList = new LinkedList<Tuple<Character, string>>();
                    string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                    Dictionary<string, List<Tuple<string, string>>> pathDictionary = new Dictionary<string, List<Tuple<string, string>>>();
                    List<Tuple<string, string, string>> namePathList = new List<Tuple<string, string, string>>();
                    bool likeRequired = (DateTime.Now - this.baseDateTime).TotalMinutes / 30 >= 1 ? true : false;

                    foreach (Character character in Script.Instance.Characters)
                    {
                        string path = Path.IsPathRooted(character.Script) ? character.Script : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), character.Script);

                        if (!pathHashSet.Contains(path))
                        {
                            pathHashSet.Add(path);
                        }

                        characterLinkedList.AddLast(Tuple.Create<Character, string>(character, path));
                    }

                    foreach (Tuple<bool, string> tuple1 in (from filename in Directory.Exists(dataDirectory) ? Directory.EnumerateFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "*", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(dataDirectory, "*", SearchOption.AllDirectories)) : Directory.EnumerateFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "*", SearchOption.AllDirectories) let attributes = File.GetAttributes(filename) let extension = Path.GetExtension(filename) let isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) where (attributes & FileAttributes.Hidden) != FileAttributes.Hidden && (isZip || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)) select Tuple.Create<bool, string>(isZip, filename)).Concat(from path in pathHashSet select Tuple.Create<bool, string>(Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase), path)))
                    {
                        if (!namePathList.Exists(delegate (Tuple<string, string, string> tuple2)
                        {
                            return tuple2.Item2.Equals(tuple1.Item2);
                        }) && !pathDictionary.Values.Any(delegate (List<Tuple<string, string>> tupleList1)
                        {
                            return tupleList1.Exists(delegate (Tuple<string, string> tuple)
                            {
                                return tuple.Item1.Equals(tuple1.Item2);
                            });
                        }))
                        {
                            if (tuple1.Item1)
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(tuple1.Item2, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    using (ZipArchive zipArchive = new ZipArchive(fs))
                                    {
                                        fs = null;

                                        foreach (List<Tuple<ZipArchiveEntry, string>> tupleList1 in (from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry).Aggregate<ZipArchiveEntry, Dictionary<string, List<Tuple<ZipArchiveEntry, string>>>>(new Dictionary<string, List<Tuple<ZipArchiveEntry, string>>>(), delegate (Dictionary<string, List<Tuple<ZipArchiveEntry, string>>> dictionary, ZipArchiveEntry zipArchiveEntry)
                                        {
                                            string filename = Path.GetFileNameWithoutExtension(zipArchiveEntry.FullName);
                                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(filename, "^(.+?)\\.([a-z]{2,3})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                                            string key;
                                            List<Tuple<ZipArchiveEntry, string>> tupleList1;

                                            if (match.Success)
                                            {
                                                key = String.Concat(Path.GetDirectoryName(zipArchiveEntry.FullName), match.Groups[1].Value);

                                                if (dictionary.TryGetValue(key, out tupleList1))
                                                {
                                                    tupleList1.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, match.Groups[2].Value));
                                                }
                                                else
                                                {
                                                    tupleList1 = new List<Tuple<ZipArchiveEntry, string>>();
                                                    tupleList1.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, match.Groups[2].Value));
                                                    dictionary.Add(key, tupleList1);
                                                }
                                            }
                                            else
                                            {
                                                key = String.Concat(Path.GetDirectoryName(zipArchiveEntry.FullName), filename);

                                                if (dictionary.TryGetValue(key, out tupleList1))
                                                {
                                                    tupleList1.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                                }
                                                else
                                                {
                                                    tupleList1 = new List<Tuple<ZipArchiveEntry, string>>();
                                                    tupleList1.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                                    dictionary.Add(key, tupleList1);
                                                }
                                            }

                                            return dictionary;
                                        }).Values)
                                        {
                                            if (pathHashSet.Contains(tuple1.Item2))
                                            {
                                                int length = namePathList.Count;
                                                Tuple<string, List<Tuple<string, string>>> tuple2 = null;
                                                Tuple<string, List<Tuple<string, string>>> tuple3 = null;

                                                tupleList1.ForEach(delegate (Tuple<ZipArchiveEntry, string> tuple4)
                                                {
                                                    Stream stream = null;
                                                    Tuple<string, List<Tuple<string, string>>> tupleList2 = Tuple.Create<string, List<Tuple<string, string>>>(tuple4.Item1.FullName, new List<Tuple<string, string>>());

                                                    try
                                                    {
                                                        stream = tuple4.Item1.Open();

                                                        foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                        {
                                                            tupleList2.Item2.Add(Tuple.Create<string, string>(attribute, tuple1.Item2));
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        return;
                                                    }
                                                    finally
                                                    {
                                                        if (stream != null)
                                                        {
                                                            stream.Close();
                                                        }
                                                    }

                                                    tupleList2.Item2.ForEach(delegate (Tuple<string, string> tuple5)
                                                    {
                                                        foreach (Character character in Script.Instance.Characters)
                                                        {
                                                            if (tuple5.Item1.Equals(character.Name))
                                                            {
                                                                namePathList.Add(Tuple.Create<string, string, string>(tuple5.Item1, tuple5.Item2, tupleList2.Item1));

                                                                break;
                                                            }
                                                        }
                                                    });

                                                    if (tuple4.Item2.Equals(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName))
                                                    {
                                                        tuple2 = tupleList2;
                                                    }
                                                    else if (tuple4.Item2.Equals(System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName))
                                                    {
                                                        tuple3 = tupleList2;
                                                    }
                                                });

                                                if (tuple2 == null)
                                                {
                                                    if (tuple3 != null)
                                                    {
                                                        if (length == namePathList.Count)
                                                        {
                                                            tuple3.Item2.ForEach(delegate (Tuple<string, string> tuple4)
                                                            {
                                                                namePathList.Add(Tuple.Create<string, string, string>(tuple4.Item1, tuple4.Item2, tuple3.Item1));
                                                            });
                                                        }
                                                        else
                                                        {
                                                            bool isNew = true;

                                                            for (int i = length; i < namePathList.Count; i++)
                                                            {
                                                                if (tuple3.Item1.Equals(namePathList[i].Item1))
                                                                {
                                                                    isNew = false;

                                                                    break;
                                                                }
                                                            }

                                                            if (isNew)
                                                            {
                                                                tuple3.Item2.ForEach(delegate (Tuple<string, string> tuple4)
                                                                {
                                                                    namePathList.Add(Tuple.Create<string, string, string>(tuple4.Item1, tuple4.Item2, tuple3.Item1));
                                                                });
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (length == namePathList.Count)
                                                {
                                                    tuple2.Item2.ForEach(delegate (Tuple<string, string> tuple4)
                                                    {
                                                        namePathList.Add(Tuple.Create<string, string, string>(tuple4.Item1, tuple4.Item2, tuple2.Item1));
                                                    });
                                                }
                                                else
                                                {
                                                    bool isNew = true;

                                                    for (int i = length; i < namePathList.Count; i++)
                                                    {
                                                        if (tuple2.Item1.Equals(namePathList[i].Item1))
                                                        {
                                                            isNew = false;

                                                            break;
                                                        }
                                                    }

                                                    if (isNew)
                                                    {
                                                        tuple2.Item2.ForEach(delegate (Tuple<string, string> tuple4)
                                                        {
                                                            namePathList.Add(Tuple.Create<string, string, string>(tuple4.Item1, tuple4.Item2, tuple2.Item1));
                                                        });
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Tuple<ZipArchiveEntry, string> tuple2 = tupleList1.Find(delegate (Tuple<ZipArchiveEntry, string> tuple3)
                                                {
                                                    return tuple3.Item2.Equals(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                                                });

                                                if (tuple2 == null)
                                                {
                                                    tuple2 = tupleList1.Find(delegate (Tuple<ZipArchiveEntry, string> tuple3)
                                                    {
                                                        return tuple3.Item2.Equals(System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName);
                                                    });

                                                    if (tuple2 != null)
                                                    {
                                                        Stream stream = null;
                                                        List<Tuple<string, string, string>> tupleList2 = new List<Tuple<string, string, string>>();

                                                        try
                                                        {
                                                            stream = tuple2.Item1.Open();

                                                            foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                            {
                                                                tupleList2.Add(Tuple.Create<string, string, string>(attribute, tuple1.Item2, tuple2.Item1.FullName));
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            continue;
                                                        }
                                                        finally
                                                        {
                                                            if (stream != null)
                                                            {
                                                                stream.Close();
                                                            }
                                                        }

                                                        if (pathHashSet.Contains(tuple1.Item2))
                                                        {

                                                        }

                                                        namePathList.AddRange(tupleList2);
                                                    }
                                                }
                                                else
                                                {
                                                    Stream stream = null;
                                                    List<Tuple<string, string, string>> tupleList2 = new List<Tuple<string, string, string>>();

                                                    try
                                                    {
                                                        stream = tuple2.Item1.Open();

                                                        foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                        {
                                                            tupleList2.Add(Tuple.Create<string, string, string>(attribute, tuple1.Item2, tuple2.Item1.FullName));
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        continue;
                                                    }
                                                    finally
                                                    {
                                                        if (stream != null)
                                                        {
                                                            stream.Close();
                                                        }
                                                    }

                                                    namePathList.AddRange(tupleList2);
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    if (fs != null)
                                    {
                                        fs.Close();
                                    }
                                }
                            }
                            else
                            {
                                string filename = Path.GetFileNameWithoutExtension(tuple1.Item2);
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(filename, "^(.+?)\\.([a-z]{2,3})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                                string key;
                                List<Tuple<string, string>> tupleList;

                                if (match.Success)
                                {
                                    key = String.Concat(Path.GetDirectoryName(tuple1.Item2), match.Groups[1].Value);

                                    if (pathDictionary.TryGetValue(key, out tupleList))
                                    {
                                        tupleList.Add(Tuple.Create<string, string>(tuple1.Item2, match.Groups[2].Value));
                                    }
                                    else
                                    {
                                        tupleList = new List<Tuple<string, string>>();
                                        tupleList.Add(Tuple.Create<string, string>(tuple1.Item2, match.Groups[2].Value));
                                        pathDictionary.Add(key, tupleList);
                                    }
                                }
                                else
                                {
                                    key = String.Concat(Path.GetDirectoryName(tuple1.Item2), filename);

                                    if (pathDictionary.TryGetValue(key, out tupleList))
                                    {
                                        tupleList.Add(Tuple.Create<string, string>(tuple1.Item2, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                    }
                                    else
                                    {
                                        tupleList = new List<Tuple<string, string>>();
                                        tupleList.Add(Tuple.Create<string, string>(tuple1.Item2, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                        pathDictionary.Add(key, tupleList);
                                    }
                                }
                            }
                        }
                    }

                    foreach (List<Tuple<string, string>> tupleList in pathDictionary.Values)
                    {
                        int length = namePathList.Count;
                        Tuple<string, string> tuple1 = null;
                        Tuple<string, string> tuple2 = null;

                        tupleList.ForEach(delegate (Tuple<string, string> tuple3)
                        {
                            if (pathHashSet.Contains(tuple3.Item1))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(tuple3.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                    {
                                        namePathList.Add(Tuple.Create<string, string, string>(attribute, tuple3.Item1, null));
                                    }
                                }
                                catch
                                {
                                    return;
                                }
                                finally
                                {
                                    if (fs != null)
                                    {
                                        fs.Close();
                                    }
                                }
                            }

                            if (tuple3.Item2.Equals(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName))
                            {
                                tuple1 = tuple3;
                            }
                            else if (tuple3.Item2.Equals(System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName))
                            {
                                tuple2 = tuple3;
                            }
                        });

                        if (tuple1 == null)
                        {
                            if (tuple2 != null)
                            {
                                if (length == namePathList.Count)
                                {
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(tuple2.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                        {
                                            namePathList.Add(Tuple.Create<string, string, string>(attribute, tuple2.Item1, null));
                                        }
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }
                                }
                                else
                                {
                                    bool isNew = true;

                                    for (int i = length; i < namePathList.Count; i++)
                                    {
                                        if (tuple2.Item1.Equals(namePathList[i].Item2))
                                        {
                                            isNew = false;

                                            break;
                                        }
                                    }

                                    if (isNew)
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tuple2.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                            {
                                                namePathList.Add(Tuple.Create<string, string, string>(attribute, tuple2.Item1, null));
                                            }
                                        }
                                        catch
                                        {
                                            continue;
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (length == namePathList.Count)
                        {
                            FileStream fs = null;

                            try
                            {
                                fs = new FileStream(tuple1.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                {
                                    namePathList.Add(Tuple.Create<string, string, string>(attribute, tuple1.Item1, null));
                                }
                            }
                            catch
                            {
                                continue;
                            }
                            finally
                            {
                                if (fs != null)
                                {
                                    fs.Close();
                                }
                            }
                        }
                        else
                        {
                            bool isNew = true;

                            for (int i = length; i < namePathList.Count; i++)
                            {
                                if (tuple1.Item1.Equals(namePathList[i].Item2))
                                {
                                    isNew = false;

                                    break;
                                }
                            }

                            if (isNew)
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(tuple1.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                    {
                                        namePathList.Add(Tuple.Create<string, string, string>(attribute, tuple1.Item1, null));
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                                finally
                                {
                                    if (fs != null)
                                    {
                                        fs.Close();
                                    }
                                }
                            }
                        }
                    }

                    charactersMenuItem.Items.Clear();

                    namePathList.Sort(delegate (Tuple<string, string, string> tuple1, Tuple<string, string, string> tuple2)
                    {
                        return String.Compare(tuple1.Item1, tuple2.Item1, StringComparison.CurrentCulture);
                    });
                    namePathList.ForEach(delegate (Tuple<string, string, string> tuple1)
                    {
                        for (LinkedListNode<Tuple<Character, string>> nextLinkedListNode = characterLinkedList.First; nextLinkedListNode != null; nextLinkedListNode = nextLinkedListNode.Next)
                        {
                            if (nextLinkedListNode.Value.Item1.Name.Equals(tuple1.Item1) && nextLinkedListNode.Value.Item2.Equals(tuple1.Item2))
                            {
                                MenuItem selectedMenuItem = characterMenuItemList.Find(delegate (MenuItem menuItem)
                                {
                                    if (tuple1.Item1.Equals(menuItem.Header as string) && (menuItem.IsChecked || menuItem.HasItems))
                                    {
                                        Tuple<string, string> tuple2 = menuItem.Tag as Tuple<string, string>;

                                        return tuple2 != null && tuple1.Item2.Equals(tuple2.Item1) && (tuple1.Item3 == null && tuple2.Item2 == null || tuple1.Item3 != null && tuple2.Item2 != null && tuple1.Item3.Equals(tuple2.Item2));
                                    }

                                    return false;
                                });

                                if (selectedMenuItem == null)
                                {
                                    selectedMenuItem = new MenuItem();
                                    selectedMenuItem.Header = tuple1.Item1;
                                    selectedMenuItem.Tag = Tuple.Create<string, string>(tuple1.Item2, tuple1.Item3);
                                }
                                else
                                {
                                    selectedMenuItem.Items.Clear();
                                    characterMenuItemList.Remove(selectedMenuItem);
                                }

                                charactersMenuItem.Items.Add(selectedMenuItem);

                                List<MenuItem> childMenuItemList = new List<MenuItem>();
                                Dictionary<string, SortedSet<int>> dictionary = new Dictionary<string, SortedSet<int>>();
                                List<string> motionTypeList = new List<string>();

                                this.cachedMotionList.ForEach(delegate (Motion motion)
                                {
                                    if (motion.Type != null)
                                    {
                                        SortedSet<int> sortedSet;

                                        if (dictionary.TryGetValue(motion.Type, out sortedSet))
                                        {
                                            if (!sortedSet.Contains(motion.ZIndex))
                                            {
                                                sortedSet.Add(motion.ZIndex);
                                            }
                                        }
                                        else
                                        {
                                            sortedSet = new SortedSet<int>();
                                            sortedSet.Add(motion.ZIndex);
                                            dictionary.Add(motion.Type, sortedSet);
                                            motionTypeList.Add(motion.Type);
                                        }
                                    }
                                });

                                motionTypeList.Sort(delegate (string s1, string s2)
                                {
                                    return String.Compare(s1, s2, StringComparison.CurrentCulture);
                                });
                                motionTypeList.ForEach(delegate (string type)
                                {
                                    foreach (MenuItem menuItem in selectedMenuItem.Items)
                                    {
                                        if (type.Equals(menuItem.Header as string))
                                        {
                                            if (nextLinkedListNode.Value.Item1.HasTypes)
                                            {
                                                menuItem.IsChecked = nextLinkedListNode.Value.Item1.Types.Contains(menuItem.Header as string);
                                            }
                                            else
                                            {
                                                menuItem.IsChecked = false;
                                            }

                                            childMenuItemList.Add(menuItem);

                                            return;
                                        }
                                    }

                                    MenuItem childMenuItem = new MenuItem();

                                    childMenuItem.Header = type;
                                    childMenuItem.Tag = nextLinkedListNode.Value.Item1.Name;
                                    childMenuItem.Click += new RoutedEventHandler(delegate
                                    {
                                        string tag = childMenuItem.Tag as string;

                                        if (tag != null)
                                        {
                                            foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(tag) select c)
                                            {
                                                foreach (Window window in Application.Current.Windows)
                                                {
                                                    Agent a = window as Agent;

                                                    if (a != null && c.Name.Equals(a.characterName))
                                                    {
                                                        string header = childMenuItem.Header as string;

                                                        a.Render();

                                                        if (c.Types.Contains(header))
                                                        {
                                                            c.Types.Remove(header);
                                                        }
                                                        else if (header != null)
                                                        {
                                                            SortedSet<int> sortedSet1;

                                                            if (dictionary.TryGetValue(header, out sortedSet1))
                                                            {
                                                                foreach (string s in c.Types.ToArray())
                                                                {
                                                                    SortedSet<int> sortedSet2;

                                                                    if (dictionary.TryGetValue(s, out sortedSet2) && sortedSet1.SequenceEqual(sortedSet2))
                                                                    {
                                                                        c.Types.Remove(s);
                                                                    }
                                                                }
                                                            }

                                                            c.Types.Add(header);
                                                        }

                                                        foreach (Image image in a.Canvas.Children.Cast<Image>())
                                                        {
                                                            Image i = image;
                                                            Motion motion = i.Tag as Motion;

                                                            if (motion != null)
                                                            {
                                                                List<string> typeList = null;
                                                                bool isVisible;

                                                                if (motion.Type == null)
                                                                {
                                                                    typeList = new List<string>();
                                                                    a.cachedMotionList.ForEach(delegate (Motion m)
                                                                    {
                                                                        if (m.ZIndex == motion.ZIndex)
                                                                        {
                                                                            typeList.Add(m.Type);
                                                                        }
                                                                    });
                                                                }

                                                                if (typeList == null)
                                                                {
                                                                    if (c.HasTypes)
                                                                    {
                                                                        typeList = new List<string>();
                                                                        a.cachedMotionList.ForEach(delegate (Motion m)
                                                                        {
                                                                            if (m.ZIndex == motion.ZIndex && c.Types.Contains(m.Type))
                                                                            {
                                                                                typeList.Add(m.Type);
                                                                            }
                                                                        });
                                                                        isVisible = typeList.Count > 0 && typeList.LastIndexOf(motion.Type) == typeList.Count - 1;
                                                                    }
                                                                    else
                                                                    {
                                                                        isVisible = false;
                                                                    }
                                                                }
                                                                else if (c.HasTypes)
                                                                {
                                                                    isVisible = !typeList.Exists(delegate (string t)
                                                                    {
                                                                        return c.Types.Contains(t);
                                                                    });
                                                                }
                                                                else
                                                                {
                                                                    isVisible = true;
                                                                }

                                                                if (isVisible && (i.Visibility != Visibility.Visible || i.OpacityMask != null))
                                                                {
                                                                    LinearGradientBrush linearGradientBrush = i.OpacityMask as LinearGradientBrush;

                                                                    if (linearGradientBrush == null)
                                                                    {
                                                                        GradientStopCollection gradientStopCollection = new GradientStopCollection();

                                                                        gradientStopCollection.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
                                                                        gradientStopCollection.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));

                                                                        i.OpacityMask = linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0.5, 0), new Point(0.5, 1));
                                                                    }

                                                                    Storyboard storyboard;
                                                                    ColorAnimation colorAnimation1 = new ColorAnimation(linearGradientBrush.GradientStops[0].Color, Color.FromArgb(Byte.MaxValue, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                                                                    ColorAnimation colorAnimation2 = new ColorAnimation(linearGradientBrush.GradientStops[1].Color, Color.FromArgb(Byte.MaxValue, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                                                                    SineEase sineEase1 = new SineEase();
                                                                    SineEase sineEase2 = new SineEase();

                                                                    if (a.imageStoryboardDictionary.TryGetValue(i, out storyboard))
                                                                    {
                                                                        storyboard.Stop(i);
                                                                        a.imageStoryboardDictionary[i] = storyboard = new Storyboard();
                                                                    }
                                                                    else
                                                                    {
                                                                        storyboard = new Storyboard();
                                                                        a.imageStoryboardDictionary.Add(i, storyboard);
                                                                    }

                                                                    sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseInOut;

                                                                    colorAnimation1.EasingFunction = sineEase1;
                                                                    colorAnimation2.BeginTime = TimeSpan.FromMilliseconds(250);
                                                                    colorAnimation2.EasingFunction = sineEase2;

                                                                    storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                                                    {
                                                                        if (((Clock)s).CurrentState == ClockState.Filling)
                                                                        {
                                                                            i.OpacityMask = null;
                                                                            a.Render();
                                                                            storyboard.Remove(i);
                                                                            a.imageStoryboardDictionary.Remove(i);
                                                                        }
                                                                    });
                                                                    storyboard.Children.Add(colorAnimation1);
                                                                    storyboard.Children.Add(colorAnimation2);

                                                                    Storyboard.SetTargetProperty(colorAnimation1, new PropertyPath("(0).(1)[0].(2)", Image.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                                                                    Storyboard.SetTargetProperty(colorAnimation2, new PropertyPath("(0).(1)[1].(2)", Image.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));

                                                                    i.Visibility = Visibility.Visible;
                                                                    i.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                                                                }
                                                                else if (!isVisible && (i.Visibility == Visibility.Visible || i.OpacityMask != null))
                                                                {
                                                                    LinearGradientBrush linearGradientBrush = i.OpacityMask as LinearGradientBrush;

                                                                    if (linearGradientBrush == null)
                                                                    {
                                                                        GradientStopCollection gradientStopCollection = new GradientStopCollection();

                                                                        gradientStopCollection.Add(new GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 0));
                                                                        gradientStopCollection.Add(new GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1));

                                                                        i.OpacityMask = linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0.5, 0), new Point(0.5, 1));
                                                                    }

                                                                    Storyboard storyboard;
                                                                    ColorAnimation colorAnimation1 = new ColorAnimation(linearGradientBrush.GradientStops[0].Color, Color.FromArgb(0, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                                                                    ColorAnimation colorAnimation2 = new ColorAnimation(linearGradientBrush.GradientStops[1].Color, Color.FromArgb(0, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                                                                    SineEase sineEase1 = new SineEase();
                                                                    SineEase sineEase2 = new SineEase();

                                                                    if (a.imageStoryboardDictionary.TryGetValue(i, out storyboard))
                                                                    {
                                                                        storyboard.Stop(i);
                                                                        a.imageStoryboardDictionary[i] = storyboard = new Storyboard();
                                                                    }
                                                                    else
                                                                    {
                                                                        storyboard = new Storyboard();
                                                                        a.imageStoryboardDictionary.Add(i, storyboard);
                                                                    }

                                                                    sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseInOut;

                                                                    colorAnimation1.EasingFunction = sineEase1;
                                                                    colorAnimation2.BeginTime = TimeSpan.FromMilliseconds(250);
                                                                    colorAnimation2.EasingFunction = sineEase2;

                                                                    storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                                                    {
                                                                        if (((Clock)s).CurrentState == ClockState.Filling)
                                                                        {
                                                                            i.OpacityMask = null;
                                                                            a.Render();
                                                                            storyboard.Remove(i);
                                                                            a.imageStoryboardDictionary.Remove(i);
                                                                        }
                                                                    });
                                                                    storyboard.Children.Add(colorAnimation1);
                                                                    storyboard.Children.Add(colorAnimation2);

                                                                    Storyboard.SetTargetProperty(colorAnimation1, new PropertyPath("(0).(1)[0].(2)", Image.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                                                                    Storyboard.SetTargetProperty(colorAnimation2, new PropertyPath("(0).(1)[1].(2)", Image.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));

                                                                    i.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    });

                                    if (nextLinkedListNode.Value.Item1.HasTypes)
                                    {
                                        childMenuItem.IsChecked = nextLinkedListNode.Value.Item1.Types.Contains(childMenuItem.Header as string);
                                    }
                                    else
                                    {
                                        childMenuItem.IsChecked = false;
                                    }

                                    childMenuItemList.Add(childMenuItem);
                                });

                                selectedMenuItem.Items.Clear();

                                if (likeRequired)
                                {
                                    MenuItem menuItem = new MenuItem();

                                    menuItem.Header = Apricot.Resources.Like;
                                    menuItem.Click += new RoutedEventHandler(delegate
                                    {
                                        this.baseDateTime = this.baseDateTime.AddMinutes(30);

                                        foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                                        {
                                            System.Configuration.Configuration config1 = null;
                                            string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                                            FontFamily fontFamily;
                                            double fontSize;
                                            FontStretch fontStretch;
                                            FontStyle fontStyle;
                                            FontWeight fontWeight;
                                            Color foregroundColor;
                                            SolidColorBrush backgroundBrush;
                                            Geometry starGeometry = CreateStarGeometry(new Rect(0, 0, 8, 8));

                                            if (Directory.Exists(directory))
                                            {
                                                string filename = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                                                foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly) where filename.Equals(System.IO.Path.GetFileNameWithoutExtension(s)) select s)
                                                {
                                                    System.Configuration.ExeConfigurationFileMap exeConfigurationFileMap = new System.Configuration.ExeConfigurationFileMap();

                                                    exeConfigurationFileMap.ExeConfigFilename = s;
                                                    config1 = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);
                                                }
                                            }

                                            if (config1 == null)
                                            {
                                                config1 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                                                if (config1.AppSettings.Settings["FontFamily"] != null && config1.AppSettings.Settings["FontFamily"].Value.Length > 0)
                                                {
                                                    fontFamily = new FontFamily(config1.AppSettings.Settings["FontFamily"].Value);
                                                }
                                                else
                                                {
                                                    fontFamily = this.FontFamily;
                                                }

                                                if (config1.AppSettings.Settings["FontSize"] != null && config1.AppSettings.Settings["FontSize"].Value.Length > 0)
                                                {
                                                    fontSize = (double)new FontSizeConverter().ConvertFromString(config1.AppSettings.Settings["FontSize"].Value);
                                                }
                                                else
                                                {
                                                    fontSize = this.FontSize;
                                                }

                                                if (config1.AppSettings.Settings["FontStretch"] != null && config1.AppSettings.Settings["FontStretch"].Value.Length > 0)
                                                {
                                                    fontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config1.AppSettings.Settings["FontStretch"].Value);
                                                }
                                                else
                                                {
                                                    fontStretch = this.FontStretch;
                                                }

                                                if (config1.AppSettings.Settings["FontStyle"] != null && config1.AppSettings.Settings["FontStyle"].Value.Length > 0)
                                                {
                                                    fontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config1.AppSettings.Settings["FontStyle"].Value);
                                                }
                                                else
                                                {
                                                    fontStyle = this.FontStyle;
                                                }

                                                if (config1.AppSettings.Settings["FontWeight"] != null && config1.AppSettings.Settings["FontWeight"].Value.Length > 0)
                                                {
                                                    fontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config1.AppSettings.Settings["FontWeight"].Value);
                                                }
                                                else
                                                {
                                                    fontWeight = this.FontWeight;
                                                }

                                                if (config1.AppSettings.Settings["TextColor"] != null && config1.AppSettings.Settings["TextColor"].Value.Length > 0)
                                                {
                                                    foregroundColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["TextColor"].Value);
                                                }
                                                else
                                                {
                                                    foregroundColor = Colors.White;
                                                }
                                            }
                                            else
                                            {
                                                System.Configuration.Configuration config2 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                                                if (config1.AppSettings.Settings["FontFamily"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["FontFamily"] != null && config2.AppSettings.Settings["FontFamily"].Value.Length > 0)
                                                    {
                                                        fontFamily = new FontFamily(config2.AppSettings.Settings["FontFamily"].Value);
                                                    }
                                                    else
                                                    {
                                                        fontFamily = this.FontFamily;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["FontFamily"].Value.Length > 0)
                                                {
                                                    fontFamily = new FontFamily(config1.AppSettings.Settings["FontFamily"].Value);
                                                }
                                                else
                                                {
                                                    fontFamily = this.FontFamily;
                                                }

                                                if (config1.AppSettings.Settings["FontSize"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["FontSize"] != null && config2.AppSettings.Settings["FontSize"].Value.Length > 0)
                                                    {
                                                        fontSize = (double)new FontSizeConverter().ConvertFromString(config2.AppSettings.Settings["FontSize"].Value);
                                                    }
                                                    else
                                                    {
                                                        fontSize = this.FontSize;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["FontSize"].Value.Length > 0)
                                                {
                                                    fontSize = (double)new FontSizeConverter().ConvertFromString(config1.AppSettings.Settings["FontSize"].Value);
                                                }
                                                else
                                                {
                                                    fontSize = this.FontSize;
                                                }

                                                if (config1.AppSettings.Settings["FontStretch"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["FontStretch"] != null && config2.AppSettings.Settings["FontStretch"].Value.Length > 0)
                                                    {
                                                        fontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config2.AppSettings.Settings["FontStretch"].Value);
                                                    }
                                                    else
                                                    {
                                                        fontStretch = this.FontStretch;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["FontStretch"].Value.Length > 0)
                                                {
                                                    fontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config1.AppSettings.Settings["FontStretch"].Value);
                                                }
                                                else
                                                {
                                                    fontStretch = this.FontStretch;
                                                }

                                                if (config1.AppSettings.Settings["FontStyle"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["FontStyle"] != null && config2.AppSettings.Settings["FontStyle"].Value.Length > 0)
                                                    {
                                                        fontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config2.AppSettings.Settings["FontStyle"].Value);
                                                    }
                                                    else
                                                    {
                                                        fontStyle = this.FontStyle;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["FontStyle"].Value.Length > 0)
                                                {
                                                    fontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config1.AppSettings.Settings["FontStyle"].Value);
                                                }
                                                else
                                                {
                                                    fontStyle = this.FontStyle;
                                                }

                                                if (config1.AppSettings.Settings["FontWeight"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["FontWeight"] != null && config2.AppSettings.Settings["FontWeight"].Value.Length > 0)
                                                    {
                                                        fontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config2.AppSettings.Settings["FontWeight"].Value);
                                                    }
                                                    else
                                                    {
                                                        fontWeight = this.FontWeight;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["FontWeight"].Value.Length > 0)
                                                {
                                                    fontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config1.AppSettings.Settings["FontWeight"].Value);
                                                }
                                                else
                                                {
                                                    fontWeight = this.FontWeight;
                                                }

                                                if (config1.AppSettings.Settings["TextColor"] == null)
                                                {
                                                    if (config2.AppSettings.Settings["TextColor"] != null && config2.AppSettings.Settings["TextColor"].Value.Length > 0)
                                                    {
                                                        foregroundColor = (Color)ColorConverter.ConvertFromString(config2.AppSettings.Settings["TextColor"].Value);
                                                    }
                                                    else
                                                    {
                                                        foregroundColor = Colors.White;
                                                    }
                                                }
                                                else if (config1.AppSettings.Settings["TextColor"].Value.Length > 0)
                                                {
                                                    foregroundColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["TextColor"].Value);
                                                }
                                                else
                                                {
                                                    foregroundColor = Colors.White;
                                                }
                                            }

                                            character.Likes += 1;

                                            Script.Instance.TryEnqueue(Script.Instance.Prepare(from sequence in Script.Instance.Sequences where sequence.Name.Equals("Like") && sequence.Owner.Equals(this.characterName) select sequence, character.Likes.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                                            Script.Instance.TryEnqueue(Script.Instance.Prepare(from sequence in Script.Instance.Sequences where sequence.Name.Equals("Charge") && sequence.Owner.Equals(this.characterName) select sequence, ((int)((DateTime.Now - this.baseDateTime).TotalMinutes / 30)).ToString(System.Globalization.CultureInfo.InvariantCulture)));

                                            if (Math.Max(Math.Max(foregroundColor.R, foregroundColor.G), foregroundColor.B) > Byte.MaxValue / 2)
                                            {
                                                backgroundBrush = new SolidColorBrush(Colors.Black);
                                            }
                                            else
                                            {
                                                backgroundBrush = new SolidColorBrush(Colors.White);
                                            }

                                            backgroundBrush.Opacity = 0.75;

                                            if (backgroundBrush.CanFreeze)
                                            {
                                                backgroundBrush.Freeze();
                                            }

                                            if (starGeometry.CanFreeze)
                                            {
                                                starGeometry.Freeze();
                                            }

                                            CreateHudWindow(backgroundBrush, foregroundColor, starGeometry, fontFamily, fontSize, fontStretch, fontStyle, fontWeight, character.Likes.ToString(System.Globalization.CultureInfo.CurrentCulture)).Show();
                                        }
                                    });

                                    selectedMenuItem.IsChecked = false;

                                    if (childMenuItemList.Count > 0)
                                    {
                                        selectedMenuItem.Items.Add(menuItem);
                                        selectedMenuItem.Items.Add(new Separator());

                                        childMenuItemList.ForEach(delegate (MenuItem mi)
                                        {
                                            selectedMenuItem.Items.Add(mi);
                                        });
                                    }
                                    else
                                    {
                                        selectedMenuItem.Items.Add(menuItem);
                                    }
                                }
                                else if (childMenuItemList.Count > 0)
                                {
                                    selectedMenuItem.IsChecked = false;

                                    childMenuItemList.ForEach(delegate (MenuItem mi)
                                    {
                                        selectedMenuItem.Items.Add(mi);
                                    });
                                }
                                else
                                {
                                    selectedMenuItem.IsChecked = true;
                                }

                                characterLinkedList.Remove(nextLinkedListNode);

                                return;
                            }
                        }

                        MenuItem unselectedMenuItem = characterMenuItemList.Find(delegate (MenuItem menuItem)
                        {
                            if (tuple1.Item1.Equals(menuItem.Header as string) && !menuItem.IsChecked && !menuItem.HasItems)
                            {
                                Tuple<string, string> tuple2 = menuItem.Tag as Tuple<string, string>;

                                return tuple2 != null && tuple1.Item2.Equals(tuple2.Item1) && (tuple1.Item3 == null && tuple2.Item2 == null || tuple1.Item3 != null && tuple2.Item2 != null && tuple1.Item3.Equals(tuple2.Item2));
                            }

                            return false;
                        });

                        if (unselectedMenuItem == null)
                        {
                            unselectedMenuItem = new MenuItem();
                            unselectedMenuItem.Header = tuple1.Item1;
                            unselectedMenuItem.Tag = Tuple.Create<string, string>(tuple1.Item2, tuple1.Item3);
                            unselectedMenuItem.Click += new RoutedEventHandler(delegate
                            {
                                Tuple<string, string> tuple2 = unselectedMenuItem.Tag as Tuple<string, string>;

                                if (tuple2 != null)
                                {
                                    List<Character> characterList = new List<Character>();

                                    if (tuple2.Item2 == null)
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tuple2.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                            if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                            {
                                                stringBuilder.Append(Path.DirectorySeparatorChar);
                                            }

                                            string path = tuple2.Item1.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tuple2.Item1.Remove(0, stringBuilder.Length) : tuple2.Item1;

                                            foreach (string a in from a in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select a.Value)
                                            {
                                                Character character = new Character();

                                                character.Name = a;
                                                character.Script = path;

                                                characterList.Add(character);
                                            }
                                        }
                                        catch
                                        {
                                            return;
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tuple2.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            using (ZipArchive zipArchive = new ZipArchive(fs))
                                            {
                                                fs = null;

                                                StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                                if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                                {
                                                    stringBuilder.Append(Path.DirectorySeparatorChar);
                                                }

                                                string path = tuple2.Item1.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tuple2.Item1.Remove(0, stringBuilder.Length) : tuple2.Item1;

                                                foreach (ZipArchiveEntry zipArchiveEntry in from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.Equals(tuple2.Item2) select zipArchiveEntry)
                                                {
                                                    Stream stream = null;

                                                    try
                                                    {
                                                        stream = zipArchiveEntry.Open();

                                                        foreach (string a in from a in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select a.Value)
                                                        {
                                                            Character character = new Character();

                                                            character.Name = a;
                                                            character.Script = path;

                                                            characterList.Add(character);
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        return;
                                                    }
                                                    finally
                                                    {
                                                        if (stream != null)
                                                        {
                                                            stream.Close();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }
                                    }

                                    if (characterList.Count > 0)
                                    {
                                        Switch(characterList);
                                    }
                                }
                            });
                        }
                        else
                        {
                            characterMenuItemList.Remove(unselectedMenuItem);
                        }

                        charactersMenuItem.Items.Add(unselectedMenuItem);
                    });

                    if (Script.Instance.Sources.Count > 0)
                    {
                        List<Source> sourceList = Script.Instance.Sources.ToList();
                        List<MenuItem> sourceMenuItemList = new List<MenuItem>();

                        foreach (Control control in sourcesMenuItem.Items)
                        {
                            MenuItem menuItem = control as MenuItem;

                            if (menuItem != null)
                            {
                                sourceMenuItemList.Add(menuItem);
                            }
                        }

                        sourcesMenuItem.Items.Clear();
                        sourcesMenuItem.IsEnabled = true;

                        if (NativeMethods.IsClipboardFormatAvailable(CF_UNICODETEXT) && NativeMethods.OpenClipboard(IntPtr.Zero))
                        {
                            IntPtr handle = NativeMethods.GetClipboardData(CF_UNICODETEXT);

                            if (handle != IntPtr.Zero)
                            {
                                IntPtr lpwstr = NativeMethods.GlobalLock(handle);

                                if (lpwstr != IntPtr.Zero)
                                {
                                    Uri uri;

                                    if (Uri.TryCreate(System.Runtime.InteropServices.Marshal.PtrToStringUni(lpwstr).Trim(), UriKind.Absolute, out uri))
                                    {
                                        MenuItem sourceMenuItem = new MenuItem();
                                        MenuItem addMenuItem = new MenuItem();

                                        sourceMenuItem.Header = uri.ToString();
                                        sourceMenuItem.Tag = uri;

                                        sourcesMenuItem.Items.Add(sourceMenuItem);
                                        sourcesMenuItem.Items.Add(new Separator());

                                        addMenuItem.Header = Apricot.Resources.Add;
                                        addMenuItem.Click += new RoutedEventHandler(delegate
                                        {
                                            Uri u = sourceMenuItem.Tag as Uri;

                                            if (u != null)
                                            {
                                                Script.Instance.Sources.Add(new Source(u));
                                            }
                                        });

                                        sourceMenuItem.Items.Add(addMenuItem);
                                    }
                                }

                                NativeMethods.GlobalUnlock(handle);
                            }

                            NativeMethods.CloseClipboard();
                        }

                        sourceList.Sort(delegate (Source source1, Source source2)
                        {
                            return String.Compare(source1.Name, source2.Name, StringComparison.CurrentCulture);
                        });
                        sourceList.ForEach(delegate (Source source)
                        {
                            MenuItem sourceMenuItem = sourceMenuItemList.Find(delegate (MenuItem menuItem)
                            {
                                return source == menuItem.Tag;
                            });

                            if (sourceMenuItem == null)
                            {
                                MenuItem removeMenuItem = new MenuItem();

                                sourceMenuItem = new MenuItem();
                                sourceMenuItem.Header = String.IsNullOrEmpty(source.Name) ? source.Location.ToString() : source.Name;
                                sourceMenuItem.Tag = source;

                                removeMenuItem.Header = Apricot.Resources.Remove;
                                removeMenuItem.Click += new RoutedEventHandler(delegate
                                {
                                    Source s = sourceMenuItem.Tag as Source;

                                    if (s != null)
                                    {
                                        Script.Instance.Sources.Remove(s);
                                    }
                                });

                                sourceMenuItem.Items.Add(removeMenuItem);
                            }
                            else
                            {
                                sourceMenuItem.Header = String.IsNullOrEmpty(source.Name) ? source.Location.ToString() : source.Name;
                                sourceMenuItemList.Remove(sourceMenuItem);
                            }

                            sourcesMenuItem.Items.Add(sourceMenuItem);
                        });
                    }
                    else
                    {
                        MenuItem sourceMenuItem = null;

                        sourcesMenuItem.Items.Clear();

                        if (NativeMethods.IsClipboardFormatAvailable(CF_UNICODETEXT) && NativeMethods.OpenClipboard(IntPtr.Zero))
                        {
                            IntPtr handle = NativeMethods.GetClipboardData(CF_UNICODETEXT);

                            if (handle != IntPtr.Zero)
                            {
                                IntPtr lpwstr = NativeMethods.GlobalLock(handle);

                                if (lpwstr != IntPtr.Zero)
                                {
                                    Uri uri;

                                    if (Uri.TryCreate(System.Runtime.InteropServices.Marshal.PtrToStringUni(lpwstr).Trim(), UriKind.Absolute, out uri))
                                    {
                                        sourceMenuItem = new MenuItem();
                                        sourceMenuItem.Header = uri.ToString();
                                        sourceMenuItem.Tag = uri;
                                    }
                                }

                                NativeMethods.GlobalUnlock(handle);
                            }

                            NativeMethods.CloseClipboard();
                        }

                        if (sourceMenuItem == null)
                        {
                            sourcesMenuItem.IsEnabled = false;
                        }
                        else
                        {
                            MenuItem addMenuItem = new MenuItem();

                            sourcesMenuItem.IsEnabled = true;
                            sourcesMenuItem.Items.Add(sourceMenuItem);

                            addMenuItem.Header = Apricot.Resources.Add;
                            addMenuItem.Click += new RoutedEventHandler(delegate
                            {
                                Uri u = sourceMenuItem.Tag as Uri;

                                if (u != null)
                                {
                                    Script.Instance.Sources.Add(new Source(u));
                                }
                            });

                            sourceMenuItem.Items.Add(addMenuItem);
                        }
                    }
                }
            });

            if (this == Application.Current.MainWindow)
            {
                System.Configuration.Configuration config1 = null;
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

                if (Directory.Exists(directory))
                {
                    string filename = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly) where filename.Equals(Path.GetFileNameWithoutExtension(s)) select s)
                    {
                        System.Configuration.ExeConfigurationFileMap exeConfigurationFileMap = new System.Configuration.ExeConfigurationFileMap();

                        exeConfigurationFileMap.ExeConfigFilename = s;
                        config1 = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);
                    }
                }

                if (config1 == null)
                {
                    config1 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                    if (config1.AppSettings.Settings["Left"] != null && config1.AppSettings.Settings["Top"] != null && config1.AppSettings.Settings["Left"].Value.Length > 0 && config1.AppSettings.Settings["Top"].Value.Length > 0)
                    {
                        double left = Double.Parse(config1.AppSettings.Settings["Left"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        double top = Double.Parse(config1.AppSettings.Settings["Top"].Value, System.Globalization.CultureInfo.InvariantCulture);

                        if (left <= SystemParameters.VirtualScreenWidth && top <= SystemParameters.VirtualScreenHeight)
                        {
                            this.Left = left;
                            this.Top = top;
                        }
                    }

                    if (config1.AppSettings.Settings["Opacity"] != null && config1.AppSettings.Settings["Opacity"].Value.Length > 0)
                    {
                        this.opacity = Double.Parse(config1.AppSettings.Settings["Opacity"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config1.AppSettings.Settings["Scale"] != null && config1.AppSettings.Settings["Scale"].Value.Length > 0)
                    {
                        this.scale = this.ZoomScaleTransform.ScaleX = this.ZoomScaleTransform.ScaleY = Double.Parse(config1.AppSettings.Settings["Scale"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config1.AppSettings.Settings["Topmost"] != null && config1.AppSettings.Settings["Topmost"].Value.Length > 0)
                    {
                        this.Topmost = Boolean.Parse(config1.AppSettings.Settings["Topmost"].Value);
                    }

                    if (config1.AppSettings.Settings["ShowInTaskbar"] != null && config1.AppSettings.Settings["ShowInTaskbar"].Value.Length > 0)
                    {
                        this.ShowInTaskbar = Boolean.Parse(config1.AppSettings.Settings["ShowInTaskbar"].Value);
                    }

                    if (config1.AppSettings.Settings["DropShadow"] != null && config1.AppSettings.Settings["DropShadow"].Value.Length > 0 && Boolean.Parse(config1.AppSettings.Settings["DropShadow"].Value))
                    {
                        DropShadowEffect dropShadowEffect = new DropShadowEffect();

                        dropShadowEffect.Color = Colors.Black;
                        dropShadowEffect.BlurRadius = 10;
                        dropShadowEffect.Direction = 270;
                        dropShadowEffect.ShadowDepth = 0;
                        dropShadowEffect.Opacity = 0.5;

                        if (dropShadowEffect.CanFreeze)
                        {
                            dropShadowEffect.Freeze();
                        }

                        this.Canvas.Effect = dropShadowEffect;
                    }

                    if (config1.AppSettings.Settings["Mute"] != null && config1.AppSettings.Settings["Mute"].Value.Length > 0)
                    {
                        this.isMute = Boolean.Parse(config1.AppSettings.Settings["Mute"].Value);
                    }

                    if (config1.AppSettings.Settings["FrameRate"] != null && config1.AppSettings.Settings["FrameRate"].Value.Length > 0)
                    {
                        this.frameRate = Double.Parse(config1.AppSettings.Settings["FrameRate"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    System.Configuration.Configuration config2 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                    if (config1.AppSettings.Settings["Left"] == null || config1.AppSettings.Settings["Top"] == null)
                    {
                        if (config2.AppSettings.Settings["Left"] != null && config2.AppSettings.Settings["Top"] != null && config2.AppSettings.Settings["Left"].Value.Length > 0 && config2.AppSettings.Settings["Top"].Value.Length > 0)
                        {
                            double left = Double.Parse(config1.AppSettings.Settings["Left"].Value, System.Globalization.CultureInfo.InvariantCulture);
                            double top = Double.Parse(config1.AppSettings.Settings["Top"].Value, System.Globalization.CultureInfo.InvariantCulture);

                            if (left <= SystemParameters.VirtualScreenWidth && top <= SystemParameters.VirtualScreenHeight)
                            {
                                this.Left = left;
                                this.Top = top;
                            }
                        }
                    }
                    else if (config1.AppSettings.Settings["Left"].Value.Length > 0 && config1.AppSettings.Settings["Top"].Value.Length > 0)
                    {
                        double left = Double.Parse(config1.AppSettings.Settings["Left"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        double top = Double.Parse(config1.AppSettings.Settings["Top"].Value, System.Globalization.CultureInfo.InvariantCulture);

                        if (left <= SystemParameters.VirtualScreenWidth && top <= SystemParameters.VirtualScreenHeight)
                        {
                            this.Left = left;
                            this.Top = top;
                        }
                    }

                    if (config1.AppSettings.Settings["Opacity"] == null)
                    {
                        if (config2.AppSettings.Settings["Opacity"] != null && config2.AppSettings.Settings["Opacity"].Value.Length > 0)
                        {
                            this.opacity = Double.Parse(config2.AppSettings.Settings["Opacity"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    else if (config1.AppSettings.Settings["Opacity"].Value.Length > 0)
                    {
                        this.opacity = Double.Parse(config1.AppSettings.Settings["Opacity"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config1.AppSettings.Settings["Scale"] == null)
                    {
                        if (config2.AppSettings.Settings["Scale"] != null && config2.AppSettings.Settings["Scale"].Value.Length > 0)
                        {
                            this.scale = this.ZoomScaleTransform.ScaleX = this.ZoomScaleTransform.ScaleY = Double.Parse(config2.AppSettings.Settings["Scale"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    else if (config1.AppSettings.Settings["Scale"].Value.Length > 0)
                    {
                        this.scale = this.ZoomScaleTransform.ScaleX = this.ZoomScaleTransform.ScaleY = Double.Parse(config1.AppSettings.Settings["Scale"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config1.AppSettings.Settings["Topmost"] == null)
                    {
                        if (config2.AppSettings.Settings["Topmost"] != null && config2.AppSettings.Settings["Topmost"].Value.Length > 0)
                        {
                            this.Topmost = Boolean.Parse(config2.AppSettings.Settings["Topmost"].Value);
                        }
                    }
                    else if (config1.AppSettings.Settings["Topmost"].Value.Length > 0)
                    {
                        this.Topmost = Boolean.Parse(config1.AppSettings.Settings["Topmost"].Value);
                    }

                    if (config1.AppSettings.Settings["ShowInTaskbar"] == null)
                    {
                        if (config2.AppSettings.Settings["ShowInTaskbar"] != null && config2.AppSettings.Settings["ShowInTaskbar"].Value.Length > 0)
                        {
                            this.ShowInTaskbar = Boolean.Parse(config2.AppSettings.Settings["ShowInTaskbar"].Value);
                        }
                    }
                    else if (config1.AppSettings.Settings["ShowInTaskbar"].Value.Length > 0)
                    {
                        this.ShowInTaskbar = Boolean.Parse(config1.AppSettings.Settings["ShowInTaskbar"].Value);
                    }

                    if (config1.AppSettings.Settings["DropShadow"] == null)
                    {
                        if (config2.AppSettings.Settings["DropShadow"] != null && config2.AppSettings.Settings["DropShadow"].Value.Length > 0 && Boolean.Parse(config2.AppSettings.Settings["DropShadow"].Value))
                        {
                            DropShadowEffect dropShadowEffect = new DropShadowEffect();

                            dropShadowEffect.Color = Colors.Black;
                            dropShadowEffect.BlurRadius = 10;
                            dropShadowEffect.Direction = 270;
                            dropShadowEffect.ShadowDepth = 0;
                            dropShadowEffect.Opacity = 0.5;

                            if (dropShadowEffect.CanFreeze)
                            {
                                dropShadowEffect.Freeze();
                            }

                            this.Canvas.Effect = dropShadowEffect;
                        }
                    }
                    else if (config1.AppSettings.Settings["DropShadow"].Value.Length > 0)
                    {
                        if (Boolean.Parse(config1.AppSettings.Settings["DropShadow"].Value))
                        {
                            DropShadowEffect dropShadowEffect = new DropShadowEffect();

                            dropShadowEffect.Color = Colors.Black;
                            dropShadowEffect.BlurRadius = 10;
                            dropShadowEffect.Direction = 270;
                            dropShadowEffect.ShadowDepth = 0;
                            dropShadowEffect.Opacity = 0.5;

                            if (dropShadowEffect.CanFreeze)
                            {
                                dropShadowEffect.Freeze();
                            }

                            this.Canvas.Effect = dropShadowEffect;
                        }
                    }

                    if (config1.AppSettings.Settings["Mute"] == null)
                    {
                        if (config2.AppSettings.Settings["Mute"] != null && config2.AppSettings.Settings["Mute"].Value.Length > 0)
                        {
                            this.isMute = Boolean.Parse(config2.AppSettings.Settings["Mute"].Value);
                        }
                    }
                    else if (config1.AppSettings.Settings["Mute"].Value.Length > 0)
                    {
                        this.isMute = Boolean.Parse(config1.AppSettings.Settings["Mute"].Value);
                    }

                    if (config1.AppSettings.Settings["FrameRate"] == null)
                    {
                        if (config2.AppSettings.Settings["FrameRate"] != null && config2.AppSettings.Settings["FrameRate"].Value.Length > 0)
                        {
                            this.frameRate = Double.Parse(config2.AppSettings.Settings["FrameRate"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    else if (config1.AppSettings.Settings["FrameRate"].Value.Length > 0)
                    {
                        this.frameRate = Double.Parse(config1.AppSettings.Settings["FrameRate"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            else
            {
                Agent agent = Application.Current.MainWindow as Agent;

                if (agent != null)
                {
                    this.opacity = agent.opacity;
                    this.scale = this.ZoomScaleTransform.ScaleX = this.ZoomScaleTransform.ScaleY = agent.scale;
                    this.Topmost = agent.Topmost;
                    this.ShowInTaskbar = agent.ShowInTaskbar;

                    if (agent.Canvas.Effect != null)
                    {
                        DropShadowEffect dropShadowEffect = new DropShadowEffect();

                        dropShadowEffect.Color = Colors.Black;
                        dropShadowEffect.BlurRadius = 10;
                        dropShadowEffect.Direction = 270;
                        dropShadowEffect.ShadowDepth = 0;
                        dropShadowEffect.Opacity = 0.5;

                        if (dropShadowEffect.CanFreeze)
                        {
                            dropShadowEffect.Freeze();
                        }

                        this.Canvas.Effect = dropShadowEffect;
                    }

                    this.isMute = agent.isMute;
                    this.frameRate = agent.frameRate;
                }
            }

            this.balloon = new Balloon();
            this.balloon.Title = this.Title;
            this.balloon.SizeChanged += new SizeChangedEventHandler(delegate (object s, SizeChangedEventArgs e)
            {
                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                {
                    this.balloon.Left = this.Left + (this.Width - e.NewSize.Width) / 2;
                    this.balloon.Top = this.Top - e.NewSize.Height + character.Origin.Y * this.ZoomScaleTransform.ScaleY;
                }
            });
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                Run();

                return null;
            }), null);

            Microsoft.Win32.SystemEvents.PowerModeChanged += new Microsoft.Win32.PowerModeChangedEventHandler(this.OnPowerModeChanged);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            System.Windows.Interop.HwndSource hwndSource = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;

            if (hwndSource != null)
            {
                const int MOD_CONTROL = 0x0002;
                const int VK_F12 = 0x7B;
                IntPtr hWnd = NativeMethods.SetClipboardViewer(hwndSource.Handle);

                if (hWnd != IntPtr.Zero)
                {
                    this.nextClipboardViewer = new Nullable<IntPtr>(hWnd);
                }

                if (this == Application.Current.MainWindow)
                {
                    this.hotKeyID = new Nullable<int>(GetHashCode());

                    NativeMethods.RegisterHotKey(hwndSource.Handle, this.hotKeyID.Value, MOD_CONTROL, VK_F12);
                }

                hwndSource.AddHook(new System.Windows.Interop.HwndSourceHook(this.WndProc));
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x30D;
            const int WM_HOTKEY = 0x0312;
            const int WM_DESTROY = 0x0002;

            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    OnDrawClipboard(new EventArgs());

                    if (this.nextClipboardViewer.HasValue)
                    {
                        NativeMethods.SendMessage(this.nextClipboardViewer.Value, (UInt32)msg, wParam, lParam);
                    }

                    break;

                case WM_CHANGECBCHAIN:
                    if (this.nextClipboardViewer.HasValue)
                    {
                        if (wParam == this.nextClipboardViewer.Value)
                        {
                            this.nextClipboardViewer = lParam;
                        }
                        else
                        {
                            NativeMethods.SendMessage(this.nextClipboardViewer.Value, (UInt32)msg, wParam, lParam);
                        }
                    }

                    break;

                case WM_HOTKEY:
                    if (this.isVisible)
                    {
                        Storyboard storyboard1 = new Storyboard();
                        DoubleAnimation doubleAnimation1 = new DoubleAnimation(this.Opacity, 0, TimeSpan.FromMilliseconds(500));
                        DoubleAnimation doubleAnimation2 = new DoubleAnimation(this.balloon.Opacity, 0, TimeSpan.FromMilliseconds(500));
                        SineEase sineEase1 = new SineEase();
                        SineEase sineEase2 = new SineEase();

                        foreach (KeyValuePair<Storyboard, Window> kvp in this.fadeStoryboardDictionary)
                        {
                            kvp.Key.Stop(kvp.Value);
                        }

                        this.fadeStoryboardDictionary.Clear();

                        sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseIn;

                        doubleAnimation1.EasingFunction = sineEase1;
                        doubleAnimation2.EasingFunction = sineEase2;

                        storyboard1.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                        {
                            if (((Clock)s).CurrentState == ClockState.Filling)
                            {
                                this.Opacity = this.balloon.Opacity = 0;
                                storyboard1.Remove(this);
                                this.fadeStoryboardDictionary.Remove(storyboard1);

                                Hide();
                            }
                        });
                        storyboard1.Children.Add(doubleAnimation1);
                        storyboard1.Children.Add(doubleAnimation2);

                        Storyboard.SetTarget(doubleAnimation2, this.balloon);
                        Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(Window.OpacityProperty));
                        Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Window.OpacityProperty));

                        this.fadeStoryboardDictionary.Add(storyboard1, this);
                        this.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, true);

                        foreach (Window window in this.OwnedWindows)
                        {
                            Agent agent = window as Agent;

                            if (agent != null)
                            {
                                Storyboard storyboard2 = new Storyboard();
                                DoubleAnimation doubleAnimation3 = new DoubleAnimation(agent.Opacity, 0, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation4 = new DoubleAnimation(agent.balloon.Opacity, 0, TimeSpan.FromMilliseconds(500));
                                SineEase sineEase3 = new SineEase();
                                SineEase sineEase4 = new SineEase();

                                foreach (KeyValuePair<Storyboard, Window> kvp in agent.fadeStoryboardDictionary)
                                {
                                    kvp.Key.Stop(kvp.Value);
                                }

                                agent.fadeStoryboardDictionary.Clear();

                                sineEase3.EasingMode = sineEase4.EasingMode = EasingMode.EaseIn;

                                doubleAnimation3.EasingFunction = sineEase3;
                                doubleAnimation4.EasingFunction = sineEase4;

                                storyboard2.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                {
                                    if (((Clock)s).CurrentState == ClockState.Filling)
                                    {
                                        agent.Opacity = agent.balloon.Opacity = 0;
                                        storyboard2.Remove(agent);
                                        agent.fadeStoryboardDictionary.Remove(storyboard2);
                                        agent.Hide();
                                    }
                                });
                                storyboard2.Children.Add(doubleAnimation3);
                                storyboard2.Children.Add(doubleAnimation4);

                                Storyboard.SetTarget(doubleAnimation4, agent.balloon);
                                Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(Window.OpacityProperty));
                                Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(Window.OpacityProperty));

                                agent.fadeStoryboardDictionary.Add(storyboard2, agent);
                                agent.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, true);
                            }
                        }

                        this.isVisible = false;
                    }
                    else
                    {
                        foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                        {
                            this.Canvas.Width = character.Size.Width;
                            this.Canvas.Height = character.Size.Height;
                            this.LayoutRoot.Width = character.Size.Width * this.ZoomScaleTransform.ScaleX;
                            this.LayoutRoot.Height = character.Size.Height * this.ZoomScaleTransform.ScaleY;
                        }

                        Show();
                        Render();

                        Storyboard storyboard1 = new Storyboard();
                        DoubleAnimation doubleAnimation1 = new DoubleAnimation(this.Opacity, this.opacity, TimeSpan.FromMilliseconds(500));
                        DoubleAnimation doubleAnimation2 = new DoubleAnimation(this.balloon.Opacity, 1, TimeSpan.FromMilliseconds(500));
                        SineEase sineEase1 = new SineEase();
                        SineEase sineEase2 = new SineEase();

                        foreach (KeyValuePair<Storyboard, Window> kvp in this.fadeStoryboardDictionary)
                        {
                            kvp.Key.Stop(kvp.Value);
                        }

                        this.fadeStoryboardDictionary.Clear();

                        sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseOut;

                        doubleAnimation1.EasingFunction = sineEase1;
                        doubleAnimation2.EasingFunction = sineEase2;

                        storyboard1.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                        {
                            if (((Clock)s).CurrentState == ClockState.Filling)
                            {
                                this.Opacity = this.opacity;
                                this.balloon.Opacity = 1;
                                storyboard1.Remove(this);
                                this.fadeStoryboardDictionary.Remove(storyboard1);
                            }
                        });
                        storyboard1.Children.Add(doubleAnimation1);
                        storyboard1.Children.Add(doubleAnimation2);

                        Storyboard.SetTarget(doubleAnimation2, this.balloon);
                        Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(Window.OpacityProperty));
                        Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Window.OpacityProperty));

                        this.fadeStoryboardDictionary.Add(storyboard1, this);
                        this.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, true);

                        foreach (Window window in this.OwnedWindows)
                        {
                            Agent agent = window as Agent;

                            if (agent != null)
                            {
                                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select character)
                                {
                                    agent.Width = character.Size.Width * agent.ZoomScaleTransform.ScaleX;
                                    agent.Height = character.Size.Height * agent.ZoomScaleTransform.ScaleY;
                                }

                                agent.Show();

                                Storyboard storyboard2 = new Storyboard();
                                DoubleAnimation doubleAnimation3 = new DoubleAnimation(agent.Opacity, agent.opacity, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation4 = new DoubleAnimation(agent.balloon.Opacity, 1, TimeSpan.FromMilliseconds(500));
                                SineEase sineEase3 = new SineEase();
                                SineEase sineEase4 = new SineEase();

                                foreach (KeyValuePair<Storyboard, Window> kvp in agent.fadeStoryboardDictionary)
                                {
                                    kvp.Key.Stop(kvp.Value);
                                }

                                agent.fadeStoryboardDictionary.Clear();

                                sineEase3.EasingMode = sineEase4.EasingMode = EasingMode.EaseOut;

                                doubleAnimation3.EasingFunction = sineEase3;
                                doubleAnimation4.EasingFunction = sineEase4;

                                storyboard2.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                                {
                                    if (((Clock)s).CurrentState == ClockState.Filling)
                                    {
                                        agent.Opacity = agent.opacity;
                                        agent.balloon.Opacity = 1;
                                        storyboard2.Remove(agent);
                                        agent.fadeStoryboardDictionary.Remove(storyboard2);
                                    }
                                });
                                storyboard2.Children.Add(doubleAnimation3);
                                storyboard2.Children.Add(doubleAnimation4);

                                Storyboard.SetTarget(doubleAnimation4, agent.balloon);
                                Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(Window.OpacityProperty));
                                Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(Window.OpacityProperty));

                                agent.fadeStoryboardDictionary.Add(storyboard2, agent);
                                agent.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, true);
                            }
                        }

                        this.isVisible = true;
                    }

                    break;

                case WM_DESTROY:
                    if (this.nextClipboardViewer.HasValue)
                    {
                        NativeMethods.ChangeClipboardChain(hwnd, this.nextClipboardViewer.Value);
                    }

                    if (this.hotKeyID.HasValue)
                    {
                        NativeMethods.UnregisterHotKey(hwnd, this.hotKeyID.Value);
                    }

                    break;
            }

            return IntPtr.Zero;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.balloon.Owner = this;

            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
            {
                Point point = new Point(character.Location.X + character.BaseLocation.X, character.Location.Y + character.BaseLocation.Y);

                if (this == Application.Current.MainWindow)
                {
                    this.Left += point.X;
                    this.Top += point.Y;
                }
                else
                {
                    Agent agent = Application.Current.MainWindow as Agent;

                    if (agent != null)
                    {
                        foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                        {
                            this.Left = agent.Left - c.Location.X - c.BaseLocation.X + point.X;
                            this.Top = agent.Top - c.Location.Y - c.BaseLocation.Y + point.Y;
                        }
                    }
                }

                this.Canvas.Width = character.Size.Width;
                this.Canvas.Height = character.Size.Height;
                this.LayoutRoot.Width = character.Size.Width * this.ZoomScaleTransform.ScaleX;
                this.LayoutRoot.Height = character.Size.Height * this.ZoomScaleTransform.ScaleY;

                if (character.Mirror)
                {
                    this.FlipScaleTransform.ScaleX = -1;
                }
                else if (!character.Mirror)
                {
                    this.FlipScaleTransform.ScaleX = 1;
                }
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                this.LayoutRoot.Width = this.LayoutRoot.Height = this.Canvas.Width = this.Canvas.Height = 0;
                this.Canvas.Children.Clear();
                this.balloon.Hide();
            }
        }

        public void Switch(IEnumerable<Character> characters)
        {
            Agent agent = Application.Current.MainWindow as Agent;

            if (agent != null)
            {
                Storyboard storyboard = new Storyboard();
                DoubleAnimation doubleAnimation1 = new DoubleAnimation(agent.Opacity, 0, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation2 = new DoubleAnimation(agent.balloon.Opacity, 0, TimeSpan.FromMilliseconds(500));
                SineEase sineEase1 = new SineEase();
                SineEase sineEase2 = new SineEase();

                foreach (KeyValuePair<Storyboard, Window> kvp in agent.fadeStoryboardDictionary)
                {
                    kvp.Key.Stop(kvp.Value);
                }

                agent.fadeStoryboardDictionary.Clear();

                sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseIn;

                doubleAnimation1.EasingFunction = sineEase1;
                doubleAnimation2.EasingFunction = sineEase2;

                storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                {
                    if (((Clock)s).CurrentState == ClockState.Filling)
                    {
                        List<string> pathList = new List<string>();
                        List<string> nameList = new List<string>();
                        bool isCleared;
                        bool isOwner = true;
                        Nullable<Point> point = null;
                        string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                        foreach (Nullable<Point> p in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select new Nullable<Point>(new Point(agent.Left - character.Location.X - character.BaseLocation.X, agent.Top - character.Location.Y - character.BaseLocation.Y)))
                        {
                            point = p;
                        }

                        agent.fadeStoryboardDictionary.Clear();

                        if (agent.scaleStoryboard != null)
                        {
                            agent.scaleStoryboard.Remove(agent.LayoutRoot);
                            agent.scaleStoryboard = null;
                        }

                        if (agent.mirrorStoryboard != null)
                        {
                            agent.mirrorStoryboard.Remove(agent.Canvas);
                            agent.mirrorStoryboard = null;
                        }

                        foreach (KeyValuePair<Image, Storyboard> kvp in agent.imageStoryboardDictionary)
                        {
                            kvp.Value.Remove(kvp.Key);
                        }

                        agent.imageStoryboardDictionary.Clear();
                        agent.Canvas.OpacityMask = null;
                        agent.LayoutRoot.Width = agent.LayoutRoot.Height = agent.Canvas.Width = agent.Canvas.Height = 0;
                        agent.balloon.Hide();

                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window != agent)
                            {
                                Agent a = window as Agent;

                                if (a != null)
                                {
                                    a.Owner = null;
                                    a.LayoutRoot.Width = a.LayoutRoot.Height = a.Canvas.Width = a.Canvas.Height = 0;
                                    a.balloon.Hide();
                                    a.Close();
                                }
                            }
                        }

                        foreach (Character character in Script.Instance.Characters.ToArray())
                        {
                            nameList.Add(character.Name);
                            Script.Instance.Characters.Remove(character);
                        }

                        do
                        {
                            isCleared = true;
                            nameList.ForEach(delegate (string name)
                            {
                                Sequence sequence;

                                if (Script.Instance.TryDequeue(name, out sequence))
                                {
                                    isCleared = false;
                                }
                            });
                        } while (!isCleared);

                        foreach (Character character in characters)
                        {
                            if (Path.IsPathRooted(character.Script))
                            {
                                if (!pathList.Exists(delegate (string path)
                                {
                                    if (!Path.IsPathRooted(path))
                                    {
                                        return character.Script.Equals(Path.Combine(directory, path));
                                    }

                                    return character.Script.Equals(path);
                                }))
                                {
                                    pathList.Add(character.Script);
                                }
                            }
                            else
                            {
                                string path1 = Path.Combine(directory, character.Script);

                                if (!pathList.Exists(delegate (string path2)
                                {
                                    if (Path.IsPathRooted(path2))
                                    {
                                        return path1.Equals(path2);
                                    }

                                    return character.Script.Equals(path2);
                                }))
                                {
                                    pathList.Add(path1);
                                }
                            }

                            Script.Instance.Characters.Add(character);
                        }

                        pathList.ForEach(delegate (string path)
                        {
                            Script.Instance.Parse(path);
                        });

                        agent.cachedBitmapImageDictionary.Clear();
                        agent.cachedMotionList.Clear();
                        agent.Render();
                        agent.queue.Clear();
                        agent.motionQueue.Clear();
                        agent.isFirst = true;
                        agent.isLast = false;
                        agent.balloon.Messages.Clear();

                        foreach (Character character in Script.Instance.Characters)
                        {
                            if (isOwner)
                            {
                                Point p = new Point(character.Location.X + character.BaseLocation.X, character.Location.Y + character.BaseLocation.Y);

                                agent.characterName = character.Name;

                                if (point.HasValue)
                                {
                                    agent.Left = point.Value.X + p.X;
                                    agent.Top = point.Value.Y + p.Y;
                                }

                                agent.Canvas.Width = character.Size.Width;
                                agent.Canvas.Height = character.Size.Height;
                                agent.LayoutRoot.Width = character.Size.Width * agent.ZoomScaleTransform.ScaleX;
                                agent.LayoutRoot.Height = character.Size.Height * agent.ZoomScaleTransform.ScaleY;
                                agent.balloon.Left = agent.Left + (character.Size.Width * agent.ZoomScaleTransform.ScaleX - agent.balloon.Width) / 2;
                                agent.balloon.Top = agent.Top - agent.balloon.Height + character.Origin.Y * agent.ZoomScaleTransform.ScaleY;

                                if (character.Mirror)
                                {
                                    agent.FlipScaleTransform.ScaleX = -1;
                                }
                                else if (!character.Mirror)
                                {
                                    agent.FlipScaleTransform.ScaleX = 1;
                                }

                                isOwner = false;
                            }
                            else
                            {
                                Agent a = new Agent(character.Name);

                                a.Owner = agent;
                                a.Show();
                            }
                        }

                        Script.Instance.Enabled = true;
                        Script.Instance.Tick(DateTime.Today);
                        Script.Instance.Update(true);
                    }
                });
                storyboard.Children.Add(doubleAnimation1);
                storyboard.Children.Add(doubleAnimation2);

                Storyboard.SetTarget(doubleAnimation2, agent.balloon);
                Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(Window.OpacityProperty));
                Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Window.OpacityProperty));

                foreach (Window window in Application.Current.Windows)
                {
                    if (window != agent)
                    {
                        Agent a = window as Agent;

                        if (a != null)
                        {
                            DoubleAnimation da1 = new DoubleAnimation(a.Opacity, 0, TimeSpan.FromMilliseconds(500));
                            DoubleAnimation da2 = new DoubleAnimation(a.balloon.Opacity, 0, TimeSpan.FromMilliseconds(500));
                            SineEase se1 = new SineEase();
                            SineEase se2 = new SineEase();

                            se1.EasingMode = se2.EasingMode = EasingMode.EaseIn;

                            da1.EasingFunction = se1;
                            da2.EasingFunction = se2;

                            storyboard.Children.Add(da1);
                            storyboard.Children.Add(da2);

                            Storyboard.SetTarget(da1, a);
                            Storyboard.SetTarget(da2, a.balloon);
                            Storyboard.SetTargetProperty(da1, new PropertyPath(Window.OpacityProperty));
                            Storyboard.SetTargetProperty(da2, new PropertyPath(Window.OpacityProperty));
                        }
                        else if (window as Balloon == null && window.Owner != null)
                        {
                            DoubleAnimation da = new DoubleAnimation(window.Opacity, 0, TimeSpan.FromMilliseconds(500));
                            SineEase se = new SineEase();

                            se.EasingMode = EasingMode.EaseIn;

                            da.EasingFunction = se;

                            storyboard.Children.Add(da);

                            Storyboard.SetTarget(da, window);
                            Storyboard.SetTargetProperty(da, new PropertyPath(Window.OpacityProperty));
                        }
                    }
                }

                agent.fadeStoryboardDictionary.Add(storyboard, agent);
                agent.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
            {
                if (this == Application.Current.MainWindow)
                {
                    character.Location = new Character.Point<double>(this.Left - (this.Left - character.Location.X - character.BaseLocation.X) - character.BaseLocation.X, this.Top - (this.Top - character.Location.Y - character.BaseLocation.Y) - character.BaseLocation.Y);

                    this.balloon.Left = this.Left + (this.Width - this.balloon.Width) / 2;
                    this.balloon.Top = this.Top - this.balloon.Height + character.Origin.Y * this.ZoomScaleTransform.ScaleY;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (this != window)
                        {
                            Agent agent = window as Agent;

                            if (agent != null)
                            {
                                foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                                {
                                    c.Location = new Character.Point<double>(agent.Left - this.Left - c.BaseLocation.X, agent.Top - this.Top - c.BaseLocation.Y);
                                }
                            }

                            if (this == window.Owner && window.WindowStartupLocation == WindowStartupLocation.CenterOwner)
                            {
                                window.Left = this.Left + (this.Width - window.Width) / 2;
                                window.Top = this.Top + (this.Height - window.Height) / 2;
                            }
                        }
                    }
                }
                else
                {
                    Agent agent = Application.Current.MainWindow as Agent;

                    if (agent != null)
                    {
                        foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                        {
                            character.Location = new Character.Point<double>(this.Left - (agent.Left - c.Location.X - c.BaseLocation.X) - character.BaseLocation.X, this.Top - (agent.Top - c.Location.Y - c.BaseLocation.Y) - character.BaseLocation.Y);

                            this.balloon.Left = this.Left + (this.Width - this.balloon.Width) / 2;
                            this.balloon.Top = this.Top - this.balloon.Height + character.Origin.Y * this.ZoomScaleTransform.ScaleY;
                        }

                        foreach (Window window in Application.Current.Windows)
                        {
                            if (agent != window && this == window.Owner && window.WindowStartupLocation == WindowStartupLocation.CenterOwner)
                            {
                                window.Left = this.Left + (this.Width - window.Width) / 2;
                                window.Top = this.Top + (this.Height - window.Height) / 2;
                            }
                        }
                    }
                }
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                Agent agent = window as Agent;

                if (agent != null)
                {
                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select character)
                    {
                        agent.Width = character.Size.Width * agent.ZoomScaleTransform.ScaleX;
                        agent.Height = character.Size.Height * agent.ZoomScaleTransform.ScaleY;
                        agent.balloon.Left = agent.Left + (agent.Width - agent.balloon.Width) / 2;
                        agent.balloon.Top = agent.Top - agent.balloon.Height + character.Origin.Y * agent.ZoomScaleTransform.ScaleY;
                        agent.SizeToContent = SizeToContent.WidthAndHeight;
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Agent agent = Application.Current.MainWindow as Agent;

                if (agent != null)
                {
                    Nullable<double> scale = null;

                    if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.OemPlus))
                    {
                        scale = new Nullable<double>(agent.scale + 0.25);
                    }
                    else if (Keyboard.IsKeyDown(Key.Subtract) || Keyboard.IsKeyDown(Key.OemMinus))
                    {
                        double s = agent.scale - 0.25;

                        if (s > 0)
                        {
                            scale = new Nullable<double>(s);
                        }
                    }

                    if (scale.HasValue)
                    {
                        agent.scale = scale.Value;

                        foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select character)
                        {
                            Storyboard storyboard = new Storyboard();
                            DoubleAnimation doubleAnimation1 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleX, agent.scale, TimeSpan.FromMilliseconds(500));
                            DoubleAnimation doubleAnimation2 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleY, agent.scale, TimeSpan.FromMilliseconds(500));
                            DoubleAnimation doubleAnimation3 = new DoubleAnimation(agent.LayoutRoot.Width, character.Size.Width * agent.scale, TimeSpan.FromMilliseconds(500));
                            DoubleAnimation doubleAnimation4 = new DoubleAnimation(agent.LayoutRoot.Height, character.Size.Height * agent.scale, TimeSpan.FromMilliseconds(500));

                            if (agent.scaleStoryboard != null)
                            {
                                agent.scaleStoryboard.Stop(agent.LayoutRoot);
                            }

                            if (agent.ZoomScaleTransform.ScaleX < agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;
                                doubleAnimation1.EasingFunction = sineEase;
                            }
                            else if (agent.ZoomScaleTransform.ScaleX > agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseIn;
                                doubleAnimation1.EasingFunction = sineEase;
                            }

                            if (agent.ZoomScaleTransform.ScaleY < agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;
                                doubleAnimation2.EasingFunction = sineEase;
                            }
                            else if (agent.ZoomScaleTransform.ScaleY > agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseIn;
                                doubleAnimation2.EasingFunction = sineEase;
                            }

                            if (agent.LayoutRoot.Width < character.Size.Width * agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;
                                doubleAnimation3.EasingFunction = sineEase;
                            }
                            else if (agent.LayoutRoot.Width > character.Size.Width * agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseIn;
                                doubleAnimation3.EasingFunction = sineEase;
                            }

                            if (agent.LayoutRoot.Height < character.Size.Height * agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseOut;
                                doubleAnimation4.EasingFunction = sineEase;
                            }
                            else if (agent.LayoutRoot.Height > character.Size.Height * agent.scale)
                            {
                                SineEase sineEase = new SineEase();

                                sineEase.EasingMode = EasingMode.EaseIn;
                                doubleAnimation4.EasingFunction = sineEase;
                            }

                            storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                            {
                                if (((Clock)s).CurrentState == ClockState.Filling)
                                {
                                    agent.ZoomScaleTransform.ScaleX = agent.scale;
                                    agent.ZoomScaleTransform.ScaleY = agent.scale;

                                    foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                                    {
                                        agent.LayoutRoot.Width = c.Size.Width * agent.scale;
                                        agent.LayoutRoot.Height = c.Size.Height * agent.scale;
                                    }

                                    storyboard.Remove(agent.LayoutRoot);
                                    agent.scaleStoryboard = null;
                                }
                            });
                            storyboard.Children.Add(doubleAnimation1);
                            storyboard.Children.Add(doubleAnimation2);
                            storyboard.Children.Add(doubleAnimation3);
                            storyboard.Children.Add(doubleAnimation4);

                            Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                            Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                            Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(ContentControl.WidthProperty));
                            Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.HeightProperty));

                            agent.scaleStoryboard = storyboard;
                            agent.LayoutRoot.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        }

                        foreach (Window window in agent.OwnedWindows)
                        {
                            Agent a = window as Agent;

                            if (a != null)
                            {
                                a.scale = agent.scale;

                                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(a.characterName) select character)
                                {
                                    Storyboard storyboard = new Storyboard();
                                    DoubleAnimation doubleAnimation1 = new DoubleAnimation(a.ZoomScaleTransform.ScaleX, a.scale, TimeSpan.FromMilliseconds(500));
                                    DoubleAnimation doubleAnimation2 = new DoubleAnimation(a.ZoomScaleTransform.ScaleY, a.scale, TimeSpan.FromMilliseconds(500));
                                    DoubleAnimation doubleAnimation3 = new DoubleAnimation(a.LayoutRoot.Width, character.Size.Width * a.scale, TimeSpan.FromMilliseconds(500));
                                    DoubleAnimation doubleAnimation4 = new DoubleAnimation(a.LayoutRoot.Height, character.Size.Height * a.scale, TimeSpan.FromMilliseconds(500));

                                    if (a.scaleStoryboard != null)
                                    {
                                        a.scaleStoryboard.Stop(a.LayoutRoot);
                                    }

                                    if (a.ZoomScaleTransform.ScaleX < a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseOut;
                                        doubleAnimation1.EasingFunction = sineEase;
                                    }
                                    else if (a.ZoomScaleTransform.ScaleX > a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseIn;
                                        doubleAnimation1.EasingFunction = sineEase;
                                    }

                                    if (a.ZoomScaleTransform.ScaleY < a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseOut;
                                        doubleAnimation2.EasingFunction = sineEase;
                                    }
                                    else if (a.ZoomScaleTransform.ScaleY > a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseIn;
                                        doubleAnimation2.EasingFunction = sineEase;
                                    }

                                    if (a.LayoutRoot.Width < character.Size.Width * a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseOut;
                                        doubleAnimation3.EasingFunction = sineEase;
                                    }
                                    else if (a.LayoutRoot.Width > character.Size.Width * a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseIn;
                                        doubleAnimation3.EasingFunction = sineEase;
                                    }

                                    if (a.LayoutRoot.Height < character.Size.Height * a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseOut;
                                        doubleAnimation4.EasingFunction = sineEase;
                                    }
                                    else if (a.LayoutRoot.Height > character.Size.Height * a.scale)
                                    {
                                        SineEase sineEase = new SineEase();

                                        sineEase.EasingMode = EasingMode.EaseIn;
                                        doubleAnimation4.EasingFunction = sineEase;
                                    }

                                    storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                                    {
                                        if (((Clock)s).CurrentState == ClockState.Filling)
                                        {
                                            a.ZoomScaleTransform.ScaleX = a.scale;
                                            a.ZoomScaleTransform.ScaleY = a.scale;

                                            foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(a.characterName) select c)
                                            {
                                                a.LayoutRoot.Width = c.Size.Width * a.scale;
                                                a.LayoutRoot.Height = c.Size.Height * a.scale;
                                            }

                                            storyboard.Remove(a.LayoutRoot);
                                            a.scaleStoryboard = null;
                                        }
                                    });
                                    storyboard.Children.Add(doubleAnimation1);
                                    storyboard.Children.Add(doubleAnimation2);
                                    storyboard.Children.Add(doubleAnimation3);
                                    storyboard.Children.Add(doubleAnimation4);

                                    Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                                    Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                                    Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(ContentControl.WidthProperty));
                                    Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.HeightProperty));

                                    a.scaleStoryboard = storyboard;
                                    a.LayoutRoot.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                                }
                            }
                        }
                    }
                }
            }
            else if (e.Key == Key.F5)
            {
                Render();

                foreach (Window window in this.OwnedWindows)
                {
                    Agent agent = window as Agent;

                    if (agent != null)
                    {
                        agent.Render();
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if ((e.LeftButton != MouseButtonState.Pressed || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) && this.mouseDownPosition.HasValue)
            {
                this.mouseDownPosition = null;

                if (this.IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed && !SystemParameters.SwapButtons || e.RightButton == MouseButtonState.Pressed && SystemParameters.SwapButtons)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    this.mouseDownPosition = new Nullable<Point>(PointToScreen(e.GetPosition(this)));

                    CaptureMouse();
                }
                else
                {
                    // Begin dragging the window.
                    DragMove();
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.LeftButton == MouseButtonState.Released && !SystemParameters.SwapButtons || e.RightButton == MouseButtonState.Released && SystemParameters.SwapButtons)
            {
                if (this.mouseDownPosition.HasValue)
                {
                    Point point = PointToScreen(e.GetPosition(this));

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                    {
                        if (Math.Sign(point.X - this.mouseDownPosition.Value.X) > 0 && !character.Mirror || Math.Sign(point.X - this.mouseDownPosition.Value.X) < 0 && character.Mirror)
                        {
                            LinearGradientBrush linearGradientBrush = this.Canvas.OpacityMask as LinearGradientBrush;
                            Storyboard storyboard = new Storyboard();
                            ColorAnimation colorAnimation1 = new ColorAnimation();
                            ColorAnimation colorAnimation2 = new ColorAnimation();
                            SineEase sineEase1 = new SineEase();
                            SineEase sineEase2 = new SineEase();

                            if (linearGradientBrush == null)
                            {
                                GradientStopCollection gradientStopCollection = new GradientStopCollection();
                                GradientStop gradientStop1 = new GradientStop();
                                GradientStop gradientStop2 = new GradientStop();

                                colorAnimation1.From = gradientStop1.Color = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                                gradientStop1.Offset = 0;
                                colorAnimation2.From = gradientStop2.Color = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                                gradientStop2.Offset = 1;

                                gradientStopCollection.Add(gradientStop1);
                                gradientStopCollection.Add(gradientStop2);

                                this.Canvas.OpacityMask = linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0, 0.5), new Point(1, 0.5));
                            }
                            else
                            {
                                colorAnimation1.From = linearGradientBrush.GradientStops[1].Color;
                                colorAnimation2.From = linearGradientBrush.GradientStops[0].Color;
                            }

                            sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseOut;

                            colorAnimation1.To = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                            colorAnimation1.Duration = TimeSpan.FromMilliseconds(250);
                            colorAnimation1.EasingFunction = sineEase1;
                            colorAnimation2.To = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                            colorAnimation2.BeginTime = TimeSpan.FromMilliseconds(250);
                            colorAnimation2.Duration = TimeSpan.FromMilliseconds(250);
                            colorAnimation2.EasingFunction = sineEase2;

                            if (this.mirrorStoryboard != null)
                            {
                                this.mirrorStoryboard.Stop(this.Canvas);
                            }

                            storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                            {
                                if (((Clock)s).CurrentState == ClockState.Filling)
                                {
                                    this.Canvas.OpacityMask = null;
                                    storyboard.Remove(this.Canvas);
                                    this.mirrorStoryboard = null;
                                }
                            });
                            storyboard.Children.Add(colorAnimation1);
                            storyboard.Children.Add(colorAnimation2);

                            Storyboard.SetTargetProperty(colorAnimation1, new PropertyPath("(0).(1)[1].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                            Storyboard.SetTargetProperty(colorAnimation2, new PropertyPath("(0).(1)[0].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));

                            this.mirrorStoryboard = storyboard;
                            this.Canvas.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        }
                        else if (Math.Sign(point.X - this.mouseDownPosition.Value.X) > 0 && character.Mirror || Math.Sign(point.X - this.mouseDownPosition.Value.X) < 0 && !character.Mirror)
                        {
                            LinearGradientBrush linearGradientBrush = this.Canvas.OpacityMask as LinearGradientBrush;
                            Storyboard storyboard = new Storyboard();
                            ColorAnimation colorAnimation1 = new ColorAnimation();
                            ColorAnimation colorAnimation2 = new ColorAnimation();
                            ColorAnimation colorAnimation3 = new ColorAnimation(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(Byte.MaxValue, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                            ColorAnimation colorAnimation4 = new ColorAnimation(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(Byte.MaxValue, 0, 0, 0), TimeSpan.FromMilliseconds(250));
                            SineEase sineEase1 = new SineEase();
                            SineEase sineEase2 = new SineEase();
                            SineEase sineEase3 = new SineEase();
                            SineEase sineEase4 = new SineEase();

                            if (linearGradientBrush == null)
                            {
                                GradientStopCollection gradientStopCollection = new GradientStopCollection();
                                GradientStop gradientStop1 = new GradientStop();
                                GradientStop gradientStop2 = new GradientStop();

                                colorAnimation1.From = gradientStop1.Color = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                                gradientStop1.Offset = 0;
                                colorAnimation2.From = gradientStop2.Color = Color.FromArgb(Byte.MaxValue, 0, 0, 0);
                                gradientStop2.Offset = 1;

                                gradientStopCollection.Add(gradientStop1);
                                gradientStopCollection.Add(gradientStop2);

                                this.Canvas.OpacityMask = linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0, 0.5), new Point(1, 0.5));
                            }
                            else
                            {
                                colorAnimation1.From = linearGradientBrush.GradientStops[1].Color;
                                colorAnimation2.From = linearGradientBrush.GradientStops[0].Color;
                            }

                            sineEase1.EasingMode = sineEase2.EasingMode = EasingMode.EaseIn;
                            sineEase3.EasingMode = sineEase4.EasingMode = EasingMode.EaseOut;

                            colorAnimation1.To = Color.FromArgb(0, 0, 0, 0);
                            colorAnimation1.Duration = TimeSpan.FromMilliseconds(250);
                            colorAnimation1.EasingFunction = sineEase1;
                            colorAnimation2.To = Color.FromArgb(0, 0, 0, 0);
                            colorAnimation2.BeginTime = TimeSpan.FromMilliseconds(250);
                            colorAnimation2.Duration = TimeSpan.FromMilliseconds(250);
                            colorAnimation2.EasingFunction = sineEase2;
                            colorAnimation2.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                            {
                                if (((Clock)s).CurrentState == ClockState.Filling)
                                {
                                    foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(this.characterName) select c)
                                    {
                                        c.Mirror = !c.Mirror;

                                        if (c.Mirror)
                                        {
                                            this.FlipScaleTransform.ScaleX = -1;
                                        }
                                        else
                                        {
                                            this.FlipScaleTransform.ScaleX = 1;
                                        }
                                    }
                                }
                            });
                            colorAnimation3.BeginTime = TimeSpan.FromMilliseconds(500);
                            colorAnimation3.EasingFunction = sineEase3;
                            colorAnimation4.BeginTime = TimeSpan.FromMilliseconds(750);
                            colorAnimation4.EasingFunction = sineEase4;

                            if (this.mirrorStoryboard != null)
                            {
                                this.mirrorStoryboard.Stop(this.Canvas);
                            }

                            storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                            {
                                if (((Clock)s).CurrentState == ClockState.Filling)
                                {
                                    this.Canvas.OpacityMask = null;
                                    storyboard.Remove(this.Canvas);
                                    this.mirrorStoryboard = null;
                                }
                            });
                            storyboard.Children.Add(colorAnimation1);
                            storyboard.Children.Add(colorAnimation2);
                            storyboard.Children.Add(colorAnimation3);
                            storyboard.Children.Add(colorAnimation4);

                            Storyboard.SetTargetProperty(colorAnimation1, new PropertyPath("(0).(1)[1].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                            Storyboard.SetTargetProperty(colorAnimation2, new PropertyPath("(0).(1)[0].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                            Storyboard.SetTargetProperty(colorAnimation3, new PropertyPath("(0).(1)[0].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));
                            Storyboard.SetTargetProperty(colorAnimation4, new PropertyPath("(0).(1)[1].(2)", UIElement.OpacityMaskProperty, LinearGradientBrush.GradientStopsProperty, GradientStop.ColorProperty));

                            this.mirrorStoryboard = storyboard;
                            this.Canvas.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        }
                    }

                    this.mouseDownPosition = null;
                }

                if (this.IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Script.Instance.Activate();
            }
            else
            {
                var query = from image in this.Canvas.Children.Cast<Image>() let motion = image.Tag as Motion where motion != null && e.OriginalSource == image select motion;

                if (query.Any())
                {
                    var q = from sequence in Script.Instance.Sequences where sequence.Name.Equals("DoubleClick") && sequence.Owner.Equals(this.characterName) select sequence;

                    foreach (Motion motion in query)
                    {
                        Script.Instance.TryEnqueue(Script.Instance.Prepare(q, motion.Current.Path));
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                const int WHEEL_DATA = 120;
                int lines = e.Delta * SystemParameters.WheelScrollLines / WHEEL_DATA;
                Agent agent = Application.Current.MainWindow as Agent;

                if (agent != null)
                {
                    double scale = agent.scale + 0.25 * lines;

                    if (scale > 0)
                    {
                        agent.scale = scale;
                    }

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(agent.characterName) select character)
                    {
                        Storyboard storyboard = new Storyboard();
                        DoubleAnimation doubleAnimation1 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleX, agent.scale, TimeSpan.FromMilliseconds(500));
                        DoubleAnimation doubleAnimation2 = new DoubleAnimation(agent.ZoomScaleTransform.ScaleY, agent.scale, TimeSpan.FromMilliseconds(500));
                        DoubleAnimation doubleAnimation3 = new DoubleAnimation(agent.LayoutRoot.Width, character.Size.Width * agent.scale, TimeSpan.FromMilliseconds(500));
                        DoubleAnimation doubleAnimation4 = new DoubleAnimation(agent.LayoutRoot.Height, character.Size.Height * agent.scale, TimeSpan.FromMilliseconds(500));

                        if (agent.scaleStoryboard != null)
                        {
                            agent.scaleStoryboard.Stop(agent.LayoutRoot);
                        }

                        if (agent.ZoomScaleTransform.ScaleX < agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseOut;
                            doubleAnimation1.EasingFunction = sineEase;
                        }
                        else if (agent.ZoomScaleTransform.ScaleX > agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseIn;
                            doubleAnimation1.EasingFunction = sineEase;
                        }

                        if (agent.ZoomScaleTransform.ScaleY < agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseOut;
                            doubleAnimation2.EasingFunction = sineEase;
                        }
                        else if (agent.ZoomScaleTransform.ScaleY > agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseIn;
                            doubleAnimation2.EasingFunction = sineEase;
                        }

                        if (agent.LayoutRoot.Width < character.Size.Width * agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseOut;
                            doubleAnimation3.EasingFunction = sineEase;
                        }
                        else if (agent.LayoutRoot.Width > character.Size.Width * agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseIn;
                            doubleAnimation3.EasingFunction = sineEase;
                        }

                        if (agent.LayoutRoot.Height < character.Size.Height * agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseOut;
                            doubleAnimation4.EasingFunction = sineEase;
                        }
                        else if (agent.LayoutRoot.Height > character.Size.Height * agent.scale)
                        {
                            SineEase sineEase = new SineEase();

                            sineEase.EasingMode = EasingMode.EaseIn;
                            doubleAnimation4.EasingFunction = sineEase;
                        }

                        storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                        {
                            if (((Clock)s).CurrentState == ClockState.Filling)
                            {
                                agent.ZoomScaleTransform.ScaleX = agent.scale;
                                agent.ZoomScaleTransform.ScaleY = agent.scale;

                                foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                                {
                                    agent.LayoutRoot.Width = c.Size.Width * agent.scale;
                                    agent.LayoutRoot.Height = c.Size.Height * agent.scale;
                                }

                                storyboard.Remove(agent.LayoutRoot);
                                agent.scaleStoryboard = null;
                            }
                        });
                        storyboard.Children.Add(doubleAnimation1);
                        storyboard.Children.Add(doubleAnimation2);
                        storyboard.Children.Add(doubleAnimation3);
                        storyboard.Children.Add(doubleAnimation4);

                        Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                        Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                        Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(ContentControl.WidthProperty));
                        Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.HeightProperty));

                        agent.scaleStoryboard = storyboard;
                        agent.LayoutRoot.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                    }

                    foreach (Window window in agent.OwnedWindows)
                    {
                        Agent a = window as Agent;

                        if (a != null)
                        {
                            a.scale = agent.scale;

                            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(a.characterName) select character)
                            {
                                Storyboard storyboard = new Storyboard();
                                DoubleAnimation doubleAnimation1 = new DoubleAnimation(a.ZoomScaleTransform.ScaleX, a.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation2 = new DoubleAnimation(a.ZoomScaleTransform.ScaleY, a.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation3 = new DoubleAnimation(a.LayoutRoot.Width, character.Size.Width * a.scale, TimeSpan.FromMilliseconds(500));
                                DoubleAnimation doubleAnimation4 = new DoubleAnimation(a.LayoutRoot.Height, character.Size.Height * a.scale, TimeSpan.FromMilliseconds(500));

                                if (a.scaleStoryboard != null)
                                {
                                    a.scaleStoryboard.Stop(a.LayoutRoot);
                                }

                                if (a.ZoomScaleTransform.ScaleX < a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation1.EasingFunction = sineEase;
                                }
                                else if (a.ZoomScaleTransform.ScaleX > a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation1.EasingFunction = sineEase;
                                }

                                if (a.ZoomScaleTransform.ScaleY < a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation2.EasingFunction = sineEase;
                                }
                                else if (a.ZoomScaleTransform.ScaleY > a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation2.EasingFunction = sineEase;
                                }

                                if (a.LayoutRoot.Width < character.Size.Width * a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation3.EasingFunction = sineEase;
                                }
                                else if (a.LayoutRoot.Width > character.Size.Width * a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation3.EasingFunction = sineEase;
                                }

                                if (a.LayoutRoot.Height < character.Size.Height * a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseOut;
                                    doubleAnimation4.EasingFunction = sineEase;
                                }
                                else if (a.LayoutRoot.Height > character.Size.Height * a.scale)
                                {
                                    SineEase sineEase = new SineEase();

                                    sineEase.EasingMode = EasingMode.EaseIn;
                                    doubleAnimation4.EasingFunction = sineEase;
                                }

                                storyboard.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs ea)
                                {
                                    if (((Clock)s).CurrentState == ClockState.Filling)
                                    {
                                        a.ZoomScaleTransform.ScaleX = a.scale;
                                        a.ZoomScaleTransform.ScaleY = a.scale;

                                        foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(a.characterName) select c)
                                        {
                                            a.LayoutRoot.Width = c.Size.Width * a.scale;
                                            a.LayoutRoot.Height = c.Size.Height * a.scale;
                                        }

                                        storyboard.Remove(a.LayoutRoot);
                                        a.scaleStoryboard = null;
                                    }
                                });
                                storyboard.Children.Add(doubleAnimation1);
                                storyboard.Children.Add(doubleAnimation2);
                                storyboard.Children.Add(doubleAnimation3);
                                storyboard.Children.Add(doubleAnimation4);

                                Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                                Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1).(2)", ContentControl.ContentProperty, Canvas.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                                Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath(ContentControl.WidthProperty));
                                Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.HeightProperty));

                                a.scaleStoryboard = storyboard;
                                a.LayoutRoot.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] data = e.Data.GetData(DataFormats.FileDrop, false) as string[];

                if (data != null)
                {
                    List<Character> characterList = new List<Character>();

                    foreach (string s in data)
                    {
                        if (Directory.Exists(s))
                        {
                            foreach (List<Tuple<string, string>> tupleList in (from filename in Directory.EnumerateFiles(s, "*.xml", SearchOption.AllDirectories) let attributes = File.GetAttributes(filename) where (attributes & FileAttributes.Hidden) != FileAttributes.Hidden select filename).Aggregate<string, Dictionary<string, List<Tuple<string, string>>>>(new Dictionary<string, List<Tuple<string, string>>>(), delegate (Dictionary<string, List<Tuple<string, string>>> dictionary, string filename1)
                            {
                                string filename2 = Path.GetFileNameWithoutExtension(filename1);
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(filename2, "^(.+?)\\.([a-z]{2,3})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                                string key;
                                List<Tuple<string, string>> tupleList;

                                if (match.Success)
                                {
                                    key = String.Concat(Path.GetDirectoryName(filename1), match.Groups[1].Value);

                                    if (dictionary.TryGetValue(key, out tupleList))
                                    {
                                        tupleList.Add(Tuple.Create<string, string>(filename1, match.Groups[2].Value));
                                    }
                                    else
                                    {
                                        tupleList = new List<Tuple<string, string>>();
                                        tupleList.Add(Tuple.Create<string, string>(filename1, match.Groups[2].Value));
                                        dictionary.Add(key, tupleList);
                                    }
                                }
                                else
                                {
                                    key = String.Concat(Path.GetDirectoryName(filename1), filename2);

                                    if (dictionary.TryGetValue(key, out tupleList))
                                    {
                                        tupleList.Add(Tuple.Create<string, string>(filename1, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                    }
                                    else
                                    {
                                        tupleList = new List<Tuple<string, string>>();
                                        tupleList.Add(Tuple.Create<string, string>(filename1, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                        dictionary.Add(key, tupleList);
                                    }
                                }

                                return dictionary;
                            }).Values)
                            {
                                Tuple<string, string> tuple = tupleList.Find(delegate (Tuple<string, string> t)
                                {
                                    return t.Item2.Equals(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                                });

                                if (tuple == null)
                                {
                                    tuple = tupleList.Find(delegate (Tuple<string, string> t)
                                    {
                                        return t.Item2.Equals(System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName);
                                    });

                                    if (tuple != null)
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tuple.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                            if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                            {
                                                stringBuilder.Append(Path.DirectorySeparatorChar);
                                            }

                                            string path = tuple.Item1.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tuple.Item1.Remove(0, stringBuilder.Length) : tuple.Item1;

                                            foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                            {
                                                Character character = new Character();

                                                character.Name = attribute;
                                                character.Script = path;

                                                characterList.Add(character);
                                            }
                                        }
                                        catch
                                        {
                                            return;
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(tuple.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                        if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                        {
                                            stringBuilder.Append(Path.DirectorySeparatorChar);
                                        }

                                        string path = tuple.Item1.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tuple.Item1.Remove(0, stringBuilder.Length) : tuple.Item1;

                                        foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                        {
                                            Character character = new Character();

                                            character.Name = attribute;
                                            character.Script = path;

                                            characterList.Add(character);
                                        }
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string extension = Path.GetExtension(s);

                            if (extension.Equals(".opml", StringComparison.OrdinalIgnoreCase))
                            {
                                Task.Factory.StartNew<List<Source>>(delegate (object state)
                                {
                                    FileStream fs = null;
                                    List<Source> sourceList = new List<Source>();

                                    try
                                    {
                                        fs = new FileStream((string)state, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        foreach (XElement element in (System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/opml/body//outline[@xmlUrl]"))
                                        {
                                            string title = null;
                                            Uri xmlUrl = null;

                                            foreach (XAttribute attribute in element.Attributes())
                                            {
                                                if (attribute.Name.LocalName.Equals("title"))
                                                {
                                                    title = attribute.Value;
                                                }
                                                else if (attribute.Name.LocalName.Equals("xmlUrl"))
                                                {

                                                    xmlUrl = new Uri(attribute.Value, UriKind.Absolute);
                                                }
                                            }

                                            sourceList.Add(new Source(title, xmlUrl));
                                        }
                                    }
                                    catch
                                    {
                                        return null;
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }

                                    return sourceList;
                                }, s).ContinueWith(delegate (Task<List<Source>> task)
                                {
                                    if (task.Result != null)
                                    {
                                        task.Result.ForEach(delegate (Source source)
                                        {
                                            Script.Instance.Sources.Add(source);
                                        });
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                            else if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                    if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                    {
                                        stringBuilder.Append(Path.DirectorySeparatorChar);
                                    }

                                    string path = s.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? s.Remove(0, stringBuilder.Length) : s;

                                    foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                    {
                                        Character character = new Character();

                                        character.Name = attribute;
                                        character.Script = path;

                                        characterList.Add(character);
                                    }
                                }
                                catch
                                {
                                    return;
                                }
                                finally
                                {
                                    if (fs != null)
                                    {
                                        fs.Close();
                                    }
                                }
                            }
                            else if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    using (ZipArchive zipArchive = new ZipArchive(fs))
                                    {
                                        fs = null;

                                        StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                        if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                        {
                                            stringBuilder.Append(Path.DirectorySeparatorChar);
                                        }

                                        string path = s.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? s.Remove(0, stringBuilder.Length) : s;

                                        foreach (List<Tuple<ZipArchiveEntry, string>> tupleList in (from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry).Aggregate<ZipArchiveEntry, Dictionary<string, List<Tuple<ZipArchiveEntry, string>>>>(new Dictionary<string, List<Tuple<ZipArchiveEntry, string>>>(), delegate (Dictionary<string, List<Tuple<ZipArchiveEntry, string>>> dictionary, ZipArchiveEntry zipArchiveEntry)
                                        {
                                            string filename = Path.GetFileNameWithoutExtension(zipArchiveEntry.FullName);
                                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(filename, "^(.+?)\\.([a-z]{2,3})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                                            string key;
                                            List<Tuple<ZipArchiveEntry, string>> tupleList;

                                            if (match.Success)
                                            {
                                                key = String.Concat(Path.GetDirectoryName(zipArchiveEntry.FullName), match.Groups[1].Value);

                                                if (dictionary.TryGetValue(key, out tupleList))
                                                {
                                                    tupleList.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, match.Groups[2].Value));
                                                }
                                                else
                                                {
                                                    tupleList = new List<Tuple<ZipArchiveEntry, string>>();
                                                    tupleList.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, match.Groups[2].Value));
                                                    dictionary.Add(key, tupleList);
                                                }
                                            }
                                            else
                                            {
                                                key = String.Concat(Path.GetDirectoryName(zipArchiveEntry.FullName), filename);

                                                if (dictionary.TryGetValue(key, out tupleList))
                                                {
                                                    tupleList.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                                }
                                                else
                                                {
                                                    tupleList = new List<Tuple<ZipArchiveEntry, string>>();
                                                    tupleList.Add(Tuple.Create<ZipArchiveEntry, string>(zipArchiveEntry, System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName));
                                                    dictionary.Add(key, tupleList);
                                                }
                                            }

                                            return dictionary;
                                        }).Values)
                                        {
                                            Tuple<ZipArchiveEntry, string> tuple = tupleList.Find(delegate (Tuple<ZipArchiveEntry, string> t)
                                            {
                                                return t.Item2.Equals(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                                            });

                                            if (tuple == null)
                                            {
                                                tuple = tupleList.Find(delegate (Tuple<ZipArchiveEntry, string> t)
                                                {
                                                    return t.Item2.Equals(System.Globalization.CultureInfo.InvariantCulture.TwoLetterISOLanguageName);
                                                });

                                                if (tuple != null)
                                                {
                                                    Stream stream = null;

                                                    try
                                                    {
                                                        stream = tuple.Item1.Open();

                                                        foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                        {
                                                            Character character = new Character();

                                                            character.Name = attribute;
                                                            character.Script = path;

                                                            characterList.Add(character);
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        return;
                                                    }
                                                    finally
                                                    {
                                                        if (stream != null)
                                                        {
                                                            stream.Close();
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Stream stream = null;

                                                try
                                                {
                                                    stream = tuple.Item1.Open();

                                                    foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                    {
                                                        Character character = new Character();

                                                        character.Name = attribute;
                                                        character.Script = path;

                                                        characterList.Add(character);
                                                    }
                                                }
                                                catch
                                                {
                                                    return;
                                                }
                                                finally
                                                {
                                                    if (stream != null)
                                                    {
                                                        stream.Close();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    if (fs != null)
                                    {
                                        fs.Close();
                                    }
                                }
                            }
                        }
                    }

                    if (characterList.Count > 0)
                    {
                        Switch(characterList);
                    }
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.Owner != null)
            {
                e.Cancel = true;
            }

            if (!e.Cancel && this == Application.Current.MainWindow)
            {
                int versionMajor = Environment.OSVersion.Version.Major;
                int versionMinor = Environment.OSVersion.Version.Minor;
                double version = versionMajor + (double)versionMinor / 10;
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

                if (version > 6.1)
                {
                    const long APPMODEL_ERROR_NO_PACKAGE = 15700L;
                    int length = 0;
                    StringBuilder sb = new StringBuilder(0);
                    int result = NativeMethods.GetCurrentPackageFullName(ref length, sb);

                    sb = new StringBuilder(length);
                    result = NativeMethods.GetCurrentPackageFullName(ref length, sb);

                    if (result == APPMODEL_ERROR_NO_PACKAGE)
                    {
                        System.Configuration.Configuration config = null;

                        if (Directory.Exists(directory))
                        {
                            string filename = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                            foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly) where filename.Equals(Path.GetFileNameWithoutExtension(s)) select s)
                            {
                                System.Configuration.ExeConfigurationFileMap exeConfigurationFileMap = new System.Configuration.ExeConfigurationFileMap();

                                exeConfigurationFileMap.ExeConfigFilename = s;
                                config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);
                            }
                        }

                        if (config == null)
                        {
                            config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                        }

                        foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                        {
                            if (config.AppSettings.Settings["Left"] == null)
                            {
                                config.AppSettings.Settings.Add("Left", (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                config.AppSettings.Settings["Left"].Value = (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }

                            if (config.AppSettings.Settings["Top"] == null)
                            {
                                config.AppSettings.Settings.Add("Top", (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                config.AppSettings.Settings["Top"].Value = (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }

                        if (config.AppSettings.Settings["Opacity"] == null)
                        {
                            config.AppSettings.Settings.Add("Opacity", this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            config.AppSettings.Settings["Opacity"].Value = this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (config.AppSettings.Settings["Scale"] == null)
                        {
                            config.AppSettings.Settings.Add("Scale", this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            config.AppSettings.Settings["Scale"].Value = this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (config.AppSettings.Settings["Topmost"] == null)
                        {
                            config.AppSettings.Settings.Add("Topmost", this.Topmost.ToString());
                        }
                        else
                        {
                            config.AppSettings.Settings["Topmost"].Value = this.Topmost.ToString();
                        }

                        if (config.AppSettings.Settings["ShowInTaskbar"] == null)
                        {
                            config.AppSettings.Settings.Add("ShowInTaskbar", this.ShowInTaskbar.ToString());
                        }
                        else
                        {
                            config.AppSettings.Settings["ShowInTaskbar"].Value = this.ShowInTaskbar.ToString();
                        }

                        if (config.AppSettings.Settings["Mute"] == null)
                        {
                            config.AppSettings.Settings.Add("Mute", this.isMute.ToString());
                        }
                        else
                        {
                            config.AppSettings.Settings["Mute"].Value = this.isMute.ToString();
                        }

                        config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    }
                    else
                    {
                        string filename = String.Concat(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location), ".config");
                        string path = Path.Combine(directory, filename);
                        System.Configuration.ExeConfigurationFileMap exeConfigurationFileMap = new System.Configuration.ExeConfigurationFileMap();

                        if (Directory.Exists(directory))
                        {
                            if (File.Exists(path))
                            {
                                exeConfigurationFileMap.ExeConfigFilename = path;

                                System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);

                                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                                {
                                    if (config.AppSettings.Settings["Left"] == null)
                                    {
                                        config.AppSettings.Settings.Add("Left", (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        config.AppSettings.Settings["Left"].Value = (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    }

                                    if (config.AppSettings.Settings["Top"] == null)
                                    {
                                        config.AppSettings.Settings.Add("Top", (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        config.AppSettings.Settings["Top"].Value = (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                }

                                if (config.AppSettings.Settings["Opacity"] == null)
                                {
                                    config.AppSettings.Settings.Add("Opacity", this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Opacity"].Value = this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (config.AppSettings.Settings["Scale"] == null)
                                {
                                    config.AppSettings.Settings.Add("Scale", this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Scale"].Value = this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (config.AppSettings.Settings["Topmost"] == null)
                                {
                                    config.AppSettings.Settings.Add("Topmost", this.Topmost.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["Topmost"].Value = this.Topmost.ToString();
                                }

                                if (config.AppSettings.Settings["ShowInTaskbar"] == null)
                                {
                                    config.AppSettings.Settings.Add("ShowInTaskbar", this.ShowInTaskbar.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["ShowInTaskbar"].Value = this.ShowInTaskbar.ToString();
                                }

                                if (config.AppSettings.Settings["Mute"] == null)
                                {
                                    config.AppSettings.Settings.Add("Mute", this.isMute.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["Mute"].Value = this.isMute.ToString();
                                }

                                config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                            }
                            else
                            {
                                exeConfigurationFileMap.ExeConfigFilename = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;

                                System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);

                                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                                {
                                    if (config.AppSettings.Settings["Left"] == null)
                                    {
                                        config.AppSettings.Settings.Add("Left", (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        config.AppSettings.Settings["Left"].Value = (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    }

                                    if (config.AppSettings.Settings["Top"] == null)
                                    {
                                        config.AppSettings.Settings.Add("Top", (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        config.AppSettings.Settings["Top"].Value = (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                }

                                if (config.AppSettings.Settings["Opacity"] == null)
                                {
                                    config.AppSettings.Settings.Add("Opacity", this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Opacity"].Value = this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (config.AppSettings.Settings["Scale"] == null)
                                {
                                    config.AppSettings.Settings.Add("Scale", this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Scale"].Value = this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (config.AppSettings.Settings["Topmost"] == null)
                                {
                                    config.AppSettings.Settings.Add("Topmost", this.Topmost.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["Topmost"].Value = this.Topmost.ToString();
                                }

                                if (config.AppSettings.Settings["ShowInTaskbar"] == null)
                                {
                                    config.AppSettings.Settings.Add("ShowInTaskbar", this.ShowInTaskbar.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["ShowInTaskbar"].Value = this.ShowInTaskbar.ToString();
                                }

                                if (config.AppSettings.Settings["Mute"] == null)
                                {
                                    config.AppSettings.Settings.Add("Mute", this.isMute.ToString());
                                }
                                else
                                {
                                    config.AppSettings.Settings["Mute"].Value = this.isMute.ToString();
                                }

                                foreach (System.Configuration.ConfigurationSection section in (from section in config.Sections.Cast<System.Configuration.ConfigurationSection>() where !config.AppSettings.SectionInformation.Name.Equals(section.SectionInformation.Name) select section).ToArray())
                                {
                                    section.SectionInformation.RevertToParent();
                                }

                                config.SaveAs(path, System.Configuration.ConfigurationSaveMode.Modified);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(directory);

                            exeConfigurationFileMap.ExeConfigFilename = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;

                            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);

                            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                            {
                                if (config.AppSettings.Settings["Left"] == null)
                                {
                                    config.AppSettings.Settings.Add("Left", (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Left"].Value = (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                if (config.AppSettings.Settings["Top"] == null)
                                {
                                    config.AppSettings.Settings.Add("Top", (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    config.AppSettings.Settings["Top"].Value = (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }
                            }

                            if (config.AppSettings.Settings["Opacity"] == null)
                            {
                                config.AppSettings.Settings.Add("Opacity", this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                config.AppSettings.Settings["Opacity"].Value = this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }

                            if (config.AppSettings.Settings["Scale"] == null)
                            {
                                config.AppSettings.Settings.Add("Scale", this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                config.AppSettings.Settings["Scale"].Value = this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }

                            if (config.AppSettings.Settings["Topmost"] == null)
                            {
                                config.AppSettings.Settings.Add("Topmost", this.Topmost.ToString());
                            }
                            else
                            {
                                config.AppSettings.Settings["Topmost"].Value = this.Topmost.ToString();
                            }

                            if (config.AppSettings.Settings["ShowInTaskbar"] == null)
                            {
                                config.AppSettings.Settings.Add("ShowInTaskbar", this.ShowInTaskbar.ToString());
                            }
                            else
                            {
                                config.AppSettings.Settings["ShowInTaskbar"].Value = this.ShowInTaskbar.ToString();
                            }

                            if (config.AppSettings.Settings["Mute"] == null)
                            {
                                config.AppSettings.Settings.Add("Mute", this.isMute.ToString());
                            }
                            else
                            {
                                config.AppSettings.Settings["Mute"].Value = this.isMute.ToString();
                            }

                            foreach (System.Configuration.ConfigurationSection section in (from section in config.Sections.Cast<System.Configuration.ConfigurationSection>() where !config.AppSettings.SectionInformation.Name.Equals(section.SectionInformation.Name) select section).ToArray())
                            {
                                section.SectionInformation.RevertToParent();
                            }

                            config.SaveAs(path, System.Configuration.ConfigurationSaveMode.Modified);
                        }
                    }
                }
                else
                {
                    System.Configuration.Configuration config = null;

                    if (Directory.Exists(directory))
                    {
                        string filename = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                        foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly) where filename.Equals(Path.GetFileNameWithoutExtension(s)) select s)
                        {
                            System.Configuration.ExeConfigurationFileMap exeConfigurationFileMap = new System.Configuration.ExeConfigurationFileMap();

                            exeConfigurationFileMap.ExeConfigFilename = s;
                            config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, System.Configuration.ConfigurationUserLevel.None);
                        }
                    }

                    if (config == null)
                    {
                        config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                    }

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                    {
                        if (config.AppSettings.Settings["Left"] == null)
                        {
                            config.AppSettings.Settings.Add("Left", (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            config.AppSettings.Settings["Left"].Value = (this.Left - character.Location.X - character.BaseLocation.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (config.AppSettings.Settings["Top"] == null)
                        {
                            config.AppSettings.Settings.Add("Top", (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            config.AppSettings.Settings["Top"].Value = (this.Top - character.Location.Y - character.BaseLocation.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }

                    if (config.AppSettings.Settings["Opacity"] == null)
                    {
                        config.AppSettings.Settings.Add("Opacity", this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        config.AppSettings.Settings["Opacity"].Value = this.opacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config.AppSettings.Settings["Scale"] == null)
                    {
                        config.AppSettings.Settings.Add("Scale", this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        config.AppSettings.Settings["Scale"].Value = this.scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (config.AppSettings.Settings["Topmost"] == null)
                    {
                        config.AppSettings.Settings.Add("Topmost", this.Topmost.ToString());
                    }
                    else
                    {
                        config.AppSettings.Settings["Topmost"].Value = this.Topmost.ToString();
                    }

                    if (config.AppSettings.Settings["ShowInTaskbar"] == null)
                    {
                        config.AppSettings.Settings.Add("ShowInTaskbar", this.ShowInTaskbar.ToString());
                    }
                    else
                    {
                        config.AppSettings.Settings["ShowInTaskbar"].Value = this.ShowInTaskbar.ToString();
                    }

                    if (config.AppSettings.Settings["Mute"] == null)
                    {
                        config.AppSettings.Settings.Add("Mute", this.isMute.ToString());
                    }
                    else
                    {
                        config.AppSettings.Settings["Mute"].Value = this.isMute.ToString();
                    }

                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                }
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged -= new Microsoft.Win32.PowerModeChangedEventHandler(this.OnPowerModeChanged);

            base.OnClosed(e);
        }

        protected virtual void OnDrawClipboard(EventArgs e)
        {
            const uint CF_UNICODETEXT = 13;

            if (this == Application.Current.MainWindow && this.IsVisible && Script.Instance.Enabled && NativeMethods.IsClipboardFormatAvailable(CF_UNICODETEXT) && NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                IntPtr handle = NativeMethods.GetClipboardData(CF_UNICODETEXT);

                if (handle != IntPtr.Zero)
                {
                    IntPtr lpwstr = NativeMethods.GlobalLock(handle);

                    if (lpwstr != IntPtr.Zero)
                    {
                        const int maxLength = 100;
                        string text = System.Runtime.InteropServices.Marshal.PtrToStringUni(lpwstr).Trim();

                        if (text.Length > maxLength)
                        {
                            text = text.Remove(maxLength, text.Length - maxLength);
                        }

                        if (text.Length > 0)
                        {
                            Script.Instance.TryEnqueue(Script.Instance.Prepare(from sequence in Script.Instance.Sequences where sequence.Name.Equals("DrawClipboard") select sequence, text).Aggregate<Sequence, List<Sequence>>(new List<Sequence>(), (preparedSequenceList, sequence) =>
                            {
                                System.Collections.ArrayList arrayList = new System.Collections.ArrayList();

                                foreach (object o in sequence)
                                {
                                    Message message = o as Message;

                                    if (message == null)
                                    {
                                        arrayList.Add(o);
                                    }
                                    else
                                    {
                                        int index = 0;
                                        StringBuilder stringBuilder = new StringBuilder();
                                        Message newMessage = new Message();

                                        for (System.Text.RegularExpressions.Match m1 = System.Text.RegularExpressions.Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", System.Text.RegularExpressions.RegexOptions.CultureInvariant); m1.Success; m1 = m1.NextMatch())
                                        {
                                            string pattern;

                                            if (m1.Index > index)
                                            {
                                                stringBuilder.Append(System.Text.RegularExpressions.Regex.Replace(message.Text.Substring(index, m1.Index - index), @"\{\{|}}", new System.Text.RegularExpressions.MatchEvaluator(delegate (System.Text.RegularExpressions.Match m2)
                                                {
                                                    return m2.Value.Substring(m2.Length / 2);
                                                }), System.Text.RegularExpressions.RegexOptions.CultureInvariant));
                                            }

                                            if (m1.Groups[1].Success)
                                            {
                                                stringBuilder.Append(m1.Groups[1].Value.Substring(m1.Groups[1].Length / 2));
                                            }

                                            if (m1.Groups[3].Success)
                                            {
                                                int i = m1.Groups[2].Index;
                                                StringBuilder sb = new StringBuilder();

                                                foreach (System.Text.RegularExpressions.Capture capture in m1.Groups[3].Captures)
                                                {
                                                    if (capture.Index > i)
                                                    {
                                                        sb.Append(message.Text.Substring(i, capture.Index - i));
                                                    }

                                                    sb.Append(capture.Value.Substring(capture.Length / 2));
                                                    i = capture.Index + capture.Length;
                                                }

                                                if (m1.Groups[2].Index + m1.Groups[2].Length > i)
                                                {
                                                    sb.Append(message.Text.Substring(i, m1.Groups[2].Index + m1.Groups[2].Length - i));
                                                }

                                                pattern = sb.ToString();
                                            }
                                            else
                                            {
                                                pattern = m1.Groups[2].Value;
                                            }

                                            if (System.Text.RegularExpressions.Regex.IsMatch(text, pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Singleline))
                                            {
                                                Entry newEntry = new Entry();
                                                System.Text.RegularExpressions.Match m3 = System.Text.RegularExpressions.Regex.Match(text, @"(\S+)://([^:/]+)(:(\d+))?(/[^#\s]*)(#(\S+))?", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                                newEntry.Title = text;

                                                if (m3.Success)
                                                {
                                                    Uri uri;

                                                    if (Uri.TryCreate(m3.Value, UriKind.RelativeOrAbsolute, out uri))
                                                    {
                                                        newEntry.Resource = uri;
                                                    }
                                                }

                                                if (stringBuilder.Length > 0)
                                                {
                                                    newMessage.Add(stringBuilder.ToString());
                                                    stringBuilder.Clear();
                                                }

                                                newMessage.Add(newEntry);
                                            }
                                            else
                                            {
                                                return preparedSequenceList;
                                            }

                                            if (m1.Groups[4].Success)
                                            {
                                                stringBuilder.Append(m1.Groups[4].Value.Substring(m1.Groups[4].Length / 2));
                                            }

                                            index = m1.Index + m1.Length;
                                        }

                                        if (message.Text.Length > index)
                                        {
                                            stringBuilder.Append(System.Text.RegularExpressions.Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new System.Text.RegularExpressions.MatchEvaluator(delegate (System.Text.RegularExpressions.Match m2)
                                            {
                                                return m2.Value.Substring(m2.Length / 2);
                                            }), System.Text.RegularExpressions.RegexOptions.CultureInvariant));
                                            newMessage.Add(stringBuilder.ToString());
                                        }
                                        else if (stringBuilder.Length > 0)
                                        {
                                            newMessage.Add(stringBuilder.ToString());
                                        }

                                        newMessage.Speed = message.Speed;
                                        newMessage.Duration = message.Duration;

                                        arrayList.Add(newMessage);
                                    }
                                }

                                sequence.Clear();

                                foreach (object o in arrayList)
                                {
                                    sequence.Add(o);
                                }

                                preparedSequenceList.Add(sequence);

                                return preparedSequenceList;
                            }));
                        }

                        NativeMethods.GlobalUnlock(handle);
                    }
                }

                NativeMethods.CloseClipboard();
            }

            if (this.DrawClipboard != null)
            {
                this.DrawClipboard(this, e);
            }
        }

        private void OnPowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            if (this == Application.Current.MainWindow && Script.Instance.Enabled)
            {
                if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
                {
                    Script.Instance.Suspend();
                }
                else if (e.Mode == Microsoft.Win32.PowerModes.Resume)
                {
                    Script.Instance.Resume();
                }
            }
        }

        private void Render()
        {
            foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
            {
                Queue<Image> imageQueue = new Queue<Image>(this.Canvas.Children.Cast<Image>());
                int index = 0;
                bool updateLayoutRequired = false;

                this.cachedMotionList.ForEach(delegate (Motion motion)
                {
                    Image image = null;
                    Queue<Image> queue = new Queue<Image>();

                    while (imageQueue.Count > 0)
                    {
                        Image i = imageQueue.Dequeue();
                        Motion m = i.Tag as Motion;

                        if (m != null && motion.ZIndex == m.ZIndex && String.Equals(motion.Type, m.Type))
                        {
                            image = i;

                            continue;
                        }

                        queue.Enqueue(i);
                    }

                    imageQueue = queue;

                    if (image == null)
                    {
                        if (motion.Current.Opacity > 0)
                        {
                            List<string> typeList = null;
                            bool isVisible;
                            BitmapImage bitmapImage;

                            if (motion.Type == null)
                            {
                                typeList = new List<string>();
                                this.cachedMotionList.ForEach(delegate (Motion m)
                                {
                                    if (m.ZIndex == motion.ZIndex)
                                    {
                                        typeList.Add(m.Type);
                                    }
                                });
                            }

                            if (typeList == null)
                            {
                                if (character.HasTypes)
                                {
                                    typeList = new List<string>();
                                    this.cachedMotionList.ForEach(delegate (Motion m)
                                    {
                                        if (m.ZIndex == motion.ZIndex && character.Types.Contains(m.Type))
                                        {
                                            typeList.Add(m.Type);
                                        }
                                    });
                                    isVisible = typeList.Count > 0 && typeList.LastIndexOf(motion.Type) == typeList.Count - 1;
                                }
                                else
                                {
                                    isVisible = false;
                                }
                            }
                            else if (character.HasTypes)
                            {
                                isVisible = !typeList.Exists(delegate (string type)
                                {
                                    return character.Types.Contains(type);
                                });
                            }
                            else
                            {
                                isVisible = true;
                            }

                            image = new Image();
                            image.CacheMode = new BitmapCache(1);
                            image.VerticalAlignment = VerticalAlignment.Top;
                            image.HorizontalAlignment = HorizontalAlignment.Left;

                            if (this.cachedBitmapImageDictionary.TryGetValue(Path.GetExtension(character.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Path.GetDirectoryName(character.Script), Path.GetFileNameWithoutExtension(character.Script), motion.Current.Path) : Path.Combine(Path.GetDirectoryName(character.Script), motion.Current.Path), out bitmapImage))
                            {
                                image.Source = bitmapImage;

                                if (motion.Current.Size.IsEmpty)
                                {
                                    image.Stretch = Stretch.None;
                                    image.Width = bitmapImage.Width;
                                    image.Height = bitmapImage.Height;
                                }
                                else
                                {
                                    image.Stretch = Stretch.Fill;

                                    if (Double.IsNaN(motion.Current.Size.Width) && motion.Current.Size.Height > 0)
                                    {
                                        image.Width = motion.Current.Size.Height * bitmapImage.PixelWidth / bitmapImage.PixelHeight;
                                        image.Height = motion.Current.Size.Height;
                                    }
                                    else if (motion.Current.Size.Width > 0 && Double.IsNaN(motion.Current.Size.Height))
                                    {
                                        image.Width = motion.Current.Size.Width;
                                        image.Height = motion.Current.Size.Width * bitmapImage.PixelHeight / bitmapImage.PixelWidth;
                                    }
                                    else
                                    {
                                        image.Width = motion.Current.Size.Width;
                                        image.Height = motion.Current.Size.Height;
                                    }
                                }
                            }
                            else
                            {
                                image.Stretch = Stretch.None;

                                if (!motion.Current.Size.IsEmpty)
                                {
                                    image.Width = motion.Current.Size.Width;
                                    image.Height = motion.Current.Size.Height;
                                }
                            }

                            image.Opacity = motion.Current.Opacity;
                            image.Tag = motion;

                            if (!isVisible)
                            {
                                image.Visibility = Visibility.Collapsed;
                            }

                            this.Canvas.Children.Add(image);

                            Canvas.SetLeft(image, character.Origin.X + motion.Current.Location.X);
                            Canvas.SetTop(image, character.Origin.Y + motion.Current.Location.Y);
                            Canvas.SetZIndex(image, index);

                            index++;
                            updateLayoutRequired = true;
                        }
                    }
                    else if (motion.Current.Opacity > 0)
                    {
                        BitmapImage bitmapImage;

                        if (this.cachedBitmapImageDictionary.TryGetValue(Path.GetExtension(character.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Path.GetDirectoryName(character.Script), Path.GetFileNameWithoutExtension(character.Script), motion.Current.Path) : Path.Combine(Path.GetDirectoryName(character.Script), motion.Current.Path), out bitmapImage))
                        {
                            image.Source = bitmapImage;

                            if (motion.Current.Size.IsEmpty)
                            {
                                image.Stretch = Stretch.None;
                                image.Width = bitmapImage.Width;
                                image.Height = bitmapImage.Height;
                            }
                            else
                            {
                                image.Stretch = Stretch.Fill;

                                if (Double.IsNaN(motion.Current.Size.Width) && motion.Current.Size.Height > 0)
                                {
                                    image.Width = motion.Current.Size.Height * bitmapImage.PixelWidth / bitmapImage.PixelHeight;
                                    image.Height = motion.Current.Size.Height;
                                }
                                else if (motion.Current.Size.Width > 0 && Double.IsNaN(motion.Current.Size.Height))
                                {
                                    image.Width = motion.Current.Size.Width;
                                    image.Height = motion.Current.Size.Width * bitmapImage.PixelHeight / bitmapImage.PixelWidth;
                                }
                                else
                                {
                                    image.Width = motion.Current.Size.Width;
                                    image.Height = motion.Current.Size.Height;
                                }
                            }
                        }
                        else
                        {
                            image.Source = null;
                            image.Stretch = Stretch.None;

                            if (motion.Current.Size.IsEmpty)
                            {
                                image.Width = image.Height = Double.NaN;
                            }
                            else
                            {
                                image.Width = motion.Current.Size.Width;
                                image.Height = motion.Current.Size.Height;
                            }
                        }

                        image.Opacity = motion.Current.Opacity;
                        image.Tag = motion;

                        if (image.OpacityMask == null)
                        {
                            List<string> typeList = null;
                            bool isVisible;

                            if (motion.Type == null)
                            {
                                typeList = new List<string>();
                                this.cachedMotionList.ForEach(delegate (Motion m)
                                {
                                    if (m.ZIndex == motion.ZIndex)
                                    {
                                        typeList.Add(m.Type);
                                    }
                                });
                            }

                            if (typeList == null)
                            {
                                if (character.HasTypes)
                                {
                                    typeList = new List<string>();
                                    this.cachedMotionList.ForEach(delegate (Motion m)
                                    {
                                        if (m.ZIndex == motion.ZIndex && character.Types.Contains(m.Type))
                                        {
                                            typeList.Add(m.Type);
                                        }
                                    });
                                    isVisible = typeList.Count > 0 && typeList.LastIndexOf(motion.Type) == typeList.Count - 1;
                                }
                                else
                                {
                                    isVisible = false;
                                }
                            }
                            else if (character.HasTypes)
                            {
                                isVisible = !typeList.Exists(delegate (string type)
                                {
                                    return character.Types.Contains(type);
                                });
                            }
                            else
                            {
                                isVisible = true;
                            }

                            if (isVisible)
                            {
                                image.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                image.Visibility = Visibility.Collapsed;
                            }
                        }

                        Canvas.SetLeft(image, character.Origin.X + motion.Current.Location.X);
                        Canvas.SetTop(image, character.Origin.Y + motion.Current.Location.Y);
                        Canvas.SetZIndex(image, index);

                        index++;
                    }
                    else
                    {
                        this.Canvas.Children.Remove(image);
                        updateLayoutRequired = true;
                    }
                });

                while (imageQueue.Count > 0)
                {
                    Image image = imageQueue.Dequeue();

                    this.Canvas.Children.Remove(image);
                    updateLayoutRequired = true;
                }

                if (updateLayoutRequired)
                {
                    this.Canvas.UpdateLayout();
                }
            }
        }

        private void Run()
        {
            bool isReady = this.IsVisible && !this.balloon.IsVisible;
            System.Collections.ObjectModel.Collection<Motion> motionCollection = null;
            bool closeRequired = false;

            if (isReady && this.queue.Count == 0 && this.motionQueue.Count == 0 && !this.isLast)
            {
                Sequence sequence;

                if (Script.Instance.TryDequeue(this.characterName, out sequence))
                {
                    if (sequence.Name == null && !sequence.Any())
                    {
                        if (this.isFirst)
                        {
                            this.isFirst = false;
                            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                            {
                                Run();

                                return null;
                            }), null);

                            return;
                        }
                        else
                        {
                            this.isLast = true;
                        }
                    }
                    else if (this.isFirst)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                        {
                            Run();

                            return null;
                        }), null);

                        return;
                    }
                    else
                    {
                        foreach (object o in sequence)
                        {
                            this.queue.Enqueue(o);
                        }
                    }
                }
            }

            if (this.queue.Count > 0)
            {
                if (this.queue.Peek() is Message)
                {
                    if (!this.balloon.IsVisible)
                    {
                        this.balloon.Show((Message)this.queue.Dequeue());
                    }
                }
                else if (this.queue.Peek() is System.Collections.ObjectModel.Collection<Motion>)
                {
                    if (this.motionQueue.Count == 0)
                    {
                        motionCollection = (System.Collections.ObjectModel.Collection<Motion>)this.queue.Dequeue();
                    }
                }
                else if (this.queue.Peek() is Sound)
                {
                    Sound sound = (Sound)this.queue.Dequeue();

                    if (this.IsVisible && !String.IsNullOrEmpty(sound.Path) && !this.isMute)
                    {
                        foreach (var v in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select new { character.Script, sound.Path })
                        {
                            if (Path.GetExtension(v.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                Task.Factory.StartNew(delegate
                                {
                                    FileStream fs = null;
                                    Stream s = null;

                                    try
                                    {
                                        fs = new FileStream(Path.IsPathRooted(v.Script) ? v.Script : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), v.Script), FileMode.Open, FileAccess.Read, FileShare.Read);

                                        using (ZipArchive zipArchive = new ZipArchive(fs))
                                        {
                                            fs = null;
                                            s = zipArchive.GetEntry(v.Path).Open();

                                            using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(s))
                                            {
                                                s = null;
                                                soundPlayer.Load();
                                                soundPlayer.PlaySync();
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (s != null)
                                        {
                                            s.Close();
                                        }

                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }
                                });
                            }
                            else
                            {
                                Task.Factory.StartNew(delegate (object state)
                                {
                                    string path = (string)state;

                                    using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(Path.IsPathRooted(path) ? path : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), path)))
                                    {
                                        soundPlayer.Load();
                                        soundPlayer.PlaySync();
                                    }
                                }, Path.Combine(Path.GetDirectoryName(v.Script), v.Path));
                            }
                        }
                    }
                }
            }

            if (motionCollection == null)
            {
                if (!this.isLast && this.motionQueue.Count == 0)
                {
                    this.cachedMotionList.ForEach(delegate (Motion motion)
                    {
                        if (motion.Repeats)
                        {
                            this.motionQueue.Enqueue(motion);
                        }
                    });
                }
            }
            else
            {
                foreach (Motion motion in motionCollection)
                {
                    this.motionQueue.Enqueue(motion);
                }
            }

            if (this.motionQueue.Count > 0)
            {
                Queue<Motion> q = new Queue<Motion>(this.motionQueue);
                List<Motion> motionList1 = new List<Motion>();

                if (this.cachedMotionList.Count == 0)
                {
                    int minZIndex = Int32.MaxValue;
                    int maxZIndex = Int32.MinValue;
                    Dictionary<int, List<Motion>> motionDictionary = new Dictionary<int, List<Motion>>();
                    List<Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>>> contentList = new List<Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>>>();
                    HashSet<DoubleAnimation> animationHashSet = new HashSet<DoubleAnimation>();
                    CountdownEvent countdownEvent = new CountdownEvent(1);

                    do
                    {
                        if (motionList1.Exists(delegate (Motion motion)
                        {
                            return motion.ZIndex == q.Peek().ZIndex && String.Equals(motion.Type, q.Peek().Type);
                        }))
                        {
                            q.Dequeue();
                        }
                        else
                        {
                            motionList1.Add(q.Dequeue());
                        }
                    } while (q.Count > 0);

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                    {
                        Dictionary<string, Tuple<string, MemoryStream>> dictionary = new Dictionary<string, Tuple<string, MemoryStream>>();

                        motionList1.ForEach(delegate (Motion motion)
                        {
                            List<Motion> motionList2;

                            if (motion.ZIndex < minZIndex)
                            {
                                minZIndex = motion.ZIndex;
                            }

                            if (motion.ZIndex > maxZIndex)
                            {
                                maxZIndex = motion.ZIndex;
                            }

                            if (!motionDictionary.TryGetValue(motion.ZIndex, out motionList2))
                            {
                                motionList2 = new List<Motion>();
                                motionDictionary.Add(motion.ZIndex, motionList2);
                            }

                            motionList2.Add(motion);

                            foreach (string path in from sprite in motion.Sprites where !String.IsNullOrEmpty(sprite.Path) select sprite.Path)
                            {
                                string key = Path.GetExtension(character.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Path.GetDirectoryName(character.Script), Path.GetFileNameWithoutExtension(character.Script), path) : Path.Combine(Path.GetDirectoryName(character.Script), path);

                                if (!dictionary.ContainsKey(key) && !this.cachedBitmapImageDictionary.ContainsKey(key))
                                {
                                    dictionary.Add(key, Tuple.Create<string, MemoryStream>(path, new MemoryStream()));
                                }
                            }
                        });

                        contentList.Add(Tuple.Create<string, Dictionary<string, Tuple<string, MemoryStream>>>(character.Script, dictionary));
                    }

                    Task.Factory.StartNew(delegate
                    {
                        contentList.ForEach(delegate (Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>> tuple1)
                        {
                            string path = Path.IsPathRooted(tuple1.Item1) ? tuple1.Item1 : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), tuple1.Item1);

                            if (Path.GetExtension(tuple1.Item1).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (Tuple<string, MemoryStream> tuple2 in tuple1.Item2.Values)
                                {
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        using (ZipArchive zipArchive = new ZipArchive(fs))
                                        {
                                            fs = null;

                                            ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(tuple2.Item1);

                                            using (Stream stream = zipArchiveEntry.Open())
                                            {
                                                byte[] buffer = new byte[zipArchiveEntry.Length];
                                                int bytesRead;

                                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                                {
                                                    tuple2.Item2.Write(buffer, 0, bytesRead);
                                                }

                                                tuple2.Item2.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (Tuple<string, MemoryStream> tuple2 in tuple1.Item2.Values)
                                {
                                    using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(path), tuple2.Item1), FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        byte[] buffer = new byte[fs.Length];
                                        int bytesRead;

                                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            tuple2.Item2.Write(buffer, 0, bytesRead);
                                        }

                                        tuple2.Item2.Seek(0, SeekOrigin.Begin);
                                    }
                                }
                            }
                        });
                    }).ContinueWith(delegate
                    {
                        contentList.ForEach(delegate (Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>> tuple1)
                        {
                            foreach (KeyValuePair<string, Tuple<string, MemoryStream>> kvp in tuple1.Item2)
                            {
                                BitmapImage bitmapImage;

                                if (!this.cachedBitmapImageDictionary.TryGetValue(kvp.Key, out bitmapImage))
                                {
                                    try
                                    {
                                        bitmapImage = new BitmapImage();
                                        bitmapImage.BeginInit();
                                        bitmapImage.StreamSource = kvp.Value.Item2;
                                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImage.CreateOptions = BitmapCreateOptions.None;
                                        bitmapImage.EndInit();

                                        if (bitmapImage.CanFreeze)
                                        {
                                            bitmapImage.Freeze();
                                        }

                                        this.cachedBitmapImageDictionary.Add(kvp.Key, bitmapImage);
                                    }
                                    finally
                                    {
                                        kvp.Value.Item2.Close();
                                    }
                                }
                            }
                        });

                        for (int i = minZIndex; i <= maxZIndex; i++)
                        {
                            List<Motion> motionList2;

                            if (motionDictionary.TryGetValue(i, out motionList2))
                            {
                                motionList2.ForEach(delegate (Motion motion)
                                {
                                    this.cachedMotionList.Add((Motion)motion.Clone());
                                });
                            }
                        }

                        Render();

                        Storyboard storyboard1 = new Storyboard();
                        DoubleAnimation doubleAnimation1 = new DoubleAnimation(0, this.opacity, TimeSpan.FromMilliseconds(500));
                        SineEase sineEase1 = new SineEase();

                        foreach (KeyValuePair<Storyboard, Window> kvp in this.fadeStoryboardDictionary)
                        {
                            kvp.Key.Stop(kvp.Value);
                        }

                        this.fadeStoryboardDictionary.Clear();

                        sineEase1.EasingMode = EasingMode.EaseOut;

                        doubleAnimation1.EasingFunction = sineEase1;
                        doubleAnimation1.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                        {
                            Clock clock = (Clock)s;

                            if (clock.CurrentState == ClockState.Stopped || clock.CurrentState == ClockState.Filling)
                            {
                                if (clock.CurrentState == ClockState.Filling)
                                {
                                    this.Opacity = this.opacity;
                                    storyboard1.Remove(this);
                                    this.fadeStoryboardDictionary.Remove(storyboard1);
                                }

                                if (!animationHashSet.Contains(doubleAnimation1))
                                {
                                    countdownEvent.Signal();
                                    animationHashSet.Add(doubleAnimation1);
                                }
                            }
                        });

                        storyboard1.Children.Add(doubleAnimation1);

                        Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(Window.OpacityProperty));

                        this.fadeStoryboardDictionary.Add(storyboard1, this);
                        this.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, true);

                        if (this.balloon.Opacity != 1)
                        {
                            Storyboard storyboard2 = new Storyboard();
                            DoubleAnimation doubleAnimation2 = new DoubleAnimation(this.balloon.Opacity, 1, TimeSpan.FromMilliseconds(500));
                            SineEase sineEase2 = new SineEase();

                            sineEase2.EasingMode = EasingMode.EaseOut;

                            doubleAnimation2.EasingFunction = sineEase2;
                            doubleAnimation2.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                            {
                                Clock clock = (Clock)s;

                                if (clock.CurrentState == ClockState.Stopped || clock.CurrentState == ClockState.Filling)
                                {
                                    if (clock.CurrentState == ClockState.Filling)
                                    {
                                        this.balloon.Opacity = 1;
                                        storyboard2.Remove(this.balloon);
                                        this.fadeStoryboardDictionary.Remove(storyboard2);
                                    }

                                    if (!animationHashSet.Contains(doubleAnimation2))
                                    {
                                        countdownEvent.Signal();
                                        animationHashSet.Add(doubleAnimation2);
                                    }
                                }
                            });

                            storyboard2.Children.Add(doubleAnimation2);

                            Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Window.OpacityProperty));

                            this.fadeStoryboardDictionary.Add(storyboard2, this.balloon);
                            this.balloon.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, true);

                            countdownEvent.AddCount();
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext()).ContinueWith(delegate
                    {
                        countdownEvent.Wait();
                        countdownEvent.Dispose();

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                        {
                            Run();

                            return null;
                        }), null);
                    });

                    return;
                }

                this.motionQueue.Clear();

                do
                {
                    if (motionList1.Exists(delegate (Motion motion)
                    {
                        return motion.ZIndex == q.Peek().ZIndex && String.Equals(motion.Type, q.Peek().Type);
                    }))
                    {
                        this.motionQueue.Enqueue(q.Dequeue());
                    }
                    else
                    {
                        motionList1.Add(q.Dequeue());
                    }
                } while (q.Count > 0);

                if (motionList1.Count > 0)
                {
                    int minZIndex = Int32.MaxValue;
                    int maxZIndex = Int32.MinValue;
                    HashSet<int> zIndexHashSet = new HashSet<int>();
                    List<Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>>> contentList = new List<Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>>>();
                    HashSet<string> keyHashSet = new HashSet<string>();

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                    {
                        Dictionary<string, Tuple<string, MemoryStream>> dictionary = new Dictionary<string, Tuple<string, MemoryStream>>();

                        motionList1.ForEach(delegate (Motion motion)
                        {
                            if (motion.ZIndex < minZIndex)
                            {
                                minZIndex = motion.ZIndex;
                            }

                            if (motion.ZIndex > maxZIndex)
                            {
                                maxZIndex = motion.ZIndex;
                            }

                            if (!zIndexHashSet.Contains(motion.ZIndex))
                            {
                                zIndexHashSet.Add(motion.ZIndex);
                            }

                            foreach (string path in from sprite in motion.Sprites where !String.IsNullOrEmpty(sprite.Path) select sprite.Path)
                            {
                                string key = Path.GetExtension(character.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Path.GetDirectoryName(character.Script), Path.GetFileNameWithoutExtension(character.Script), path) : Path.Combine(Path.GetDirectoryName(character.Script), path);

                                if (!dictionary.ContainsKey(key) && !this.cachedBitmapImageDictionary.ContainsKey(key))
                                {
                                    dictionary.Add(key, Tuple.Create<string, MemoryStream>(path, new MemoryStream()));
                                }

                                if (!keyHashSet.Contains(key))
                                {
                                    keyHashSet.Add(key);
                                }
                            }
                        });

                        contentList.Add(Tuple.Create<string, Dictionary<string, Tuple<string, MemoryStream>>>(character.Script, dictionary));
                    }

                    Task.Factory.StartNew(delegate
                    {
                        contentList.ForEach(delegate (Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>> tuple1)
                        {
                            string path = Path.IsPathRooted(tuple1.Item1) ? tuple1.Item1 : Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), tuple1.Item1);

                            if (Path.GetExtension(tuple1.Item1).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (Tuple<string, MemoryStream> tuple2 in tuple1.Item2.Values)
                                {
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        using (ZipArchive zipArchive = new ZipArchive(fs))
                                        {
                                            fs = null;

                                            ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(tuple2.Item1);

                                            using (Stream stream = zipArchiveEntry.Open())
                                            {
                                                byte[] buffer = new byte[zipArchiveEntry.Length];
                                                int bytesRead;

                                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                                {
                                                    tuple2.Item2.Write(buffer, 0, bytesRead);
                                                }

                                                tuple2.Item2.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (Tuple<string, MemoryStream> tuple2 in tuple1.Item2.Values)
                                {
                                    using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(path), tuple2.Item1), FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        byte[] buffer = new byte[fs.Length];
                                        int bytesRead;

                                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            tuple2.Item2.Write(buffer, 0, bytesRead);
                                        }

                                        tuple2.Item2.Seek(0, SeekOrigin.Begin);
                                    }
                                }
                            }
                        });
                    }).ContinueWith(delegate
                    {
                        List<Motion> previousMotionList = new List<Motion>();
                        List<Sprite> cachedSpriteList = new List<Sprite>();
                        double maxFrameRate = 0;
                        Dictionary<Motion, int> skipDictionary = new Dictionary<Motion, int>();
                        Dictionary<Motion, Motion> switchDictionary = new Dictionary<Motion, Motion>();

                        contentList.ForEach(delegate (Tuple<string, Dictionary<string, Tuple<string, MemoryStream>>> tuple1)
                        {
                            foreach (KeyValuePair<string, Tuple<string, MemoryStream>> kvp in tuple1.Item2)
                            {
                                BitmapImage bitmapImage;

                                if (!this.cachedBitmapImageDictionary.TryGetValue(kvp.Key, out bitmapImage))
                                {
                                    try
                                    {
                                        bitmapImage = new BitmapImage();
                                        bitmapImage.BeginInit();
                                        bitmapImage.StreamSource = kvp.Value.Item2;
                                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImage.CreateOptions = BitmapCreateOptions.None;
                                        bitmapImage.EndInit();

                                        if (bitmapImage.CanFreeze)
                                        {
                                            bitmapImage.Freeze();
                                        }

                                        this.cachedBitmapImageDictionary.Add(kvp.Key, bitmapImage);
                                    }
                                    finally
                                    {
                                        kvp.Value.Item2.Close();
                                    }
                                }
                            }
                        });

                        foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                        {
                            this.cachedMotionList.ForEach(delegate (Motion motion)
                            {
                                if (zIndexHashSet.Contains(motion.ZIndex))
                                {
                                    if (character.HasTypes)
                                    {
                                        if (!character.Types.Contains(motion.Type))
                                        {
                                            return;
                                        }
                                    }
                                    else if (motion.Type != null)
                                    {
                                        return;
                                    }
                                }

                                if (motion.ZIndex < minZIndex)
                                {
                                    minZIndex = motion.ZIndex;
                                }

                                if (motion.ZIndex > maxZIndex)
                                {
                                    maxZIndex = motion.ZIndex;
                                }

                                foreach (string path in from sprite in motion.Sprites where !String.IsNullOrEmpty(sprite.Path) select sprite.Path)
                                {
                                    string key = Path.GetExtension(character.Script).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Path.GetDirectoryName(character.Script), Path.GetFileNameWithoutExtension(character.Script), path) : Path.Combine(Path.GetDirectoryName(character.Script), path);

                                    if (!keyHashSet.Contains(key))
                                    {
                                        keyHashSet.Add(key);
                                    }
                                }

                                previousMotionList.Add(motion);
                                cachedSpriteList.Add(motion.Current);
                            });
                        }

                        foreach (string s in (from s in this.cachedBitmapImageDictionary.Keys where !keyHashSet.Contains(s) select s).ToArray())
                        {
                            this.cachedBitmapImageDictionary.Remove(s);
                        }

                        this.cachedMotionList.Clear();

                        for (int i = minZIndex; i <= maxZIndex; i++)
                        {
                            List<Motion> motionList2 = previousMotionList.FindAll(delegate (Motion m)
                            {
                                return i == m.ZIndex;
                            });
                            List<Motion> motionList3 = motionList1.FindAll(delegate (Motion m)
                            {
                                return i == m.ZIndex;
                            });

                            motionList2.ForEach(delegate (Motion m1)
                            {
                                Motion motion = null;
                                Motion nextMotion = null;
                                Motion m2 = motionList3.Find(delegate (Motion m)
                                {
                                    return String.Equals(m.Type, m1.Type);
                                });

                                if (m2 != null)
                                {
                                    if (m1 != m2 && m1.Position > 0 && m1.Position < m1.Sprites.Count - 1)
                                    {
                                        nextMotion = (Motion)m2.Clone();
                                    }
                                    else
                                    {
                                        motion = (Motion)m2.Clone();
                                    }

                                    motionList3.Remove(m2);
                                }

                                if (motion == null)
                                {
                                    motion = (Motion)m1.Clone();
                                }

                                if (motion.FrameRate > maxFrameRate)
                                {
                                    maxFrameRate = motion.FrameRate;
                                }

                                if (motion.Repeats && motion.Position > 0)
                                {
                                    skipDictionary.Add(motion, motion.Position);
                                }

                                if (nextMotion != null)
                                {
                                    switchDictionary.Add(motion, nextMotion);
                                }

                                this.cachedMotionList.Add(motion);
                            });
                            motionList3.ForEach(delegate (Motion m2)
                            {
                                Motion motion = (Motion)m2.Clone();

                                if (motion.FrameRate > maxFrameRate)
                                {
                                    maxFrameRate = motion.FrameRate;
                                }

                                if (motion.Repeats && motion.Position > 0)
                                {
                                    skipDictionary.Add(motion, motion.Position);
                                }

                                this.cachedMotionList.Add(motion);
                            });
                        }

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                        {
                            Animate(false, System.Diagnostics.Stopwatch.StartNew(), this.frameRate, maxFrameRate, 0, 0, switchDictionary, skipDictionary, 0, 0, cachedSpriteList);

                            return null;
                        }), null);
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    return;
                }
            }

            if (this.isLast && this.queue.Count == 0)
            {
                if (this.balloon.IsVisible)
                {
                    Task.Factory.StartNew(delegate
                    {
                        Thread.Sleep(1);

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                        {
                            Run();

                            return null;
                        }), null);
                    });

                    return;
                }
                else if (this.Opacity > 0)
                {
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimation doubleAnimation = new DoubleAnimation(this.Opacity, 0, TimeSpan.FromMilliseconds(500));
                    SineEase sineEase = new SineEase();

                    foreach (KeyValuePair<Storyboard, Window> kvp in this.fadeStoryboardDictionary)
                    {
                        kvp.Key.Stop(kvp.Value);
                    }

                    this.fadeStoryboardDictionary.Clear();

                    sineEase.EasingMode = EasingMode.EaseIn;

                    doubleAnimation.EasingFunction = sineEase;
                    doubleAnimation.CurrentStateInvalidated += new EventHandler(delegate (object s, EventArgs e)
                    {
                        Clock clock = (Clock)s;

                        if (clock.CurrentState == ClockState.Stopped && this.Opacity > 0)
                        {
                            this.isLast = false;
                            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                            {
                                Run();

                                return null;
                            }), null);
                        }
                        else if (clock.CurrentState == ClockState.Filling)
                        {
                            this.Opacity = 0;
                            storyboard.Remove(this);
                            this.fadeStoryboardDictionary.Remove(storyboard);

                            if (this == Application.Current.MainWindow || this.Owner != null)
                            {
                                foreach (Window window in Application.Current.Windows)
                                {
                                    Agent agent = window as Agent;

                                    if (agent != null && agent.Opacity > 0 || window as Balloon == null && window.Owner != null)
                                    {
                                        Task.Factory.StartNew(delegate
                                        {
                                            Thread.Sleep(1);

                                            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                                            {
                                                Run();

                                                return null;
                                            }), null);
                                        });

                                        return;
                                    }
                                }
                            }

                            this.LayoutRoot.Width = this.LayoutRoot.Height = this.Canvas.Width = this.Canvas.Height = 0;
                            Close();
                        }
                    });

                    storyboard.Children.Add(doubleAnimation);

                    Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(Window.OpacityProperty));

                    this.fadeStoryboardDictionary.Add(storyboard, this);
                    this.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, true);

                    return;
                }
                else
                {
                    closeRequired = true;
                }
            }
            else if (this.queue.Count > 0)
            {
                if (this.queue.Peek() is Message && !isReady)
                {
                    Task.Factory.StartNew(delegate
                    {
                        Thread.Sleep(1);

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                        {
                            Run();

                            return null;
                        }), null);
                    });

                    return;
                }
            }
            else
            {
                Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(1);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                    {
                        Run();

                        return null;
                    }), null);
                });

                return;
            }

            if (closeRequired)
            {
                if (this == Application.Current.MainWindow || this.Owner != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        Agent agent = window as Agent;

                        if (agent != null && agent.Opacity > 0 || window as Balloon == null && window.Owner != null)
                        {
                            Task.Factory.StartNew(delegate
                            {
                                Thread.Sleep(1);

                                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                                {
                                    Run();

                                    return null;
                                }), null);
                            });

                            return;
                        }
                    }
                }

                this.LayoutRoot.Width = this.LayoutRoot.Height = this.Canvas.Width = this.Canvas.Height = 0;
                Close();
            }
            else
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                {
                    Run();

                    return null;
                }), null);
            }
        }

        private void Animate(bool isRunning, System.Diagnostics.Stopwatch stopwatch, double frameRate, double maxFrameRate, double nextFrame, double nextRedrawFrame, Dictionary<Motion, Motion> switchDictionary, Dictionary<Motion, int> skipDictionary, long currentFrame, long maxFrames, List<Sprite> spriteList)
        {
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds >= nextFrame)
            {
                Sprite[] sprites = spriteList.ToArray();

                if (isRunning)
                {
                    if (switchDictionary.Count > 0 && this.cachedMotionList.TrueForAll(delegate (Motion motion)
                    {
                        return motion.Position == motion.Sprites.Count - 1;
                    }))
                    {
                        foreach (KeyValuePair<Motion, Motion> kvp in switchDictionary)
                        {
                            int index = this.cachedMotionList.IndexOf(kvp.Key);

                            if (index >= 0)
                            {
                                kvp.Value.Reset();
                                this.cachedMotionList[index] = kvp.Value;
                            }
                        }

                        switchDictionary.Clear();
                        skipDictionary.Clear();

                        spriteList.Clear();
                        this.cachedMotionList.ForEach(delegate (Motion motion)
                        {
                            spriteList.Add(motion.Current);
                        });
                    }
                    else
                    {
                        spriteList.Clear();
                        this.cachedMotionList.ForEach(delegate (Motion motion)
                        {
                            int skipFrames;
                            long frames = currentFrame;

                            if (skipDictionary.TryGetValue(motion, out skipFrames))
                            {
                                frames += skipFrames * (int)Math.Round(maxFrameRate / motion.FrameRate);
                            }

                            if (frames % (int)Math.Round(maxFrameRate / motion.FrameRate) == 0 && !motion.Next() && motion.Repeats && switchDictionary.Count == 0)
                            {
                                motion.Reset();
                            }

                            spriteList.Add(motion.Current);
                        });
                    }
                }
                else
                {
                    spriteList.Clear();
                    this.cachedMotionList.ForEach(delegate (Motion motion)
                    {
                        long frames;
                        Motion m;

                        if (switchDictionary.TryGetValue(motion, out m))
                        {
                            frames = (motion.Sprites.Count - motion.Position) * (int)Math.Round(maxFrameRate / motion.FrameRate);

                            if (m.Repeats)
                            {
                                frames += (int)Math.Round(maxFrameRate / m.FrameRate);
                            }
                            else
                            {
                                frames += m.Sprites.Count * (int)Math.Round(maxFrameRate / m.FrameRate);
                            }
                        }
                        else if (motion.Repeats)
                        {
                            frames = (int)Math.Round(maxFrameRate / motion.FrameRate);
                        }
                        else
                        {
                            frames = (motion.Sprites.Count - motion.Position) * (int)Math.Round(maxFrameRate / motion.FrameRate);
                        }

                        if (frames > maxFrames)
                        {
                            maxFrames = frames;
                        }

                        spriteList.Add(motion.Current);
                    });

                    if (frameRate > maxFrameRate)
                    {
                        frameRate = maxFrameRate;
                    }
                }

                if (elapsedMilliseconds >= nextRedrawFrame)
                {
                    if (!spriteList.SequenceEqual(sprites) && stopwatch.ElapsedMilliseconds < nextRedrawFrame + 1000 / frameRate)
                    {
                        Render();
                    }

                    nextRedrawFrame += 1000 / frameRate;
                }

                nextFrame += 1000 / maxFrameRate;
            }
            else
            {
                Task.Factory.StartNew(delegate
                {
                    Thread.Sleep(1);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                    {
                        Animate(true, stopwatch, frameRate, maxFrameRate, nextFrame, nextRedrawFrame, switchDictionary, skipDictionary, currentFrame, maxFrames, spriteList);

                        return null;
                    }), null);
                });

                return;
            }

            currentFrame++;

            if (currentFrame < maxFrames)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                {
                    Animate(true, stopwatch, frameRate, maxFrameRate, nextFrame, nextRedrawFrame, switchDictionary, skipDictionary, currentFrame, maxFrames, spriteList);

                    return null;
                }), null);
            }
            else
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                {
                    Run();

                    return null;
                }), null);
            }
        }

        private Window CreateHudWindow(Brush backgroundBrush, Color foregroundColor, Geometry geometry, FontFamily fontFamily, double fontSize, FontStretch fontStretch, FontStyle fontStyle, FontWeight fontWeight, string text)
        {
            Window window = new Window();
            ContentControl contentControl = new ContentControl();
            Border border1 = new Border();
            StackPanel stackPanel = new StackPanel();
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            Border border2 = new Border();
            Label label = new Label();
            SolidColorBrush foregroundBrush = new SolidColorBrush(foregroundColor);
            DropShadowEffect dropShadowEffect = new DropShadowEffect();

            if (foregroundBrush.CanFreeze)
            {
                foregroundBrush.Freeze();
            }

            window.Owner = this;
            window.Title = this.Title;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.AllowsTransparency = true;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.Topmost = true;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Background = Brushes.Transparent;
            window.Loaded += new RoutedEventHandler(delegate
            {
                ScaleTransform scaleTransform = contentControl.RenderTransform as ScaleTransform;
                Storyboard storyboard = new Storyboard();
                DoubleAnimation doubleAnimation1 = new DoubleAnimation(contentControl.Opacity, 1, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation2 = new DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation3 = new DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation4 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation5 = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500));
                DoubleAnimation doubleAnimation6 = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500));
                SineEase sineEase1 = new SineEase();
                SineEase sineEase2 = new SineEase();
                SineEase sineEase3 = new SineEase();
                SineEase sineEase4 = new SineEase();
                SineEase sineEase5 = new SineEase();
                SineEase sineEase6 = new SineEase();

                if (contentControl.ActualWidth > contentControl.ActualHeight)
                {
                    border1.Width = border1.Height = contentControl.ActualWidth;
                    contentControl.Width = contentControl.Height = contentControl.ActualWidth * 1.5;

                    if (scaleTransform != null)
                    {
                        scaleTransform.CenterX = scaleTransform.CenterY = contentControl.Height / 2;
                    }
                }
                else
                {
                    border1.Width = border1.Height = contentControl.ActualHeight;
                    contentControl.Width = contentControl.Height = contentControl.ActualHeight * 1.5;

                    if (scaleTransform != null)
                    {
                        scaleTransform.CenterX = scaleTransform.CenterY = contentControl.Width / 2;
                    }
                }

                sineEase1.EasingMode = sineEase2.EasingMode = sineEase3.EasingMode = EasingMode.EaseOut;
                sineEase4.EasingMode = sineEase5.EasingMode = sineEase6.EasingMode = EasingMode.EaseIn;
                doubleAnimation1.EasingFunction = sineEase1;
                doubleAnimation2.EasingFunction = sineEase2;
                doubleAnimation3.EasingFunction = sineEase3;
                doubleAnimation4.BeginTime = TimeSpan.FromMilliseconds(1000);
                doubleAnimation4.EasingFunction = sineEase4;
                doubleAnimation5.BeginTime = TimeSpan.FromMilliseconds(1000);
                doubleAnimation5.EasingFunction = sineEase5;
                doubleAnimation6.BeginTime = TimeSpan.FromMilliseconds(1000);
                doubleAnimation6.EasingFunction = sineEase6;

                storyboard.Children.Add(doubleAnimation1);
                storyboard.Children.Add(doubleAnimation2);
                storyboard.Children.Add(doubleAnimation3);
                storyboard.Children.Add(doubleAnimation4);
                storyboard.Children.Add(doubleAnimation5);
                storyboard.Children.Add(doubleAnimation6);
                storyboard.CurrentStateInvalidated += new EventHandler(delegate (object sender, EventArgs e)
                {
                    if (((Clock)sender).CurrentState == ClockState.Filling)
                    {
                        window.Close();
                    }
                });

                Storyboard.SetTarget(doubleAnimation1, contentControl);
                Storyboard.SetTarget(doubleAnimation2, contentControl);
                Storyboard.SetTarget(doubleAnimation3, contentControl);
                Storyboard.SetTarget(doubleAnimation4, contentControl);
                Storyboard.SetTarget(doubleAnimation5, contentControl);
                Storyboard.SetTarget(doubleAnimation6, contentControl);
                Storyboard.SetTargetProperty(doubleAnimation1, new PropertyPath(ContentControl.OpacityProperty));
                Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                Storyboard.SetTargetProperty(doubleAnimation3, new PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty));
                Storyboard.SetTargetProperty(doubleAnimation4, new PropertyPath(ContentControl.OpacityProperty));
                Storyboard.SetTargetProperty(doubleAnimation5, new PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty));
                Storyboard.SetTargetProperty(doubleAnimation6, new PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty));

                storyboard.Begin();
            });

            contentControl.UseLayoutRounding = true;
            contentControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            contentControl.VerticalAlignment = VerticalAlignment.Stretch;
            contentControl.Opacity = 0;
            contentControl.RenderTransform = new ScaleTransform(1, 1);

            window.Content = contentControl;

            border1.HorizontalAlignment = HorizontalAlignment.Center;
            border1.VerticalAlignment = VerticalAlignment.Center;
            border1.Margin = new Thickness(0);
            border1.Padding = new Thickness(16);
            border1.CornerRadius = new CornerRadius(4);
            border1.Background = backgroundBrush;

            contentControl.Content = border1;

            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.VerticalAlignment = VerticalAlignment.Center;
            stackPanel.Orientation = Orientation.Horizontal;
            stackPanel.Background = Brushes.Transparent;

            border1.Child = stackPanel;

            path.HorizontalAlignment = HorizontalAlignment.Stretch;
            path.VerticalAlignment = VerticalAlignment.Center;
            path.Fill = foregroundBrush;
            path.Data = geometry;

            stackPanel.Children.Add(path);

            border2.HorizontalAlignment = HorizontalAlignment.Stretch;
            border2.VerticalAlignment = VerticalAlignment.Stretch;
            border2.Margin = new Thickness(0);
            border2.Padding = new Thickness(0);
            border2.CornerRadius = new CornerRadius(0);
            border2.Background = Brushes.Transparent;

            dropShadowEffect.BlurRadius = 1;
            dropShadowEffect.Color = Math.Max(Math.Max(foregroundColor.R, foregroundColor.G), foregroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
            dropShadowEffect.Direction = 270;
            dropShadowEffect.Opacity = 0.5;
            dropShadowEffect.ShadowDepth = 1;

            if (dropShadowEffect.CanFreeze)
            {
                dropShadowEffect.Freeze();
            }

            border2.Effect = dropShadowEffect;

            stackPanel.Children.Add(border2);

            label.HorizontalAlignment = HorizontalAlignment.Stretch;
            label.VerticalAlignment = VerticalAlignment.Stretch;
            label.FontFamily = FontFamily;
            label.FontSize = fontSize;
            label.FontStretch = fontStretch;
            label.FontStyle = fontStyle;
            label.FontWeight = fontWeight;
            label.Foreground = foregroundBrush;
            label.FontWeight = fontWeight;
            label.Content = text;

            RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled);

            border2.Child = label;

            return window;
        }

        private Geometry CreateStarGeometry(Rect rect)
        {
            StreamGeometry streamGeometry = new StreamGeometry();

            streamGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext context = streamGeometry.Open())
            {
                context.BeginFigure(new Point(rect.X, rect.Y + rect.Height * 3 / 8), true, true);
                context.LineTo(new Point(rect.X + rect.Width * 7 / 20, rect.Y + rect.Height * 13 / 40), true, false);
                context.LineTo(new Point(rect.X + rect.Width / 2, rect.Y), true, false);
                context.LineTo(new Point(rect.X + rect.Width * 13 / 20, rect.Y + rect.Height * 13 / 40), true, false);
                context.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height * 3 / 8), true, false);
                context.LineTo(new Point(rect.X + rect.Width * 3 / 4, rect.Y + rect.Height * 5 / 8), true, false);
                context.LineTo(new Point(rect.X + rect.Width * 4 / 5, rect.Y + rect.Height), true, false);
                context.LineTo(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height * 33 / 40), true, false);
                context.LineTo(new Point(rect.X + rect.Width / 5, rect.Y + rect.Height), true, false);
                context.LineTo(new Point(rect.X + rect.Width / 4, rect.Y + rect.Height * 5 / 8), true, false);
            }

            return streamGeometry;
        }
    }
}
