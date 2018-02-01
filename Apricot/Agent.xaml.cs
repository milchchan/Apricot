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
        public event EventHandler DrawClipboard = null;
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
            this.ContextMenu = new ContextMenu();

            MenuItem opacityMenuItem = new MenuItem();
            MenuItem scalingMenuItem = new MenuItem();
            MenuItem refreshMenuItem = new MenuItem();
            MenuItem topmostMenuItem = new MenuItem();
            MenuItem showInTaskbarMenuItem = new MenuItem();
            MenuItem muteMenuItem = new MenuItem();
            MenuItem charactersMenuItem = new MenuItem();
            MenuItem updateMenuItem = new MenuItem();
            MenuItem exitMenuItem = new MenuItem();
            double opacity = 1;
            double scale = 2;

            opacityMenuItem.Header = Properties.Resources.Opacity;

            do
            {
                MenuItem menuItem = new MenuItem();

                menuItem.Header = String.Concat(((int)Math.Floor(opacity * 100)).ToString(System.Globalization.CultureInfo.CurrentCulture), Properties.Resources.Percent);
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

            scalingMenuItem.Header = Properties.Resources.Scaling;

            do
            {
                MenuItem menuItem = new MenuItem();

                menuItem.Header = String.Concat(((int)Math.Floor(scale * 100)).ToString(System.Globalization.CultureInfo.CurrentCulture), Properties.Resources.Percent);
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

            refreshMenuItem.Header = Properties.Resources.Refresh;
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
            topmostMenuItem.Header = Properties.Resources.Topmost;
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
            showInTaskbarMenuItem.Header = Properties.Resources.ShowInTaskbar;
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
            muteMenuItem.Header = Properties.Resources.Mute;
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
            charactersMenuItem.Header = Properties.Resources.Characters;
            updateMenuItem.Header = Properties.Resources.Update;
            updateMenuItem.Click += new RoutedEventHandler(delegate
            {
                Script.Instance.Update(true);
            });
            exitMenuItem.Header = Properties.Resources.Exit;
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
            this.ContextMenu.Items.Add(updateMenuItem);
            this.ContextMenu.Items.Add(new Separator());
            this.ContextMenu.Items.Add(exitMenuItem);
            this.ContextMenu.Opened += new RoutedEventHandler(delegate
            {
                Agent agent = Application.Current.MainWindow as Agent;

                if (agent != null)
                {
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

                    List<MenuItem> menuItemList = new List<MenuItem>(charactersMenuItem.Items.Cast<MenuItem>());
                    HashSet<string> pathHashSet = new HashSet<string>();
                    LinkedList<KeyValuePair<Character, string>> characterLinkedList = new LinkedList<KeyValuePair<Character, string>>();

                    foreach (Character character in Script.Instance.Characters)
                    {
                        string path = Path.IsPathRooted(character.Script) ? character.Script : Path.GetFullPath(character.Script);

                        if (!pathHashSet.Contains(path))
                        {
                            pathHashSet.Add(path);
                        }

                        characterLinkedList.AddLast(new KeyValuePair<Character, string>(character, path));
                    }

                    List<KeyValuePair<string, string>> keyValuePairList = (from fileName in Directory.EnumerateFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "*", SearchOption.AllDirectories) let extension = Path.GetExtension(fileName) let isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) where isZip || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase) select new KeyValuePair<bool, string>(isZip, fileName)).Concat(from path in pathHashSet select new KeyValuePair<bool, string>(Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase), path)).Aggregate<KeyValuePair<bool, string>, List<KeyValuePair<string, string>>>(new List<KeyValuePair<string, string>>(), (list, kvp1) =>
                    {
                        if (!list.Exists(delegate (KeyValuePair<string, string> kvp2)
                        {
                            return kvp2.Value.Equals(kvp1.Value);
                        }))
                        {
                            if (kvp1.Key)
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(kvp1.Value, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    using (ZipArchive zipArchive = new ZipArchive(fs))
                                    {
                                        fs = null;

                                        foreach (ZipArchiveEntry zipArchiveEntry in from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry)
                                        {
                                            Stream stream = null;

                                            try
                                            {
                                                stream = zipArchiveEntry.Open();

                                                foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(stream).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                                {
                                                    list.Add(new KeyValuePair<string, string>(attribute, kvp1.Value));
                                                }
                                            }
                                            catch
                                            {
                                                return list;
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
                            else
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(kvp1.Value, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    foreach (string attribute in from attribute in ((System.Collections.IEnumerable)XDocument.Load(fs).XPathEvaluate("/script/character/@name")).Cast<XAttribute>() select attribute.Value)
                                    {
                                        list.Add(new KeyValuePair<string, string>(attribute, kvp1.Value));
                                    }
                                }
                                catch
                                {
                                    return list;
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

                        return list;
                    });

                    charactersMenuItem.Items.Clear();

                    keyValuePairList.Sort(delegate (KeyValuePair<string, string> kvp1, KeyValuePair<string, string> kvp2)
                    {
                        return String.Compare(kvp1.Key, kvp2.Key, StringComparison.CurrentCulture);
                    });
                    keyValuePairList.ForEach(delegate (KeyValuePair<string, string> kvp)
                    {
                        for (LinkedListNode<KeyValuePair<Character, string>> nextLinkedListNode = characterLinkedList.First; nextLinkedListNode != null; nextLinkedListNode = nextLinkedListNode.Next)
                        {
                            if (nextLinkedListNode.Value.Key.Name.Equals(kvp.Key) && nextLinkedListNode.Value.Value.Equals(kvp.Value))
                            {
                                MenuItem selectedMenuItem = menuItemList.Find(delegate (MenuItem menuItem)
                                {
                                    return kvp.Key.Equals(menuItem.Header as string) && kvp.Value.Equals(menuItem.Tag as string) && (menuItem.IsChecked || menuItem.HasItems);
                                });

                                if (selectedMenuItem == null)
                                {
                                    selectedMenuItem = new MenuItem();
                                    selectedMenuItem.Header = kvp.Key;
                                    selectedMenuItem.Tag = kvp.Value;
                                }
                                else
                                {
                                    selectedMenuItem.Items.Clear();
                                    menuItemList.Remove(selectedMenuItem);
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
                                            if (nextLinkedListNode.Value.Key.HasTypes)
                                            {
                                                menuItem.IsChecked = nextLinkedListNode.Value.Key.Types.Contains(menuItem.Header as string);
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
                                    childMenuItem.Tag = nextLinkedListNode.Value.Key.Name;
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

                                    if (nextLinkedListNode.Value.Key.HasTypes)
                                    {
                                        childMenuItem.IsChecked = nextLinkedListNode.Value.Key.Types.Contains(childMenuItem.Header as string);
                                    }
                                    else
                                    {
                                        childMenuItem.IsChecked = false;
                                    }

                                    childMenuItemList.Add(childMenuItem);
                                });

                                selectedMenuItem.Items.Clear();

                                if (childMenuItemList.Count > 0)
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

                        MenuItem unselectedMenuItem = menuItemList.Find(delegate (MenuItem menuItem)
                        {
                            return kvp.Key.Equals(menuItem.Header as string) && kvp.Value.Equals(menuItem.Tag as string) && !menuItem.IsChecked && !menuItem.HasItems;
                        });

                        if (unselectedMenuItem == null)
                        {
                            unselectedMenuItem = new MenuItem();
                            unselectedMenuItem.Header = kvp.Key;
                            unselectedMenuItem.Tag = kvp.Value;
                            unselectedMenuItem.Click += new RoutedEventHandler(delegate
                            {
                                string tag = unselectedMenuItem.Tag as string;

                                if (tag != null)
                                {
                                    List<Character> characterList = new List<Character>();

                                    if (Path.GetExtension(tag).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tag, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            using (ZipArchive zipArchive = new ZipArchive(fs))
                                            {
                                                fs = null;

                                                StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                                if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                                {
                                                    stringBuilder.Append(Path.DirectorySeparatorChar);
                                                }

                                                string path = tag.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tag.Remove(0, stringBuilder.Length) : tag;

                                                foreach (ZipArchiveEntry zipArchiveEntry in from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry)
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
                                    else
                                    {
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(tag, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                            if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                            {
                                                stringBuilder.Append(Path.DirectorySeparatorChar);
                                            }

                                            string path = tag.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? tag.Remove(0, stringBuilder.Length) : tag;

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

                                    if (characterList.Count > 0)
                                    {
                                        Switch(characterList);
                                    }
                                }
                            });
                        }
                        else
                        {
                            menuItemList.Remove(unselectedMenuItem);
                        }

                        charactersMenuItem.Items.Add(unselectedMenuItem);
                    });
                }
            });

            if (this == Application.Current.MainWindow)
            {
                System.Configuration.Configuration config = null;
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

                if (Directory.Exists(directory))
                {
                    string fileName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config") where fileName.Equals(Path.GetFileNameWithoutExtension(s)) select s)
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

                if (config.AppSettings.Settings["Left"] != null && config.AppSettings.Settings["Top"] != null)
                {
                    if (config.AppSettings.Settings["Left"].Value.Length > 0 && config.AppSettings.Settings["Top"].Value.Length > 0)
                    {
                        this.Left = Double.Parse(config.AppSettings.Settings["Left"].Value, System.Globalization.CultureInfo.InvariantCulture);
                        this.Top = Double.Parse(config.AppSettings.Settings["Top"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                if (config.AppSettings.Settings["Opacity"] != null)
                {
                    if (config.AppSettings.Settings["Opacity"].Value.Length > 0)
                    {
                        this.opacity = Double.Parse(config.AppSettings.Settings["Opacity"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                if (config.AppSettings.Settings["Scale"] != null)
                {
                    if (config.AppSettings.Settings["Scale"].Value.Length > 0)
                    {
                        this.scale = this.ZoomScaleTransform.ScaleX = this.ZoomScaleTransform.ScaleY = Double.Parse(config.AppSettings.Settings["Scale"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                if (config.AppSettings.Settings["Topmost"] != null)
                {
                    if (config.AppSettings.Settings["Topmost"].Value.Length > 0)
                    {
                        this.Topmost = Boolean.Parse(config.AppSettings.Settings["Topmost"].Value);
                    }
                }

                if (config.AppSettings.Settings["ShowInTaskbar"] != null)
                {
                    if (config.AppSettings.Settings["ShowInTaskbar"].Value.Length > 0)
                    {
                        this.ShowInTaskbar = Boolean.Parse(config.AppSettings.Settings["ShowInTaskbar"].Value);
                    }
                }

                if (config.AppSettings.Settings["DropShadow"] != null)
                {
                    if (config.AppSettings.Settings["DropShadow"].Value.Length > 0)
                    {
                        if (Boolean.Parse(config.AppSettings.Settings["DropShadow"].Value))
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
                }

                if (config.AppSettings.Settings["Mute"] != null)
                {
                    if (config.AppSettings.Settings["Mute"].Value.Length > 0)
                    {
                        this.isMute = Boolean.Parse(config.AppSettings.Settings["Mute"].Value);
                    }
                }

                if (config.AppSettings.Settings["FrameRate"] != null)
                {
                    if (config.AppSettings.Settings["FrameRate"].Value.Length > 0)
                    {
                        this.frameRate = Double.Parse(config.AppSettings.Settings["FrameRate"].Value, System.Globalization.CultureInfo.InvariantCulture);
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
                            if (!pathList.Exists(delegate (string path)
                            {
                                if (Path.IsPathRooted(path) && !Path.IsPathRooted(character.Script))
                                {
                                    return Path.GetFullPath(character.Script).Equals(path);
                                }
                                else if (!Path.IsPathRooted(path) && Path.IsPathRooted(character.Script))
                                {
                                    return character.Script.Equals(Path.GetFullPath(path));
                                }

                                return character.Script.Equals(path);
                            }))
                            {
                                pathList.Add(character.Script);
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
                    character.Location = new Point(this.Left - (this.Left - character.Location.X - character.BaseLocation.X) - character.BaseLocation.X, this.Top - (this.Top - character.Location.Y - character.BaseLocation.Y) - character.BaseLocation.Y);

                    this.balloon.Left = this.Left + (this.Width - this.balloon.Width) / 2;
                    this.balloon.Top = this.Top - this.balloon.Height + character.Origin.Y * this.ZoomScaleTransform.ScaleY;

                    if (this == Application.Current.MainWindow)
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window != Application.Current.MainWindow)
                            {
                                Agent agent = window as Agent;

                                if (agent != null)
                                {
                                    foreach (Character c in from c in Script.Instance.Characters where c.Name.Equals(agent.characterName) select c)
                                    {
                                        c.Location = new Point(agent.Left - this.Left - c.BaseLocation.X, agent.Top - this.Top - c.BaseLocation.Y);
                                    }
                                }
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
                            character.Location = new Point(this.Left - (agent.Left - c.Location.X - c.BaseLocation.X) - character.BaseLocation.X, this.Top - (agent.Top - c.Location.Y - c.BaseLocation.Y) - character.BaseLocation.Y);

                            this.balloon.Left = this.Left + (this.Width - this.balloon.Width) / 2;
                            this.balloon.Top = this.Top - this.balloon.Height + character.Origin.Y * this.ZoomScaleTransform.ScaleY;
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

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

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (this.mouseDownPosition.HasValue)
            {
                Point point = PointToScreen(e.GetPosition(this));

                foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                {
                    if ((Math.Sign(point.X - this.mouseDownPosition.Value.X) > 0 && !character.Mirror) || (Math.Sign(point.X - this.mouseDownPosition.Value.X) < 0 && character.Mirror))
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
                    else if ((Math.Sign(point.X - this.mouseDownPosition.Value.X) > 0 && character.Mirror) || (Math.Sign(point.X - this.mouseDownPosition.Value.X) < 0 && !character.Mirror))
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

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

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
                            foreach (string fileName in Directory.EnumerateFiles(s, "*.xml", SearchOption.AllDirectories))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    StringBuilder stringBuilder = new StringBuilder(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                                    if (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).LastIndexOf(Path.DirectorySeparatorChar) != Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 1)
                                    {
                                        stringBuilder.Append(Path.DirectorySeparatorChar);
                                    }

                                    string path = fileName.StartsWith(stringBuilder.ToString(), StringComparison.Ordinal) ? fileName.Remove(0, stringBuilder.Length) : fileName;

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
                        else if (Path.GetExtension(s).Equals(".zip", StringComparison.OrdinalIgnoreCase))
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

                                    foreach (ZipArchiveEntry zipArchiveEntry in from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry)
                                    {
                                        Stream stream = null;

                                        try
                                        {
                                            stream = zipArchiveEntry.Open();

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
                System.Configuration.Configuration config = null;
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

                if (Directory.Exists(directory))
                {
                    string fileName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    foreach (string s in from s in Directory.EnumerateFiles(directory, "*.config") where fileName.Equals(Path.GetFileNameWithoutExtension(s)) select s)
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
                                        fs = new FileStream(v.Script, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                                    using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer((string)state))
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
                    Dictionary<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>> tempDictionary = new Dictionary<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>>();
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

                                if (!tempDictionary.ContainsKey(key) && !this.cachedBitmapImageDictionary.ContainsKey(key))
                                {
                                    tempDictionary.Add(key, new KeyValuePair<KeyValuePair<string, string>, MemoryStream>(new KeyValuePair<string, string>(character.Script, path), new MemoryStream()));
                                }
                            }
                        });
                    }

                    Task.Factory.StartNew(delegate
                    {
                        foreach (KeyValuePair<KeyValuePair<string, string>, MemoryStream> kvp in tempDictionary.Values)
                        {
                            if (Path.GetExtension(kvp.Key.Key).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(kvp.Key.Key, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    using (ZipArchive zipArchive = new ZipArchive(fs))
                                    {
                                        fs = null;

                                        ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(kvp.Key.Value);

                                        using (Stream stream = zipArchiveEntry.Open())
                                        {
                                            byte[] buffer = new byte[zipArchiveEntry.Length];
                                            int bytesRead;

                                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                            {
                                                kvp.Value.Write(buffer, 0, bytesRead);
                                            }

                                            kvp.Value.Seek(0, SeekOrigin.Begin);
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
                                using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(kvp.Key.Key), kvp.Key.Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    byte[] buffer = new byte[fs.Length];
                                    int bytesRead;

                                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        kvp.Value.Write(buffer, 0, bytesRead);
                                    }

                                    kvp.Value.Seek(0, SeekOrigin.Begin);
                                }
                            }
                        }
                    }).ContinueWith(delegate
                    {
                        foreach (KeyValuePair<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>> kvp in tempDictionary)
                        {
                            BitmapImage bitmapImage;

                            if (!this.cachedBitmapImageDictionary.TryGetValue(kvp.Key, out bitmapImage))
                            {
                                try
                                {
                                    bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = kvp.Value.Value;
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
                                    kvp.Value.Value.Close();
                                }
                            }
                        }

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
                    Dictionary<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>> tempDictionary = new Dictionary<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>>();
                    HashSet<string> keyHashSet = new HashSet<string>();

                    foreach (Character character in from character in Script.Instance.Characters where character.Name.Equals(this.characterName) select character)
                    {
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

                                if (!tempDictionary.ContainsKey(key) && !this.cachedBitmapImageDictionary.ContainsKey(key))
                                {
                                    tempDictionary.Add(key, new KeyValuePair<KeyValuePair<string, string>, MemoryStream>(new KeyValuePair<string, string>(character.Script, path), new MemoryStream()));
                                }

                                if (!keyHashSet.Contains(key))
                                {
                                    keyHashSet.Add(key);
                                }
                            }
                        });
                    }

                    Task.Factory.StartNew(delegate
                    {
                        foreach (KeyValuePair<KeyValuePair<string, string>, MemoryStream> kvp in tempDictionary.Values)
                        {
                            if (Path.GetExtension(kvp.Key.Key).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                FileStream fs = null;

                                try
                                {
                                    fs = new FileStream(kvp.Key.Key, FileMode.Open, FileAccess.Read, FileShare.Read);

                                    using (ZipArchive zipArchive = new ZipArchive(fs))
                                    {
                                        fs = null;

                                        ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(kvp.Key.Value);

                                        using (Stream stream = zipArchiveEntry.Open())
                                        {
                                            byte[] buffer = new byte[zipArchiveEntry.Length];
                                            int bytesRead;

                                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                            {
                                                kvp.Value.Write(buffer, 0, bytesRead);
                                            }

                                            kvp.Value.Seek(0, SeekOrigin.Begin);
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
                                using (FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(kvp.Key.Key), kvp.Key.Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    byte[] buffer = new byte[fs.Length];
                                    int bytesRead;

                                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        kvp.Value.Write(buffer, 0, bytesRead);
                                    }

                                    kvp.Value.Seek(0, SeekOrigin.Begin);
                                }
                            }
                        }
                    }).ContinueWith(delegate
                    {
                        List<Motion> previousMotionList = new List<Motion>();
                        List<Sprite> cachedSpriteList = new List<Sprite>();
                        double maxFrameRate = 0;
                        Dictionary<Motion, int> skipDictionary = new Dictionary<Motion, int>();
                        Dictionary<Motion, Motion> switchDictionary = new Dictionary<Motion, Motion>();

                        foreach (KeyValuePair<string, KeyValuePair<KeyValuePair<string, string>, MemoryStream>> kvp in tempDictionary)
                        {
                            BitmapImage bitmapImage;

                            if (!this.cachedBitmapImageDictionary.TryGetValue(kvp.Key, out bitmapImage))
                            {
                                try
                                {
                                    bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = kvp.Value.Value;
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
                                    kvp.Value.Value.Close();
                                }
                            }
                        }

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

                                    if (agent != null && agent.Opacity > 0)
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

                        if (agent != null && agent.Opacity > 0)
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
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

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
