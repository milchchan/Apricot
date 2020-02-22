using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for Balloon.xaml
    /// </summary>
    public partial class Balloon : Window
    {
        private readonly double frameRate = 60;
        private readonly double maxScale = 1.1;
        private readonly double baseWidth = 240;
        private readonly double baseHeaderHeight = 20;
        private readonly double baseFooterHeight = 25;
        private readonly int numberOfLines = 10;
        private readonly double lineHeight = 0;
        private readonly int maxHistory = 10;
        private readonly Color backgroundColor = Colors.Black;
        private readonly Color textColor = Colors.White;
        private readonly Color linkColor = Colors.Blue;
        private readonly Brush textBrush = null;
        private readonly Brush linkBrush = null;
        private readonly Size maxMessageSize = new Size(0, 0);
        private DispatcherTimer messageTypeTimer = null;
        private DispatcherTimer waitTimer = null;
        private DispatcherTimer switchTimer = null;
        private TimeSpan lastRenderingTime = TimeSpan.Zero;
        private List<double> frameRateList = null;
        private double sourceScaleX;
        private double sourceScaleY;
        private double targetScaleX;
        private double targetScaleY;
        private double sourceOpacity;
        private double targetOpacity;
        private Nullable<int> historyPoint = null;
        private Nullable<int> nextHistoryPoint = null;
        private Collection<Message> messageCollection = null;
        private StringBuilder messageBuffer = null;
        private int targetMessageLength = 0;
        private int randomMessageLength = 0;
        private System.Collections.ArrayList inlineList = null;
        private Dictionary<int, double> embedColorStepDictionary = null;
        private Dictionary<int, double> embedScrollStepDictionary = null;
        private HashSet<int> embedIsScrollableHashSet = null;
        private Dictionary<int, double> attachmentFadeStepDictionary = null;
        private Dictionary<int, double> attachmentImageLoadingStepDictionary = null;
        private Dictionary<int, double> attachmentImageSlideStepDictionary = null;
        private Dictionary<int, double> attachmentImagePopupStepDictionary = null;
        private Dictionary<int, double> attachmentHighlightStepDictionary = null;
        private Dictionary<int, double> attachmentEnableStepDictionary = null;
        private Dictionary<int, double> attachmentFilterStepDictionary = null;
        private Dictionary<int, double> attachmentScrollStepDictionary = null;
        private HashSet<int> attachmentIsScrollableHashSet = null;
        private Dictionary<int, BitmapImage> attachmentImageDictionary = null;
        private Dictionary<Uri, BitmapImage> imageDictionary = null;
        private HashSet<Uri> imageUriHashSet = null;
        private Dictionary<int, Image> cachedInlineImageDictionary = null;
        private Dictionary<int, Image> cachedAttachmentThumbnailImageDictionary = null;
        private Dictionary<int, Image> cachedAttachmentTextImageDictionary = null;
        private Canvas cachedCounterCanvas = null;
        private Dictionary<int, Image> cachedTitleImageDictionary = null;
        private Dictionary<int, Image> cachedSubtitleImageDictionary = null;
        private Dictionary<int, Image> cachedAuthorImageDictionary = null;
        private Dictionary<int, Image> cachedTagImageDictionary = null;
        private bool isReady = false;
        private bool isPinned = false;
        private Size sourceSize = new Size(0, 0);
        private Size targetSize = new Size(0, 0);
        private double popupStep = 0;
        private double resizeStep = 0;
        private Nullable<double> scrollStep = null;
        private Nullable<double> filterStep = null;
        private Nullable<double> liftStep = null;
        private double closeBlinkStep = 0;
        private double backBlinkStep = 0;
        private double upBlinkStep = 0;
        private double downBlinkStep = 0;
        private double imageBlinkStep = 0;
        private Nullable<double> titleScrollStep = null;
        private Nullable<double> subtitleScrollStep = null;
        private Nullable<double> authorScrollStep = null;
        private Dictionary<int, double> tagScrollStepDictionary = null;
        private bool titleIsScrollable = false;
        private bool subtitleIsScrollable = false;
        private bool authorIsScrollable = false;
        private HashSet<int> tagIsScrollableHashSet = null;
        private Nullable<double> counterScrollStep = null;
        private Nullable<double> inspectorFadeStep = null;
        private double imageLoadStep = 0;
        private double imageSlideStep = 0;
        private double imagePopupStep = 0;
        private bool closeIsHover = false;
        private bool backIsHover = false;
        private bool attachmentsAreHover = false;
        private bool imageIsHover = false;
        private bool titleIsHover = false;
        private bool subtitleIsHover = false;
        private bool authorIsHover = false;
        private Nullable<int> hoverEmbeddedIndex = null;
        private Nullable<int> hoverTagIndex = null;
        private double thresholdScoreStep = 0;
        private bool enableFilter = false;
        private double minScore = 0;
        private double maxScore = 0;
        private Queue<double> thresholdQueue = null;
        private double thresholdScore = 0;
        private double previousThresholdScore = 0;
        private double nextThresholdScore = 0;
        private bool isReversed = false;
        private Queue<double> scrollQueue = null;
        private double sourceScrollPosition = 0;
        private double targetScrollPosition = 0;
        private double scrollIndexStep = 0;
        private bool messageIsScrollable = false;
        private bool upIsHover = false;
        private bool downIsHover = false;
        private Nullable<int> hoverIndex = null;
        private Nullable<int> selectedIndex = null;
        private Nullable<double> selectedPosition = null;
        private Queue<double> selectedPositionQueue = null;
        private Entry inspectorEntry = null;
        private Entry previousInspectorEntry = null;
        private Entry nextInspectorEntry = null;
        private Queue<int> circulationQueue = null;
        private int previousCirculationIndex = 0;
        private int nextCirculationIndex = 0;
        private double circulationStep = 0;
        private Nullable<Point> mouseDownPosition = null;
        private Queue<Uri> imageUriQueue = null;
        private Uri imageUri = null;
        private BitmapImage cachedBitmapImage = null;

        public Collection<Message> Messages
        {
            get
            {
                return this.messageCollection;
            }
        }

        public Balloon()
        {
            InitializeComponent();

            this.frameRateList = new List<double>();
            this.messageCollection = new Collection<Message>();
            this.messageBuffer = new StringBuilder();
            this.inlineList = new System.Collections.ArrayList();
            this.embedColorStepDictionary = new Dictionary<int, double>();
            this.embedScrollStepDictionary = new Dictionary<int, double>();
            this.embedIsScrollableHashSet = new HashSet<int>();
            this.tagScrollStepDictionary = new Dictionary<int, double>();
            this.tagIsScrollableHashSet = new HashSet<int>();
            this.attachmentFadeStepDictionary = new Dictionary<int, double>();
            this.attachmentImageLoadingStepDictionary = new Dictionary<int, double>();
            this.attachmentImageSlideStepDictionary = new Dictionary<int, double>();
            this.attachmentImagePopupStepDictionary = new Dictionary<int, double>();
            this.attachmentHighlightStepDictionary = new Dictionary<int, double>();
            this.attachmentEnableStepDictionary = new Dictionary<int, double>();
            this.attachmentFilterStepDictionary = new Dictionary<int, double>();
            this.attachmentScrollStepDictionary = new Dictionary<int, double>();
            this.attachmentIsScrollableHashSet = new HashSet<int>();
            this.attachmentImageDictionary = new Dictionary<int, BitmapImage>();
            this.cachedInlineImageDictionary = new Dictionary<int, Image>();
            this.cachedAttachmentThumbnailImageDictionary = new Dictionary<int, Image>();
            this.cachedAttachmentTextImageDictionary = new Dictionary<int, Image>();
            this.cachedTitleImageDictionary = new Dictionary<int, Image>();
            this.cachedSubtitleImageDictionary = new Dictionary<int, Image>();
            this.cachedAuthorImageDictionary = new Dictionary<int, Image>();
            this.cachedTagImageDictionary = new Dictionary<int, Image>();
            this.thresholdQueue = new Queue<double>();
            this.scrollQueue = new Queue<double>();
            this.selectedPositionQueue = new Queue<double>();
            this.circulationQueue = new Queue<int>();
            this.imageUriQueue = new Queue<Uri>();
            this.imageDictionary = new Dictionary<Uri, BitmapImage>();
            this.imageUriHashSet = new HashSet<Uri>();
            this.messageTypeTimer = new DispatcherTimer(DispatcherPriority.Normal);
            this.messageTypeTimer.Tick += new EventHandler(this.OnTick);
            this.waitTimer = new DispatcherTimer(DispatcherPriority.Normal);
            this.waitTimer.Tick += new EventHandler(this.OnTick);
            this.switchTimer = new DispatcherTimer(DispatcherPriority.Normal);
            this.switchTimer.Tick += new EventHandler(this.OnTick);
            this.switchTimer.Interval = TimeSpan.FromSeconds(3);

            System.Configuration.Configuration config1 = null;
            string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

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

                if (config1.AppSettings.Settings["FrameRate"] != null && config1.AppSettings.Settings["FrameRate"].Value.Length > 0)
                {
                    this.frameRate = Double.Parse(config1.AppSettings.Settings["FrameRate"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["FontFamily"] != null && config1.AppSettings.Settings["FontFamily"].Value.Length > 0)
                {
                    this.FontFamily = new FontFamily(config1.AppSettings.Settings["FontFamily"].Value);
                }

                if (config1.AppSettings.Settings["FontSize"] != null && config1.AppSettings.Settings["FontSize"].Value.Length > 0)
                {
                    this.FontSize = (double)new FontSizeConverter().ConvertFromString(config1.AppSettings.Settings["FontSize"].Value);
                }

                if (config1.AppSettings.Settings["FontStretch"] != null && config1.AppSettings.Settings["FontStretch"].Value.Length > 0)
                {
                    this.FontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config1.AppSettings.Settings["FontStretch"].Value);
                }

                if (config1.AppSettings.Settings["FontStyle"] != null && config1.AppSettings.Settings["FontStyle"].Value.Length > 0)
                {
                    this.FontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config1.AppSettings.Settings["FontStyle"].Value);
                }

                if (config1.AppSettings.Settings["FontWeight"] != null && config1.AppSettings.Settings["FontWeight"].Value.Length > 0)
                {
                    this.FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config1.AppSettings.Settings["FontWeight"].Value);
                }

                if (config1.AppSettings.Settings["LineLength"] != null && config1.AppSettings.Settings["LineLength"].Value.Length > 0)
                {
                    this.baseWidth = Double.Parse(config1.AppSettings.Settings["LineLength"].Value, CultureInfo.InvariantCulture) + 30;
                }

                if (config1.AppSettings.Settings["LineHeight"] != null && config1.AppSettings.Settings["LineHeight"].Value.Length > 0)
                {
                    this.lineHeight = Double.Parse(config1.AppSettings.Settings["LineHeight"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["BackgroundColor"] != null && config1.AppSettings.Settings["BackgroundColor"].Value.Length > 0)
                {
                    this.backgroundColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["BackgroundColor"].Value);
                }

                SolidColorBrush brush1 = new SolidColorBrush(Color.FromArgb((byte)(this.backgroundColor.A * 75 / 100), this.backgroundColor.R, this.backgroundColor.G, this.backgroundColor.B));

                if (brush1.CanFreeze)
                {
                    brush1.Freeze();
                }

                this.OuterPath.Fill = brush1;

                if (config1.AppSettings.Settings["BackgroundImage"] == null)
                {
                    SolidColorBrush brush2 = new SolidColorBrush(this.backgroundColor);

                    if (brush2.CanFreeze)
                    {
                        brush2.Freeze();
                    }

                    this.InnerPath.Fill = brush2;
                }
                else
                {
                    BitmapImage bi = new BitmapImage();

                    using (FileStream fs = new FileStream(System.IO.Path.IsPathRooted(config1.AppSettings.Settings["BackgroundImage"].Value) ? config1.AppSettings.Settings["BackgroundImage"].Value : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["BackgroundImage"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bi.BeginInit();
                        bi.StreamSource = fs;
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.CreateOptions = BitmapCreateOptions.None;
                        bi.EndInit();
                    }

                    ImageBrush imageBrush = new ImageBrush(bi);

                    imageBrush.TileMode = TileMode.Tile;
                    imageBrush.ViewportUnits = BrushMappingMode.Absolute;
                    imageBrush.Viewport = new Rect(0, 0, bi.Width, bi.Height);
                    imageBrush.Stretch = Stretch.None;

                    if (imageBrush.CanFreeze)
                    {
                        imageBrush.Freeze();
                    }

                    this.InnerPath.Fill = imageBrush;
                }

                if (config1.AppSettings.Settings["TextColor"] != null && config1.AppSettings.Settings["TextColor"].Value.Length > 0)
                {
                    this.textColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["TextColor"].Value);
                    this.textBrush = new SolidColorBrush(this.textColor);
                }
                else
                {
                    this.textBrush = new SolidColorBrush(this.textColor);
                }

                if (this.textBrush.CanFreeze)
                {
                    this.textBrush.Freeze();
                }

                if (config1.AppSettings.Settings["LinkColor"] != null && config1.AppSettings.Settings["LinkColor"].Value.Length > 0)
                {
                    this.linkColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["LinkColor"].Value);
                    this.linkBrush = new SolidColorBrush(this.linkColor);
                }
                else
                {
                    this.linkBrush = new SolidColorBrush(this.linkColor);
                }

                if (this.linkBrush.CanFreeze)
                {
                    this.linkBrush.Freeze();
                }
            }
            else
            {
                System.Configuration.Configuration config2 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                if (config1.AppSettings.Settings["FrameRate"] == null)
                {
                    if (config2.AppSettings.Settings["FrameRate"] != null && config2.AppSettings.Settings["FrameRate"].Value.Length > 0)
                    {
                        this.frameRate = Double.Parse(config2.AppSettings.Settings["FrameRate"].Value, CultureInfo.InvariantCulture);
                    }
                }
                else if (config1.AppSettings.Settings["FrameRate"].Value.Length > 0)
                {
                    this.frameRate = Double.Parse(config1.AppSettings.Settings["FrameRate"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["FontFamily"] == null)
                {
                    if (config2.AppSettings.Settings["FontFamily"] != null && config2.AppSettings.Settings["FontFamily"].Value.Length > 0)
                    {
                        this.FontFamily = new FontFamily(config2.AppSettings.Settings["FontFamily"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["FontFamily"].Value.Length > 0)
                {
                    this.FontFamily = new FontFamily(config1.AppSettings.Settings["FontFamily"].Value);
                }

                if (config1.AppSettings.Settings["FontSize"] == null)
                {
                    if (config2.AppSettings.Settings["FontSize"] != null && config2.AppSettings.Settings["FontSize"].Value.Length > 0)
                    {
                        this.FontSize = (double)new FontSizeConverter().ConvertFromString(config2.AppSettings.Settings["FontSize"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["FontSize"].Value.Length > 0)
                {
                    this.FontSize = (double)new FontSizeConverter().ConvertFromString(config1.AppSettings.Settings["FontSize"].Value);
                }

                if (config1.AppSettings.Settings["FontStretch"] == null)
                {
                    if (config2.AppSettings.Settings["FontStretch"] != null && config2.AppSettings.Settings["FontStretch"].Value.Length > 0)
                    {
                        this.FontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config2.AppSettings.Settings["FontStretch"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["FontStretch"].Value.Length > 0)
                {
                    this.FontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(config1.AppSettings.Settings["FontStretch"].Value);
                }

                if (config1.AppSettings.Settings["FontStyle"] == null)
                {
                    if (config2.AppSettings.Settings["FontStyle"] != null && config2.AppSettings.Settings["FontStyle"].Value.Length > 0)
                    {
                        this.FontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config2.AppSettings.Settings["FontStyle"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["FontStyle"].Value.Length > 0)
                {
                    this.FontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(config1.AppSettings.Settings["FontStyle"].Value);
                }

                if (config1.AppSettings.Settings["FontWeight"] == null)
                {
                    if (config2.AppSettings.Settings["FontWeight"] != null && config2.AppSettings.Settings["FontWeight"].Value.Length > 0)
                    {
                        this.FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config2.AppSettings.Settings["FontWeight"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["FontWeight"].Value.Length > 0)
                {
                    this.FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(config1.AppSettings.Settings["FontWeight"].Value);
                }

                if (config1.AppSettings.Settings["LineLength"] == null)
                {
                    if (config2.AppSettings.Settings["LineLength"] != null && config2.AppSettings.Settings["LineLength"].Value.Length > 0)
                    {
                        this.baseWidth = Double.Parse(config2.AppSettings.Settings["LineLength"].Value, CultureInfo.InvariantCulture) + 30;
                    }
                }
                else if (config1.AppSettings.Settings["LineLength"].Value.Length > 0)
                {
                    this.baseWidth = Double.Parse(config1.AppSettings.Settings["LineLength"].Value, CultureInfo.InvariantCulture) + 30;
                }

                if (config1.AppSettings.Settings["LineHeight"] == null)
                {
                    if (config2.AppSettings.Settings["LineHeight"] != null && config2.AppSettings.Settings["LineHeight"].Value.Length > 0)
                    {
                        this.lineHeight = Double.Parse(config2.AppSettings.Settings["LineHeight"].Value, CultureInfo.InvariantCulture);
                    }
                }
                else if (config1.AppSettings.Settings["LineHeight"].Value.Length > 0)
                {
                    this.lineHeight = Double.Parse(config1.AppSettings.Settings["LineHeight"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["BackgroundColor"] == null)
                {
                    if (config2.AppSettings.Settings["BackgroundColor"] != null && config2.AppSettings.Settings["BackgroundColor"].Value.Length > 0)
                    {
                        this.backgroundColor = (Color)ColorConverter.ConvertFromString(config2.AppSettings.Settings["BackgroundColor"].Value);
                    }
                }
                else if (config1.AppSettings.Settings["BackgroundColor"].Value.Length > 0)
                {
                    this.backgroundColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["BackgroundColor"].Value);
                }

                SolidColorBrush brush1 = new SolidColorBrush(Color.FromArgb((byte)(this.backgroundColor.A * 75 / 100), this.backgroundColor.R, this.backgroundColor.G, this.backgroundColor.B));

                if (brush1.CanFreeze)
                {
                    brush1.Freeze();
                }

                this.OuterPath.Fill = brush1;

                if (config1.AppSettings.Settings["BackgroundImage"] == null)
                {
                    if (config2.AppSettings.Settings["BackgroundImage"] == null)
                    {
                        SolidColorBrush brush2 = new SolidColorBrush(this.backgroundColor);

                        if (brush2.CanFreeze)
                        {
                            brush2.Freeze();
                        }

                        this.InnerPath.Fill = brush2;
                    }
                    else
                    {
                        BitmapImage bi = new BitmapImage();

                        if (System.IO.Path.IsPathRooted(config2.AppSettings.Settings["BackgroundImage"].Value))
                        {
                            using (FileStream fs = new FileStream(config2.AppSettings.Settings["BackgroundImage"].Value, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                bi.BeginInit();
                                bi.StreamSource = fs;
                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                bi.CreateOptions = BitmapCreateOptions.None;
                                bi.EndInit();
                            }
                        }
                        else
                        {
                            string path = System.IO.Path.Combine(directory, config2.AppSettings.Settings["BackgroundImage"].Value);

                            using (FileStream fs = new FileStream(File.Exists(path) ? path : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config2.AppSettings.Settings["BackgroundImage"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                bi.BeginInit();
                                bi.StreamSource = fs;
                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                bi.CreateOptions = BitmapCreateOptions.None;
                                bi.EndInit();
                            }
                        }

                        ImageBrush imageBrush = new ImageBrush(bi);

                        imageBrush.TileMode = TileMode.Tile;
                        imageBrush.ViewportUnits = BrushMappingMode.Absolute;
                        imageBrush.Viewport = new Rect(0, 0, bi.Width, bi.Height);
                        imageBrush.Stretch = Stretch.None;

                        if (imageBrush.CanFreeze)
                        {
                            imageBrush.Freeze();
                        }

                        this.InnerPath.Fill = imageBrush;
                    }
                }
                else
                {
                    BitmapImage bi = new BitmapImage();

                    if (System.IO.Path.IsPathRooted(config1.AppSettings.Settings["BackgroundImage"].Value))
                    {
                        using (FileStream fs = new FileStream(config1.AppSettings.Settings["BackgroundImage"].Value, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            bi.BeginInit();
                            bi.StreamSource = fs;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.CreateOptions = BitmapCreateOptions.None;
                            bi.EndInit();
                        }
                    }
                    else
                    {
                        string path = System.IO.Path.Combine(directory, config1.AppSettings.Settings["BackgroundImage"].Value);

                        using (FileStream fs = new FileStream(File.Exists(path) ? path : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["BackgroundImage"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            bi.BeginInit();
                            bi.StreamSource = fs;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.CreateOptions = BitmapCreateOptions.None;
                            bi.EndInit();
                        }
                    }

                    ImageBrush imageBrush = new ImageBrush(bi);

                    imageBrush.TileMode = TileMode.Tile;
                    imageBrush.ViewportUnits = BrushMappingMode.Absolute;
                    imageBrush.Viewport = new Rect(0, 0, bi.Width, bi.Height);
                    imageBrush.Stretch = Stretch.None;

                    if (imageBrush.CanFreeze)
                    {
                        imageBrush.Freeze();
                    }

                    this.InnerPath.Fill = imageBrush;
                }

                if (config1.AppSettings.Settings["TextColor"] == null)
                {
                    if (config2.AppSettings.Settings["TextColor"] != null && config2.AppSettings.Settings["TextColor"].Value.Length > 0)
                    {
                        this.textColor = (Color)ColorConverter.ConvertFromString(config2.AppSettings.Settings["TextColor"].Value);
                        this.textBrush = new SolidColorBrush(this.textColor);
                    }
                    else
                    {
                        this.textBrush = new SolidColorBrush(this.textColor);
                    }
                }
                else if (config1.AppSettings.Settings["TextColor"].Value.Length > 0)
                {
                    this.textColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["TextColor"].Value);
                    this.textBrush = new SolidColorBrush(this.textColor);
                }
                else
                {
                    this.textBrush = new SolidColorBrush(this.textColor);
                }

                if (this.textBrush.CanFreeze)
                {
                    this.textBrush.Freeze();
                }

                if (config1.AppSettings.Settings["LinkColor"] == null)
                {
                    if (config2.AppSettings.Settings["LinkColor"] != null && config2.AppSettings.Settings["LinkColor"].Value.Length > 0)
                    {
                        this.linkColor = (Color)ColorConverter.ConvertFromString(config2.AppSettings.Settings["LinkColor"].Value);
                        this.linkBrush = new SolidColorBrush(this.linkColor);
                    }
                    else
                    {
                        this.linkBrush = new SolidColorBrush(this.linkColor);
                    }
                }
                else if (config1.AppSettings.Settings["LinkColor"].Value.Length > 0)
                {
                    this.linkColor = (Color)ColorConverter.ConvertFromString(config1.AppSettings.Settings["LinkColor"].Value);
                    this.linkBrush = new SolidColorBrush(this.linkColor);
                }
                else
                {
                    this.linkBrush = new SolidColorBrush(this.linkColor);
                }

                if (this.linkBrush.CanFreeze)
                {
                    this.linkBrush.Freeze();
                }
            }

            this.maxMessageSize = new Size(this.baseWidth - 30, this.lineHeight * this.numberOfLines);

            Canvas.SetTop(this.FilterImage, this.baseHeaderHeight + 2);
            Canvas.SetLeft(this.ScrollCanvas, 10);
            Canvas.SetTop(this.ScrollCanvas, this.baseHeaderHeight);
            Canvas.SetTop(this.CloseImage, 8);
            Canvas.SetTop(this.BackImage, 7);

            CompositionTarget.Rendering += new EventHandler(this.OnRendering);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PresentationSource presentationSource = PresentationSource.FromVisual(this);

            if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
            {
                RenderOptions.SetEdgeMode(this.FilterImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.ScrollImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.CloseImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.BackImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.UpImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.DownImage, EdgeMode.Aliased);
                RenderOptions.SetEdgeMode(this.InspectorImage, EdgeMode.Aliased);
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                this.lastRenderingTime = TimeSpan.Zero;
                this.frameRateList.Clear();
                this.historyPoint = null;
                this.nextHistoryPoint = null;
                this.isReady = false;
                this.isPinned = false;
                this.closeIsHover = false;
                this.backIsHover = false;
                this.upIsHover = false;
                this.downIsHover = false;
                this.attachmentsAreHover = false;
                this.titleIsHover = false;
                this.subtitleIsHover = false;
                this.imageIsHover = false;
                this.authorIsHover = false;
                this.hoverEmbeddedIndex = null;
                this.hoverTagIndex = null;
                this.messageIsScrollable = false;
                this.enableFilter = false;
                this.minScore = 0;
                this.maxScore = 0;
                this.thresholdQueue.Clear();
                this.thresholdScore = 0;
                this.previousThresholdScore = 0;
                this.nextThresholdScore = 0;
                this.thresholdScoreStep = 0;
                this.isReversed = false;
                this.scrollQueue.Clear();
                this.selectedPositionQueue.Clear();
                this.sourceScrollPosition = 0;
                this.targetScrollPosition = 0;
                this.scrollIndexStep = 0;
                this.hoverIndex = null;
                this.selectedIndex = null;
                this.selectedPosition = null;
                this.sourceScaleX = this.sourceScaleY = 0;
                this.targetScaleX = this.targetScaleY = 0;
                this.popupStep = 0;
                this.resizeStep = 0;
                this.scrollStep = null;
                this.filterStep = null;
                this.liftStep = null;
                this.closeBlinkStep = 0;
                this.backBlinkStep = 0;
                this.upBlinkStep = 0;
                this.downBlinkStep = 0;
                this.imageBlinkStep = 0;
                this.titleScrollStep = null;
                this.subtitleScrollStep = null;
                this.authorScrollStep = null;
                this.tagScrollStepDictionary.Clear();
                this.titleIsScrollable = false;
                this.subtitleIsScrollable = false;
                this.authorIsScrollable = false;
                this.tagIsScrollableHashSet.Clear();
                this.counterScrollStep = null;
                this.inspectorFadeStep = null;
                this.imageLoadStep = 0;
                this.imageSlideStep = 0;
                this.imagePopupStep = 0;
                this.sourceOpacity = 0;
                this.targetOpacity = 0;
                this.inspectorEntry = null;
                this.previousInspectorEntry = null;
                this.nextInspectorEntry = null;
                this.circulationQueue.Clear();
                this.previousCirculationIndex = 0;
                this.nextCirculationIndex = 0;
                this.circulationStep = 0;
                this.mouseDownPosition = null;
                this.imageUriQueue.Clear();
                this.imageUri = null;
                this.cachedBitmapImage = null;
                this.messageBuffer = new StringBuilder();
                this.targetMessageLength = 0;
                this.randomMessageLength = 0;
                this.inlineList.Clear();
                this.embedColorStepDictionary.Clear();
                this.embedScrollStepDictionary.Clear();
                this.embedIsScrollableHashSet.Clear();
                this.attachmentFadeStepDictionary.Clear();
                this.attachmentImageLoadingStepDictionary.Clear();
                this.attachmentImageSlideStepDictionary.Clear();
                this.attachmentImagePopupStepDictionary.Clear();
                this.attachmentHighlightStepDictionary.Clear();
                this.attachmentEnableStepDictionary.Clear();
                this.attachmentFilterStepDictionary.Clear();
                this.attachmentScrollStepDictionary.Clear();
                this.attachmentIsScrollableHashSet.Clear();
                this.attachmentImageDictionary.Clear();
                this.cachedInlineImageDictionary.Clear();
                this.cachedAttachmentThumbnailImageDictionary.Clear();
                this.cachedAttachmentTextImageDictionary.Clear();
                this.cachedCounterCanvas = null;
                this.cachedTitleImageDictionary.Clear();
                this.cachedSubtitleImageDictionary.Clear();
                this.cachedAuthorImageDictionary.Clear();
                this.cachedTagImageDictionary.Clear();
                this.imageDictionary.Clear();
                this.imageUriHashSet.Clear();
                this.Width = this.Height = 0;
                this.Canvas.Width = this.Canvas.Height = Double.NaN;
                this.Canvas.Opacity = 0;
                this.ScaleTransform.ScaleX = this.ScaleTransform.ScaleY = 0;
                this.TranslateTransform.X = this.TranslateTransform.Y = 0;
                this.OuterPath.Width = this.OuterPath.Height = Double.NaN;
                this.OuterPath.Data = null;
                this.InnerPath.Width = this.InnerPath.Height = Double.NaN;
                this.InnerPath.Data = null;
                this.OverlayPath.Width = this.OverlayPath.Height = Double.NaN;
                this.OverlayPath.Data = null;
                this.OverlayPath.Fill = null;
                this.HighlightPath.Width = this.HighlightPath.Height = Double.NaN;
                this.HighlightPath.Data = null;
                this.HighlightLinePath.Width = this.HighlightLinePath.Height = Double.NaN;
                this.HighlightLinePath.Data = null;
                this.ScrollCanvas.Width = this.ScrollCanvas.Height = Double.NaN;
                this.MessageCanvas.Width = this.MessageCanvas.Height = Double.NaN;
                this.MessageCanvas.Children.Clear();
                this.InspectorCanvas.Visibility = Visibility.Collapsed;
                this.InspectorCanvas.Width = this.InspectorCanvas.Height = Double.NaN;
                this.InspectorCanvas.Background = null;
                this.InspectorCanvas.Opacity = 0;
                this.InspectorCanvas.Children.Clear();
                this.FilterImage.Visibility = Visibility.Collapsed;
                this.FilterImage.Width = this.FilterImage.Height = Double.NaN;
                this.FilterImage.Source = null;
                this.FilterImage.Opacity = 0;
                this.ScrollImage.Visibility = Visibility.Collapsed;
                this.ScrollImage.Width = this.ScrollImage.Height = Double.NaN;
                this.ScrollImage.Source = null;
                this.ScrollImage.Opacity = 0;
                this.CloseImage.Width = this.CloseImage.Height = Double.NaN;
                this.CloseImage.Source = null;
                this.BackImage.Width = this.BackImage.Height = Double.NaN;
                this.BackImage.Source = null;
                this.UpImage.Visibility = Visibility.Collapsed;
                this.UpImage.Width = this.UpImage.Height = Double.NaN;
                this.UpImage.Source = null;
                this.UpImage.Opacity = 0;
                this.DownImage.Visibility = Visibility.Collapsed;
                this.DownImage.Width = this.DownImage.Height = Double.NaN;
                this.DownImage.Source = null;
                this.DownImage.Opacity = 0;
                this.InspectorImage.Visibility = Visibility.Collapsed;
                this.InspectorImage.Width = this.InspectorImage.Height = Double.NaN;
                this.InspectorImage.Source = null;
                this.InspectorImage.Opacity = 0;
                this.waitTimer.Stop();
                this.messageTypeTimer.Stop();
                this.switchTimer.Stop();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Up)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.enableFilter)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];

                        if (message.HasAttachments)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, 1, true);

                            if (this.nextThresholdScore - interval < this.minScore)
                            {
                                this.thresholdQueue.Enqueue(this.minScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.minScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(-interval);
                                this.nextThresholdScore -= interval;
                            }
                        }
                    }
                    else
                    {
                        if (this.selectedPosition.HasValue)
                        {
                            this.selectedPositionQueue.Enqueue(-1);
                        }
                        else
                        {
                            this.scrollQueue.Enqueue(-1);
                        }
                    }
                }
            }
            else if (e.Key == Key.Down)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.enableFilter)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];

                        if (message.HasAttachments)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, 1, false);

                            if (this.nextThresholdScore + interval > this.maxScore)
                            {
                                this.thresholdQueue.Enqueue(this.maxScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.maxScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(interval);
                                this.nextThresholdScore += interval;
                            }
                        }
                    }
                    else
                    {
                        if (this.selectedPosition.HasValue)
                        {
                            this.selectedPositionQueue.Enqueue(1);
                        }
                        else
                        {
                            this.scrollQueue.Enqueue(1);
                        }
                    }
                }
            }
            else if (e.Key == Key.PageUp)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.selectedPosition.HasValue)
                    {
                        if (this.targetScrollPosition > 0 && this.targetScrollPosition < this.numberOfLines)
                        {
                            this.selectedPositionQueue.Enqueue(-this.targetScrollPosition);
                        }
                        else
                        {
                            this.selectedPositionQueue.Enqueue(-this.numberOfLines);
                        }
                    }
                    else
                    {
                        this.scrollQueue.Enqueue(-this.numberOfLines);
                    }
                }
            }
            else if (e.Key == Key.PageDown || e.Key == Key.Space)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    double lines = this.numberOfLines;

                    if (this.selectedPosition.HasValue)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];

                        if (message.Attachments.Count > (this.selectedPosition.Value + this.numberOfLines) && message.Attachments.Count - (this.selectedPosition.Value + this.numberOfLines) < this.numberOfLines)
                        {
                            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                            double x = 0;
                            int messageLines = 0;
                            bool isBreaked = true;
                            bool isReseted = true;

                            foreach (object o in message)
                            {
                                string inline = o as string;
                                Brush brush = this.textBrush;
                                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                StringBuilder lineStringBuilder = new StringBuilder();

                                if (inline == null)
                                {
                                    Entry entry = o as Entry;

                                    if (entry == null)
                                    {
                                        inline = o.ToString();
                                    }
                                    else
                                    {
                                        inline = entry.Title;
                                        brush = this.linkBrush;
                                    }
                                }

                                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                {
                                    dictionary.Add(match.Index, match.Length);
                                }

                                for (int i = 0; i < inline.Length; i++)
                                {
                                    int length;

                                    if (dictionary.TryGetValue(i, out length) && x + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width && !isReseted)
                                    {
                                        lineStringBuilder.Clear();
                                        x = 0;
                                        messageLines++;
                                        isBreaked = true;
                                    }

                                    lineStringBuilder.Append(inline[i]);

                                    if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                    {
                                        lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                        if (lineStringBuilder.Length > 0)
                                        {
                                            lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                        }

                                        x = 0;
                                        messageLines++;
                                        isBreaked = true;
                                        isReseted = true;
                                    }
                                    else if (x + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width)
                                    {
                                        if (lineStringBuilder.Length - 1 > 0)
                                        {
                                            lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                        }

                                        x = 0;
                                        messageLines++;
                                        isBreaked = true;
                                        isReseted = true;
                                    }
                                    else
                                    {
                                        isReseted = false;
                                    }
                                }

                                if (lineStringBuilder.Length > 0)
                                {
                                    x += Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                                    isBreaked = false;
                                    isReseted = false;
                                }
                            }

                            if (isBreaked)
                            {
                                messageLines++;
                            }

                            lines = messageLines + this.selectedPosition.Value - this.targetScrollPosition + message.Attachments.Count - (this.selectedPosition.Value + this.numberOfLines);
                        }

                        this.selectedPositionQueue.Enqueue(lines);
                    }
                    else
                    {
                        this.scrollQueue.Enqueue(lines);
                    }
                }
            }
            else if (e.Key == Key.Home)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.selectedPosition.HasValue)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];
                        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                        double x = 0;
                        int messageLines = 0;
                        bool isBreaked = true;
                        bool isReseted = true;

                        foreach (object o in message)
                        {
                            string inline = o as string;
                            Brush brush = this.textBrush;
                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                            StringBuilder lineStringBuilder = new StringBuilder();

                            if (inline == null)
                            {
                                Entry entry = o as Entry;

                                if (entry == null)
                                {
                                    inline = o.ToString();
                                }
                                else
                                {
                                    inline = entry.Title;
                                    brush = this.linkBrush;
                                }
                            }

                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                            {
                                dictionary.Add(match.Index, match.Length);
                            }

                            for (int i = 0; i < inline.Length; i++)
                            {
                                int length;

                                if (dictionary.TryGetValue(i, out length) && x + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width && !isReseted)
                                {
                                    lineStringBuilder.Clear();
                                    x = 0;
                                    messageLines++;
                                    isBreaked = true;
                                }

                                lineStringBuilder.Append(inline[i]);

                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                {
                                    lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                    if (lineStringBuilder.Length > 0)
                                    {
                                        lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                    }

                                    x = 0;
                                    messageLines++;
                                    isBreaked = true;
                                    isReseted = true;
                                }
                                else if (x + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width)
                                {
                                    if (lineStringBuilder.Length - 1 > 0)
                                    {
                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                    }

                                    x = 0;
                                    messageLines++;
                                    isBreaked = true;
                                    isReseted = true;
                                }
                                else
                                {
                                    isReseted = false;
                                }
                            }

                            if (lineStringBuilder.Length > 0)
                            {
                                x += Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                                isBreaked = false;
                                isReseted = false;
                            }
                        }

                        if (isBreaked)
                        {
                            messageLines++;
                        }

                        this.selectedPositionQueue.Enqueue(-(messageLines + this.selectedPosition.Value));
                    }
                    else
                    {
                        this.scrollQueue.Enqueue(-this.targetScrollPosition);
                    }
                }
            }
            else if (e.Key == Key.End)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    Message message = this.messageCollection[this.historyPoint.Value];
                    double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                    double x = 0;
                    int messageLines = 0;
                    bool isBreaked = true;
                    bool isReseted = true;

                    foreach (object o in message)
                    {
                        string inline = o as string;
                        Brush brush = this.textBrush;
                        Dictionary<int, int> dictionary = new Dictionary<int, int>();
                        StringBuilder lineStringBuilder = new StringBuilder();

                        if (inline == null)
                        {
                            Entry entry = o as Entry;

                            if (entry == null)
                            {
                                inline = o.ToString();
                            }
                            else
                            {
                                inline = entry.Title;
                                brush = this.linkBrush;
                            }
                        }

                        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                        {
                            dictionary.Add(match.Index, match.Length);
                        }

                        for (int i = 0; i < inline.Length; i++)
                        {
                            int length;

                            if (dictionary.TryGetValue(i, out length) && x + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width && !isReseted)
                            {
                                lineStringBuilder.Clear();
                                x = 0;
                                messageLines++;
                                isBreaked = true;
                            }

                            lineStringBuilder.Append(inline[i]);

                            if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                            {
                                lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                if (lineStringBuilder.Length > 0)
                                {
                                    lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                }

                                x = 0;
                                messageLines++;
                                isBreaked = true;
                                isReseted = true;
                            }
                            else
                            {
                                if (x + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width)
                                {
                                    if (lineStringBuilder.Length - 1 > 0)
                                    {
                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                    }

                                    x = 0;
                                    messageLines++;
                                    isBreaked = true;
                                    isReseted = true;
                                }
                                else
                                {
                                    isReseted = false;
                                }
                            }
                        }

                        if (lineStringBuilder.Length > 0)
                        {
                            x += Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                            isBreaked = false;
                            isReseted = false;
                        }
                    }

                    if (isBreaked)
                    {
                        messageLines++;
                    }

                    if (this.selectedPosition.HasValue)
                    {
                        this.selectedPositionQueue.Enqueue(messageLines + message.Attachments.Count - this.selectedPosition.Value);
                    }
                    else
                    {
                        this.scrollQueue.Enqueue(messageLines + message.Attachments.Count - this.targetScrollPosition);
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                this.sourceOpacity = this.Canvas.Opacity;
                this.targetOpacity = 0;
                this.sourceScaleX = this.ScaleTransform.ScaleX;
                this.sourceScaleY = this.ScaleTransform.ScaleY;
                this.targetScaleX = this.targetScaleY = 0;
                this.popupStep = 0;
            }
            else if (e.Key == Key.Back)
            {
                Back();
            }
            else if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift) && !this.enableFilter)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    Message message = this.messageCollection[this.historyPoint.Value];
                    var query = from entry in message.Attachments where entry.Score.HasValue select entry.Score.Value;

                    if (query.Any())
                    {
                        List<double> scoreList = query.ToList();

                        this.enableFilter = false;
                        this.maxScore = scoreList.Max<double>();
                        this.minScore = scoreList.Min<double>();
                        this.isReversed = false;

                        for (int i = 0; i < scoreList.Count - 1; i++)
                        {
                            if (scoreList[i] != scoreList[i + 1])
                            {
                                this.enableFilter = true;

                                break;
                            }
                        }

                        if (this.enableFilter)
                        {
                            int counter = 0;

                            for (int i = 0; i < scoreList.Count - 1; i++)
                            {
                                if (scoreList[i] >= scoreList[i + 1])
                                {
                                    counter++;
                                }
                            }

                            if (counter == scoreList.Count - 1)
                            {
                                this.isReversed = true;
                            }
                        }

                        if (this.thresholdScore > this.maxScore || this.thresholdScore < this.minScore)
                        {
                            if (this.isReversed)
                            {
                                this.thresholdScore = this.minScore;
                            }
                            else
                            {
                                this.thresholdScore = this.maxScore;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != (ModifierKeys.Control | ModifierKeys.Shift) && this.enableFilter)
            {
                this.enableFilter = false;
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender == this.CloseImage)
            {
                this.closeIsHover = true;
            }
            else if (sender == this.BackImage)
            {
                this.backIsHover = true;
            }
            else if (sender == this.UpImage)
            {
                this.upIsHover = true;
            }
            else if (sender == this.DownImage)
            {
                this.downIsHover = true;
            }
            else if (sender == this.MessageCanvas)
            {
                this.attachmentsAreHover = true;
            }
            else if (sender == this.InspectorImage)
            {
                this.imageIsHover = true;

                if (this.switchTimer.IsEnabled && this.inspectorEntry != null)
                {
                    bool stopRequired = true;

                    if (!this.inspectorEntry.HasMultipleImages)
                    {
                        const double space = 10;
                        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                        double x = 0;

                        foreach (string tag in this.inspectorEntry.Tags)
                        {
                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                            StringBuilder lineStringBuilder = new StringBuilder();

                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(tag, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                            {
                                dictionary.Add(match.Index, match.Length);
                            }

                            if (x + space + Math.Ceiling(new FormattedText(tag, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && x != 0)
                            {
                                stopRequired = false;

                                break;
                            }

                            for (int i = 0; i < tag.Length; i++)
                            {
                                int length;

                                if (dictionary.TryGetValue(i, out length) && x + space + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), tag.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && lineStringBuilder.Length > 0)
                                {
                                    stopRequired = false;

                                    break;
                                }

                                lineStringBuilder.Append(tag[i]);

                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                {
                                    stopRequired = false;

                                    break;
                                }
                                else if (lineStringBuilder.Length > 0 && x + space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74)
                                {
                                    stopRequired = false;

                                    break;
                                }
                            }

                            if (!stopRequired)
                            {
                                break;
                            }

                            if (lineStringBuilder.Length > 0)
                            {
                                x += space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                            }
                        }
                    }

                    if (stopRequired)
                    {
                        this.switchTimer.Stop();
                    }
                }
            }
            else
            {
                this.isPinned = true;
                this.waitTimer.Stop();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender == this.CloseImage)
            {
                this.closeIsHover = false;
            }
            else if (sender == this.BackImage)
            {
                this.backIsHover = false;
            }
            else if (sender == this.UpImage)
            {
                this.upIsHover = false;
            }
            else if (sender == this.DownImage)
            {
                this.downIsHover = false;
            }
            else if (sender == this.MessageCanvas)
            {
                this.attachmentsAreHover = false;
            }
            else if (sender == this.InspectorImage)
            {
                this.imageIsHover = false;

                if (!this.switchTimer.IsEnabled && this.inspectorEntry != null && this.inspectorEntry.HasMultipleImages)
                {
                    this.switchTimer.Start();
                }
            }
            else
            {
                this.isPinned = false;

                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    this.waitTimer.Interval = this.messageCollection[this.historyPoint.Value].Duration;
                }
                else
                {
                    this.waitTimer.Interval = TimeSpan.Zero;
                }

                this.waitTimer.Start();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.OriginalSource == this.UpImage)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.enableFilter)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];

                        if (message.HasAttachments)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, 1, true);

                            if (this.nextThresholdScore - interval < this.minScore)
                            {
                                this.thresholdQueue.Enqueue(this.minScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.minScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(-interval);
                                this.nextThresholdScore -= interval;
                            }
                        }
                    }
                    else
                    {
                        if (this.selectedPosition.HasValue)
                        {
                            this.selectedPositionQueue.Enqueue(-1);
                        }
                        else
                        {
                            this.scrollQueue.Enqueue(-1);
                        }
                    }
                }
            }
            else if (e.OriginalSource == this.DownImage)
            {
                if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                {
                    if (this.enableFilter)
                    {
                        Message message = this.messageCollection[this.historyPoint.Value];

                        if (message.HasAttachments)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, 1, false);

                            if (this.nextThresholdScore + interval > this.maxScore)
                            {
                                this.thresholdQueue.Enqueue(this.maxScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.maxScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(interval);
                                this.nextThresholdScore += interval;
                            }
                        }
                    }
                    else
                    {
                        if (this.selectedPosition.HasValue)
                        {
                            this.selectedPositionQueue.Enqueue(1);
                        }
                        else
                        {
                            this.scrollQueue.Enqueue(1);
                        }
                    }
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.OriginalSource == this.InspectorImage)
            {
                if (this.inspectorEntry.Image != null)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && this.inspectorEntry.HasMultipleImages)
                    {
                        this.inspectorEntry.NextImage();
                    }

                    if (!this.imageUriHashSet.Contains(this.inspectorEntry.Image))
                    {
                        this.imageUriHashSet.Add(this.inspectorEntry.Image);
                    }

                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        UpdateImage(this.inspectorEntry.Image, true);
                    }
                    else
                    {
                        UpdateImage(this.inspectorEntry.Image, false);
                    }

                    this.imageUriQueue.Enqueue(this.inspectorEntry.Image);
                }
            }
            else if (e.OriginalSource == this.CloseImage)
            {
                this.sourceOpacity = this.Canvas.Opacity;
                this.targetOpacity = 0;
                this.sourceScaleX = this.ScaleTransform.ScaleX;
                this.sourceScaleY = this.ScaleTransform.ScaleY;
                this.targetScaleX = this.targetScaleY = 0;
                this.popupStep = 0;
                this.waitTimer.Stop();
            }
            else if (e.OriginalSource == this.BackImage)
            {
                Back();
            }
            else if (e.OriginalSource != this.InspectorCanvas && e.OriginalSource != this.UpImage && e.OriginalSource != this.DownImage)
            {
                this.selectedPosition = this.selectedIndex = null;
                this.previousInspectorEntry = this.inspectorEntry;
                this.nextInspectorEntry = null;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            const int WHEEL_DATA = 120;
            int lines = e.Delta * SystemParameters.WheelScrollLines / WHEEL_DATA;

            if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
            {
                if (this.enableFilter)
                {
                    Message message = this.messageCollection[this.historyPoint.Value];

                    if (message.HasAttachments)
                    {
                        if (e.Delta > 0)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, lines, true);

                            if (this.nextThresholdScore - interval < this.minScore)
                            {
                                this.thresholdQueue.Enqueue(this.minScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.minScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(-interval);
                                this.nextThresholdScore -= interval;
                            }
                        }
                        else if (e.Delta < 0)
                        {
                            double interval = GetNextInterval(message.Attachments, this.nextThresholdScore, lines, false);

                            if (this.nextThresholdScore + interval > this.maxScore)
                            {
                                this.thresholdQueue.Enqueue(this.maxScore - this.nextThresholdScore);
                                this.nextThresholdScore = this.maxScore;
                            }
                            else
                            {
                                this.thresholdQueue.Enqueue(interval);
                                this.nextThresholdScore += interval;
                            }
                        }
                    }
                }
                else
                {
                    if (this.selectedPosition.HasValue)
                    {
                        this.selectedPositionQueue.Enqueue(-lines);
                    }
                    else
                    {
                        this.scrollQueue.Enqueue(-lines);
                    }
                }
            }
        }

        private void OnManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Handled = true;
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
            {
                this.scrollQueue.Enqueue(-e.DeltaManipulation.Translation.Y / this.lineHeight);
            }

            e.Handled = true;
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.TotalManipulation.Translation.Y == 0)
            {
                e.Cancel();
            }

            e.Handled = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.Owner != null)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        public void Show(Message message)
        {
            var query = from entry in message.Attachments where entry.Score.HasValue select entry.Score.Value;
            bool isRecyclable = true;

            if (query.Any())
            {
                List<double> scoreList = query.ToList();

                this.maxScore = scoreList.Max<double>();
                this.minScore = scoreList.Min<double>();
                this.isReversed = false;
                bool isAvailable = false;

                for (int i = 0; i < scoreList.Count - 1; i++)
                {
                    if (scoreList[i] != scoreList[i + 1])
                    {
                        isAvailable = true;

                        break;
                    }
                }

                if (isAvailable)
                {
                    int counter = 0;

                    for (int i = 0; i < scoreList.Count - 1; i++)
                    {
                        if (scoreList[i] >= scoreList[i + 1])
                        {
                            counter++;
                        }
                    }

                    if (counter == scoreList.Count - 1)
                    {
                        this.isReversed = true;
                    }
                }

                if (this.isReversed)
                {
                    this.thresholdScore = this.nextThresholdScore = this.minScore;
                }
                else
                {
                    this.thresholdScore = this.nextThresholdScore = this.maxScore;
                }
            }

            this.messageCollection.Add(message);

            if (this.messageCollection.Count > this.maxHistory)
            {
                List<Message> messageList = this.messageCollection.ToList();

                for (int i = 0; i < messageList.Count - this.maxHistory; i++)
                {
                    Message m = messageList[i];
                    Size size1 = GetBalloonSize(m);
                    double height = 0;

                    foreach (object o in m)
                    {
                        Entry entry = o as Entry;

                        if (entry != null)
                        {
                            Size size2 = GetInspectorSize(entry);

                            if (size2.Height > height)
                            {
                                height = size2.Height;
                            }
                        }
                    }

                    if (m.HasAttachments)
                    {
                        double h = m.Attachments.Max<Entry, double>(entry => GetInspectorSize(entry).Height);

                        if (h > height)
                        {
                            height = h;
                        }
                    }

                    if (height > 0)
                    {
                        height -= 11;
                    }

                    if (size1.Width * this.maxScale == this.LayoutRoot.Width && (size1.Height + height) * this.maxScale == this.LayoutRoot.Height)
                    {
                        isRecyclable = false;

                        break;
                    }
                }

                messageList.RemoveRange(0, messageList.Count - this.maxHistory);

                this.messageCollection.Clear();

                messageList.ForEach(delegate (Message m)
                {
                    this.messageCollection.Add(m);
                });
            }

            if (this.messageCollection.Count - 1 >= 0)
            {
                this.historyPoint = new Nullable<int>(this.messageCollection.Count - 1);
            }

            Size balloonSize = GetBalloonSize(message, ref this.messageIsScrollable);
            double maxHeight = 0;

            foreach (object o in message)
            {
                Entry entry = o as Entry;

                if (entry != null)
                {
                    Size size = GetInspectorSize(entry);

                    if (size.Height > maxHeight)
                    {
                        maxHeight = size.Height;
                    }
                }
            }

            if (message.HasAttachments)
            {
                double height = message.Attachments.Max<Entry, double>(entry => GetInspectorSize(entry).Height);

                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }

            if (maxHeight > 0)
            {
                maxHeight -= 11;
            }

            Size maxSize = new Size(balloonSize.Width, balloonSize.Height + maxHeight);

            if (isRecyclable && this.LayoutRoot.Width >= maxSize.Width * this.maxScale && this.LayoutRoot.Height >= maxSize.Height * this.maxScale)
            {
                maxSize.Width = this.LayoutRoot.Width;
                maxSize.Height = this.LayoutRoot.Height;
            }
            else
            {
                foreach (Message m in from m in this.messageCollection where m != message select m)
                {
                    Size size1 = GetBalloonSize(m);
                    double height = 0;

                    foreach (object o in m)
                    {
                        Entry entry = o as Entry;

                        if (entry != null)
                        {
                            Size size2 = GetInspectorSize(entry);

                            if (size2.Height > height)
                            {
                                height = size2.Height;
                            }
                        }
                    }

                    if (m.HasAttachments)
                    {
                        double h = m.Attachments.Max<Entry, double>(entry => GetInspectorSize(entry).Height);

                        if (h > height)
                        {
                            height = h;
                        }
                    }

                    if (height > 0)
                    {
                        height -= 11;
                    }

                    if (size1.Width > maxSize.Width)
                    {
                        maxSize.Width = size1.Width;
                    }

                    if (size1.Height + height > maxSize.Height)
                    {
                        maxSize.Height = size1.Height + height;
                    }
                }

                maxSize.Width = maxSize.Width * this.maxScale;
                maxSize.Height = maxSize.Height * this.maxScale;
            }

            this.sourceSize = this.targetSize = balloonSize;
            this.targetOpacity = 1;
            this.targetScaleX = this.targetScaleY = 1;

            Geometry roundedRectangleGeometry = CreateRoundedRectangleGeometry(new Rect(0, 0, this.sourceSize.Width, this.sourceSize.Height - 11 + 4), 8, 8);
            Geometry balloonGeometry = CreateBalloonGeometry(new Rect(4, 4, this.sourceSize.Width - 4 * 2, this.sourceSize.Height - 4), 8 * 3 / 4, 8 * 3 / 4);
            Geometry highlightGeometry = CreateHighlightGeometry(new Rect(0, 0, this.sourceSize.Width, this.baseHeaderHeight + 8), 8, 8);
            Geometry highlightLineGeometry = CreateHighlightLineGeometry(new Rect(0, 0, this.sourceSize.Width, 8), 8, 8);

            if (roundedRectangleGeometry.CanFreeze)
            {
                roundedRectangleGeometry.Freeze();
            }

            if (balloonGeometry.CanFreeze)
            {
                balloonGeometry.Freeze();
            }

            if (highlightGeometry.CanFreeze)
            {
                highlightGeometry.Freeze();
            }

            if (highlightLineGeometry.CanFreeze)
            {
                highlightLineGeometry.Freeze();
            }

            GeometryGroup geometryGroup = new GeometryGroup();

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(roundedRectangleGeometry);
            geometryGroup.Children.Add(balloonGeometry);

            if (geometryGroup.CanFreeze)
            {
                geometryGroup.Freeze();
            }

            RadialGradientBrush radialGradientBrush = new RadialGradientBrush();
            GradientStop gradientStop1 = new GradientStop(Color.FromArgb(Byte.MaxValue, Byte.MaxValue, Byte.MaxValue, Byte.MaxValue), 0);
            GradientStop gradientStop2 = new GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1);

            radialGradientBrush.GradientOrigin = new Point(0.5, 0);
            radialGradientBrush.Center = new Point(0.5, 0);
            radialGradientBrush.RadiusX = 0.5;
            radialGradientBrush.RadiusY = this.sourceSize.Width / 2 < this.sourceSize.Height - this.baseFooterHeight ? this.sourceSize.Width / 2 / this.sourceSize.Height : (this.sourceSize.Height - this.baseFooterHeight) / this.sourceSize.Height;
            radialGradientBrush.Opacity = 0.1;
            radialGradientBrush.GradientStops.Add(gradientStop1);
            radialGradientBrush.GradientStops.Add(gradientStop2);

            if (radialGradientBrush.CanFreeze)
            {
                radialGradientBrush.Freeze();
            }

            this.Width = this.LayoutRoot.Width = maxSize.Width;
            this.Height = this.LayoutRoot.Height = maxSize.Height;
            this.OuterPath.Width = roundedRectangleGeometry.Bounds.Right;
            this.OuterPath.Height = roundedRectangleGeometry.Bounds.Bottom;
            this.OuterPath.Data = roundedRectangleGeometry;
            this.InnerPath.Width = balloonGeometry.Bounds.Right;
            this.InnerPath.Height = balloonGeometry.Bounds.Bottom;
            this.InnerPath.Data = balloonGeometry;
            this.OverlayPath.Width = geometryGroup.Bounds.Right;
            this.OverlayPath.Height = geometryGroup.Bounds.Bottom;
            this.OverlayPath.Data = geometryGroup;
            this.OverlayPath.Fill = radialGradientBrush;
            this.HighlightPath.Width = highlightGeometry.Bounds.Right;
            this.HighlightPath.Height = highlightGeometry.Bounds.Bottom;
            this.HighlightPath.Data = highlightGeometry;
            this.HighlightLinePath.Width = highlightLineGeometry.Bounds.Right;
            this.HighlightLinePath.Height = highlightLineGeometry.Bounds.Bottom;
            this.HighlightLinePath.Data = highlightLineGeometry;
            this.Canvas.Width = this.sourceSize.Width;
            this.Canvas.Height = this.sourceSize.Height;
            this.ScaleTransform.CenterX = this.sourceSize.Width / 2;
            this.ScaleTransform.CenterY = this.sourceSize.Height;

            Canvas.SetLeft(this.Canvas, (maxSize.Width - this.sourceSize.Width) / 2);
            Canvas.SetTop(this.Canvas, maxSize.Height - this.sourceSize.Height);
            Canvas.SetLeft(this.FilterImage, this.sourceSize.Width - 14);
            Canvas.SetLeft(this.ScrollImage, this.sourceSize.Width - 14);
            Canvas.SetLeft(this.CloseImage, this.sourceSize.Width - 17);
            Canvas.SetLeft(this.BackImage, this.sourceSize.Width - 30);
            Canvas.SetLeft(this.UpImage, this.sourceSize.Width - 17);
            Canvas.SetTop(this.UpImage, this.sourceSize.Height - 32);
            Canvas.SetLeft(this.DownImage, this.sourceSize.Width - 17);
            Canvas.SetTop(this.DownImage, this.sourceSize.Height - 21);

            Show();
        }

        public void Back()
        {
            if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value > 0)
            {
                this.nextHistoryPoint = new Nullable<int>(this.historyPoint.Value - 1);
                this.messageIsScrollable = false;
                this.enableFilter = false;
                this.previousInspectorEntry = this.inspectorEntry;
                this.nextInspectorEntry = null;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (sender == this.waitTimer && !this.isPinned && this.targetOpacity > 0 && this.targetScaleX > 0 && this.targetScaleY > 0)
            {
                this.sourceOpacity = this.Canvas.Opacity;
                this.targetOpacity = 0;
                this.sourceScaleX = this.ScaleTransform.ScaleX;
                this.sourceScaleY = this.ScaleTransform.ScaleY;
                this.targetScaleX = this.targetScaleY = 0;
                this.popupStep = 0;
                this.waitTimer.Stop();
            }
            else if (sender == this.messageTypeTimer && this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
            {
                Message message = this.messageCollection[this.historyPoint.Value];
                bool stopRequired = true;

                if (this.messageBuffer.Length < this.targetMessageLength)
                {
                    int index = this.messageBuffer.Length;
                    string text = message.Text;

                    if (index < text.Length)
                    {
                        int width = text.Length / 2 + text.Length % 2;
                        int length = text.Length;
                        int start = text.LastIndexOf(Environment.NewLine, index, StringComparison.Ordinal);
                        int end = text.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);

                        if (start >= 0 || end >= 0)
                        {
                            if (start < 0)
                            {
                                start = 0;
                            }
                            else
                            {
                                start += Environment.NewLine.Length;
                            }

                            if (end < 0)
                            {
                                end = text.Length;
                            }

                            width = start + (end - start) / 2 + (end - start) % 2;
                            length = end;
                        }

                        if (this.randomMessageLength >= width)
                        {
                            if (text.IndexOf(Environment.NewLine, this.messageBuffer.Length, StringComparison.Ordinal) == this.messageBuffer.Length)
                            {
                                this.messageBuffer.Append(Environment.NewLine);
                            }
                            else
                            {
                                this.messageBuffer.Append(text[index]);
                            }

                            if (this.messageBuffer.Length != this.targetMessageLength)
                            {
                                stopRequired = false;
                            }
                        }
                        else
                        {
                            stopRequired = false;
                        }

                        if (this.randomMessageLength < length)
                        {
                            if (text.IndexOf(Environment.NewLine, this.randomMessageLength, StringComparison.Ordinal) == this.randomMessageLength)
                            {
                                this.randomMessageLength += Environment.NewLine.Length;
                            }
                            else
                            {
                                this.randomMessageLength++;
                            }
                        }
                    }
                    else
                    {
                        stopRequired = false;
                    }
                }
                else if (this.randomMessageLength > this.targetMessageLength)
                {
                    int index = this.randomMessageLength - 1;
                    string text = message.Text;

                    if (index < text.Length)
                    {
                        int start = text.LastIndexOf(Environment.NewLine, index, StringComparison.Ordinal);
                        int end = text.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);

                        if (start < 0)
                        {
                            start = 0;
                        }
                        else
                        {
                            start += Environment.NewLine.Length;
                        }

                        if (end < 0)
                        {
                            end = text.Length;
                        }

                        if (this.messageBuffer.Length <= start + (end - start) / 2)
                        {
                            if (this.randomMessageLength - Environment.NewLine.Length >= 0 && text.LastIndexOf(Environment.NewLine, this.randomMessageLength, StringComparison.Ordinal) == this.randomMessageLength - Environment.NewLine.Length)
                            {
                                this.randomMessageLength -= Environment.NewLine.Length;
                            }
                            else
                            {
                                this.randomMessageLength--;
                            }
                        }

                        if (this.messageBuffer.Length > start)
                        {
                            if (this.messageBuffer.Length - Environment.NewLine.Length >= 0 && text.LastIndexOf(Environment.NewLine, this.messageBuffer.Length, StringComparison.Ordinal) == this.messageBuffer.Length - Environment.NewLine.Length)
                            {
                                this.messageBuffer.Remove(this.messageBuffer.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                            }
                            else
                            {
                                this.messageBuffer.Remove(this.messageBuffer.Length - 1, 1);
                            }
                        }
                    }

                    stopRequired = false;
                }

                if (stopRequired)
                {
                    this.messageTypeTimer.Stop();
                }
            }
            else if (sender == this.switchTimer && this.inspectorEntry != null)
            {
                const double space = 10;
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                double x = 0;

                if (this.inspectorEntry.HasMultipleImages)
                {
                    this.inspectorEntry.NextImage();

                    if (!this.imageDictionary.ContainsKey(this.inspectorEntry.Image) && !this.imageUriHashSet.Contains(this.inspectorEntry.Image))
                    {
                        this.imageUriHashSet.Add(this.inspectorEntry.Image);

                        UpdateImage(this.inspectorEntry.Image, false);
                    }

                    this.imageUriQueue.Enqueue(this.inspectorEntry.Image);
                }

                foreach (string tag in this.inspectorEntry.Tags)
                {
                    Dictionary<int, int> dictionary = new Dictionary<int, int>();
                    StringBuilder lineStringBuilder = new StringBuilder();
                    bool isCirculatable = false;

                    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(tag, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                    {
                        dictionary.Add(match.Index, match.Length);
                    }

                    if (x + space + Math.Ceiling(new FormattedText(tag, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && x != 0)
                    {
                        this.circulationQueue.Enqueue(1);

                        break;
                    }

                    for (int i = 0; i < tag.Length; i++)
                    {
                        int length;

                        if (dictionary.TryGetValue(i, out length) && x + space + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), tag.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && lineStringBuilder.Length > 0)
                        {
                            isCirculatable = true;

                            break;
                        }

                        lineStringBuilder.Append(tag[i]);

                        if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                        {
                            isCirculatable = true;

                            break;
                        }
                        else if (lineStringBuilder.Length > 0 && x + space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74)
                        {
                            isCirculatable = true;

                            break;
                        }
                    }

                    if (isCirculatable)
                    {
                        this.circulationQueue.Enqueue(1);

                        break;
                    }

                    if (lineStringBuilder.Length > 0)
                    {
                        x += space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                    }
                }
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            TimeSpan ts = ((RenderingEventArgs)e).RenderingTime;
            double frameRate = 1000 / (ts - this.lastRenderingTime).TotalMilliseconds;

            if (frameRate <= this.frameRate)
            {
                this.lastRenderingTime = ts;

                if (frameRate >= 1)
                {
                    this.frameRateList.Add(frameRate > this.frameRate / 10 ? frameRate : this.frameRate / 10);

                    double averageFrameRate = this.frameRateList.Average();
                    int limit = (int)Math.Round(averageFrameRate);

                    if (this.frameRateList.Count > limit)
                    {
                        this.frameRateList.RemoveRange(0, this.frameRateList.Count - limit);
                    }

                    if (this.IsVisible)
                    {
                        bool isReady = this.isReady;

                        if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count && this.targetSize.Width == this.Canvas.Width && this.targetSize.Height == this.Canvas.Height && this.inspectorFadeStep == null && this.inspectorEntry != this.nextInspectorEntry && this.nextInspectorEntry == null)
                        {
                            Size size = GetBalloonSize(this.messageCollection[this.historyPoint.Value], ref this.messageIsScrollable);

                            this.inspectorEntry = this.nextInspectorEntry;

                            if (size.Width != this.targetSize.Width || size.Height != this.targetSize.Height)
                            {
                                this.targetSize = size;

                                if (this.targetSize.Width != this.Canvas.Width || this.targetSize.Height != this.Canvas.Height)
                                {
                                    this.sourceSize.Width = this.Canvas.Width;
                                    this.sourceSize.Height = this.Canvas.Height;
                                    this.resizeStep = 0;
                                }
                            }
                        }

                        if (this.messageCollection.Count > 0 && this.nextHistoryPoint.HasValue && this.nextHistoryPoint.Value < this.messageCollection.Count && !this.isReady && this.targetSize.Width == this.Canvas.Width && this.targetSize.Height == this.Canvas.Height)
                        {
                            Message message = this.messageCollection[this.nextHistoryPoint.Value];

                            this.historyPoint = this.nextHistoryPoint;
                            this.nextHistoryPoint = null;

                            var query = from entry in message.Attachments where entry.Score.HasValue select entry.Score.Value;

                            if (query.Any())
                            {
                                List<double> scoreList = query.ToList();

                                this.maxScore = scoreList.Max<double>();
                                this.minScore = scoreList.Min<double>();
                                this.isReversed = false;
                                bool enable = false;

                                for (int i = 0; i < scoreList.Count - 1; i++)
                                {
                                    if (scoreList[i] != scoreList[i + 1])
                                    {
                                        enable = true;

                                        break;
                                    }
                                }

                                if (enable)
                                {
                                    int counter = 0;

                                    for (int i = 0; i < scoreList.Count - 1; i++)
                                    {
                                        if (scoreList[i] >= scoreList[i + 1])
                                        {
                                            counter++;
                                        }
                                    }

                                    if (counter == scoreList.Count - 1)
                                    {
                                        this.isReversed = true;
                                    }
                                }

                                if (this.isReversed)
                                {
                                    this.thresholdScore = this.nextThresholdScore = this.minScore;
                                }
                                else
                                {
                                    this.thresholdScore = this.nextThresholdScore = this.maxScore;
                                }
                            }

                            Size size = GetBalloonSize(message, ref this.messageIsScrollable);

                            if (size.Width != this.targetSize.Width || size.Height != this.targetSize.Height)
                            {
                                this.targetSize = size;

                                if (this.targetSize.Width != this.Canvas.Width || this.targetSize.Height != this.Canvas.Height)
                                {
                                    this.sourceSize.Width = this.Canvas.Width;
                                    this.sourceSize.Height = this.Canvas.Height;
                                    this.resizeStep = 0;
                                }
                            }
                        }

                        if (this.sourceOpacity == this.targetOpacity && this.targetOpacity > 0 && this.sourceScaleX == this.targetScaleX && this.targetScaleX > 0 && this.sourceScaleY == this.targetScaleY && this.targetScaleY > 0)
                        {
                            if (this.targetSize.Width == this.Canvas.Width && this.targetSize.Height == this.Canvas.Height)
                            {
                                this.isReady = true;
                            }
                        }
                        else
                        {
                            this.popupStep += 1 / (averageFrameRate / 2);

                            if (this.popupStep >= 1)
                            {
                                this.popupStep = 1;
                                this.Canvas.Opacity = this.sourceOpacity = this.targetOpacity;
                                this.ScaleTransform.ScaleX = this.sourceScaleX = this.targetScaleX;
                                this.ScaleTransform.ScaleY = this.sourceScaleY = this.targetScaleY;
                            }
                            else
                            {
                                if (this.targetOpacity > this.sourceOpacity)
                                {
                                    this.Canvas.Opacity = this.sourceOpacity + (this.targetOpacity - this.sourceOpacity) * Math.Sin((this.popupStep > 0.5 ? 0.5 : this.popupStep) * Math.PI);
                                }
                                else if (this.targetOpacity < this.sourceOpacity)
                                {
                                    this.Canvas.Opacity = this.targetOpacity + (this.sourceOpacity - this.targetOpacity) * Math.Cos(this.popupStep / 2 * Math.PI);
                                }

                                if (this.targetScaleX > this.sourceScaleX || this.targetScaleY > this.sourceScaleY)
                                {
                                    if (this.popupStep > 0.5)
                                    {
                                        this.ScaleTransform.ScaleX = this.targetScaleX + (this.targetScaleX * this.maxScale - this.targetScaleX) * Math.Sin(this.popupStep * Math.PI);
                                        this.ScaleTransform.ScaleY = this.targetScaleY + (this.targetScaleY * this.maxScale - this.targetScaleY) * Math.Sin(this.popupStep * Math.PI);
                                    }
                                    else
                                    {
                                        this.ScaleTransform.ScaleX = this.sourceScaleX + (this.targetScaleX * this.maxScale - this.sourceScaleX) * Math.Sin(this.popupStep * Math.PI);
                                        this.ScaleTransform.ScaleY = this.sourceScaleY + (this.targetScaleY * this.maxScale - this.sourceScaleY) * Math.Sin(this.popupStep * Math.PI);
                                    }
                                }
                                else if (this.targetScaleX < this.sourceScaleX || this.targetScaleY < this.sourceScaleY)
                                {
                                    this.ScaleTransform.ScaleX = this.targetScaleX + (this.sourceScaleX - this.targetScaleX) * Math.Cos(this.popupStep / 2 * Math.PI);
                                    this.ScaleTransform.ScaleY = this.targetScaleY + (this.sourceScaleY - this.targetScaleY) * Math.Cos(this.popupStep / 2 * Math.PI);
                                }
                            }
                        }

                        if (this.isReady)
                        {
                            if (this.messageCollection.Count > 0 && this.historyPoint.HasValue && this.historyPoint.Value < this.messageCollection.Count)
                            {
                                Lazy<List<Point>> inlinePointList = new Lazy<List<Point>>(() =>
                                {
                                    Point location = new Point(0, 0);
                                    double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                    bool isBreaked = true;
                                    List<Point> pointList = new List<Point>();
                                    bool isReseted = true;

                                    foreach (object o in this.messageCollection[this.historyPoint.Value])
                                    {
                                        string inline = o as string;
                                        Brush brush = this.textBrush;
                                        Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                        StringBuilder lineStringBuilder = new StringBuilder();

                                        if (inline == null)
                                        {
                                            Entry entry = o as Entry;

                                            if (entry == null)
                                            {
                                                inline = o.ToString();
                                            }
                                            else
                                            {
                                                inline = entry.Title;
                                                brush = this.linkBrush;
                                            }
                                        }

                                        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                        {
                                            dictionary.Add(match.Index, match.Length);
                                        }

                                        for (int i = 0; i < inline.Length; i++)
                                        {
                                            int length;

                                            if (dictionary.TryGetValue(i, out length) && location.X + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width && !isReseted)
                                            {
                                                if (lineStringBuilder.Length > 0)
                                                {
                                                    pointList.Add(new Point(location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace), location.Y));
                                                }

                                                lineStringBuilder.Clear();
                                                location.X = 0;
                                                location.Y += this.lineHeight;
                                                isBreaked = true;
                                            }

                                            lineStringBuilder.Append(inline[i]);

                                            if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                            {
                                                lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                if (lineStringBuilder.Length > 0)
                                                {
                                                    pointList.Add(new Point(location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace), location.Y));
                                                    lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                }
                                                else
                                                {
                                                    pointList.Add(location);
                                                }

                                                pointList.Add(new Point(Double.NaN, location.Y));
                                                location.X = 0;
                                                location.Y += this.lineHeight;
                                                isBreaked = true;
                                                isReseted = true;
                                            }
                                            else
                                            {
                                                if (location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width)
                                                {
                                                    if (lineStringBuilder.Length - 1 > 0)
                                                    {
                                                        pointList.Add(new Point(location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace), location.Y));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                    }
                                                    else
                                                    {
                                                        pointList.Add(location);
                                                    }

                                                    location.X = 0;
                                                    location.Y += this.lineHeight;
                                                    isBreaked = true;
                                                    isReseted = true;
                                                }
                                                else
                                                {
                                                    isReseted = false;
                                                }
                                            }
                                        }

                                        if (lineStringBuilder.Length > 0)
                                        {
                                            location.X += Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                                            pointList.Add(location);
                                            isBreaked = false;
                                            isReseted = false;
                                        }
                                    }

                                    if (isBreaked)
                                    {
                                        pointList.Add(location);
                                    }

                                    return pointList;
                                });
                                bool isTyped = (isReady && this.nextHistoryPoint.HasValue ? this.messageCollection[this.historyPoint.Value].Cast<object>() : this.messageCollection[this.historyPoint.Value].Cast<object>().Concat(new object[] { null })).SequenceEqual(this.inlineList.Cast<object>());
                                bool isFragmented = false;
                                bool isScrolled = false;
                                int paddingLines = 0;
                                bool waitRequired = false;
                                bool updateLayoutRequired = false;

                                if (isReady && this.nextHistoryPoint.HasValue && this.targetMessageLength != 0 && this.embedColorStepDictionary.Count == 0)
                                {
                                    this.targetMessageLength = 0;
                                    this.messageTypeTimer.Interval = TimeSpan.FromSeconds(1 / this.messageCollection[this.historyPoint.Value].Speed / 2);
                                    this.messageTypeTimer.Start();
                                }
                                else if ((!isReady || !this.nextHistoryPoint.HasValue) && this.targetMessageLength != this.messageCollection[this.historyPoint.Value].Text.Length)
                                {
                                    this.targetMessageLength = this.messageCollection[this.historyPoint.Value].Text.Length;
                                    this.messageTypeTimer.Interval = TimeSpan.FromSeconds(1 / this.messageCollection[this.historyPoint.Value].Speed);
                                    this.messageTypeTimer.Start();
                                }

                                if (this.sourceScrollPosition == this.targetScrollPosition)
                                {
                                    Nullable<double> totalSelectedPosition = null;

                                    if (this.selectedPositionQueue.Count > 0)
                                    {
                                        double position = 0;

                                        do
                                        {
                                            position += this.selectedPositionQueue.Dequeue();
                                        } while (this.selectedPositionQueue.Count > 0);

                                        totalSelectedPosition = new Nullable<double>(position);
                                    }

                                    if (totalSelectedPosition.HasValue || this.scrollQueue.Count > 0)
                                    {
                                        int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hashSet, point) =>
                                        {
                                            if (!hashSet.Contains(point.Y))
                                            {
                                                hashSet.Add(point.Y);
                                            }

                                            return hashSet;
                                        }).Count;
                                        int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                        double position = this.targetScrollPosition;
                                        bool isCountable = false;

                                        while (this.scrollQueue.Count > 0)
                                        {
                                            this.targetScrollPosition += this.scrollQueue.Dequeue();
                                        }

                                        if (this.selectedPosition.HasValue && totalSelectedPosition.HasValue && (this.selectedPosition.Value + totalSelectedPosition.Value < 0 || this.targetScrollPosition >= messageLines + Math.Floor(this.selectedPosition.Value + totalSelectedPosition.Value + 1) || this.targetScrollPosition + maxLines < messageLines + Math.Floor(this.selectedPosition.Value + totalSelectedPosition.Value + 1)))
                                        {
                                            this.targetScrollPosition += totalSelectedPosition.Value;
                                        }

                                        if (this.targetScrollPosition < 0)
                                        {
                                            this.targetScrollPosition = 0;
                                        }
                                        else if (this.targetScrollPosition + maxLines > messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count)
                                        {
                                            this.targetScrollPosition = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count - maxLines;

                                            if (position == this.targetScrollPosition && this.messageCollection[this.historyPoint.Value].HasAttachments)
                                            {
                                                isCountable = true;
                                            }
                                        }

                                        if (this.selectedPosition.HasValue)
                                        {
                                            double lines = totalSelectedPosition.HasValue ? this.selectedPosition.Value + totalSelectedPosition.Value : this.selectedPosition.Value;

                                            if (this.targetScrollPosition - messageLines > Math.Floor(lines))
                                            {
                                                lines = this.targetScrollPosition - messageLines;
                                            }
                                            else if (Math.Floor(lines) > this.targetScrollPosition - messageLines + maxLines - 1)
                                            {
                                                lines = this.targetScrollPosition - messageLines + maxLines - 1;
                                            }

                                            if (this.selectedPosition.Value == lines)
                                            {
                                                if (isCountable && Math.Floor(this.selectedPosition.Value) == this.messageCollection[this.historyPoint.Value].Attachments.Count - 1 && this.counterScrollStep == null)
                                                {
                                                    this.counterScrollStep = new Nullable<double>(0);
                                                }
                                            }
                                            else
                                            {
                                                this.selectedIndex = new Nullable<int>((int)Math.Floor(this.selectedPosition.Value));

                                                if (lines < 0)
                                                {
                                                    this.selectedPosition = new Nullable<double>(0);
                                                }
                                                else if (lines >= this.messageCollection[this.historyPoint.Value].Attachments.Count)
                                                {
                                                    this.selectedPosition = new Nullable<double>(this.messageCollection[this.historyPoint.Value].Attachments.Count - 1);
                                                }
                                                else
                                                {
                                                    this.selectedPosition = new Nullable<double>(lines);
                                                }
                                            }
                                        }
                                        else if (isCountable && this.counterScrollStep == null)
                                        {
                                            this.counterScrollStep = new Nullable<double>(0);
                                        }

                                        if (this.targetScrollPosition - this.sourceScrollPosition > 0)
                                        {
                                            this.scrollIndexStep = 0;
                                            isFragmented = true;
                                        }
                                        else if (this.targetScrollPosition - this.sourceScrollPosition < 0)
                                        {
                                            this.scrollIndexStep = 1;
                                            isFragmented = true;
                                        }
                                    }
                                }

                                if (this.sourceScrollPosition > this.targetScrollPosition)
                                {
                                    this.scrollIndexStep -= 1 / (averageFrameRate / 4);

                                    if (this.scrollIndexStep <= 0)
                                    {
                                        this.scrollIndexStep = 0;
                                        this.sourceScrollPosition = this.targetScrollPosition;
                                        isFragmented = true;
                                    }

                                    isScrolled = true;
                                }
                                else if (this.sourceScrollPosition < this.targetScrollPosition)
                                {
                                    this.scrollIndexStep += 1 / (averageFrameRate / 4);

                                    if (this.scrollIndexStep >= 1)
                                    {
                                        this.scrollIndexStep = 1;
                                        this.sourceScrollPosition = this.targetScrollPosition;
                                        isFragmented = true;
                                    }

                                    isScrolled = true;
                                }

                                if (this.sourceScrollPosition != this.targetScrollPosition)
                                {
                                    paddingLines = (int)Math.Ceiling(Math.Abs(this.targetScrollPosition - this.sourceScrollPosition));
                                }
                                else if (Math.Floor(this.targetScrollPosition) != Math.Ceiling(this.targetScrollPosition))
                                {
                                    paddingLines = (int)Math.Ceiling(Math.Ceiling(this.targetScrollPosition) - Math.Floor(this.targetScrollPosition));
                                }

                                if (this.counterScrollStep.HasValue)
                                {
                                    this.counterScrollStep += 1 / (averageFrameRate / 2);

                                    if (this.counterScrollStep >= 1)
                                    {
                                        this.counterScrollStep = null;
                                    }
                                }

                                if (isFragmented || !isTyped || isReady && this.nextHistoryPoint.HasValue || this.embedColorStepDictionary.Values.Any(step => isReady && this.nextHistoryPoint.HasValue && step > 0 || (!isReady || !this.nextHistoryPoint.HasValue) && step < 1) || this.embedIsScrollableHashSet.Count > 0 || this.embedScrollStepDictionary.Count > 0 || this.hoverEmbeddedIndex.HasValue && this.embedIsScrollableHashSet.Count == 0 && this.embedScrollStepDictionary.Count == 0)
                                {
                                    List<char> charList = new List<char>();
                                    Random random = new Random(Environment.TickCount);
                                    StringBuilder randomStringBuilder = new StringBuilder();
                                    string text = this.messageCollection[this.historyPoint.Value].Text.Replace(Environment.NewLine, String.Empty);
                                    int minScrollIndex = (int)Math.Floor(Math.Min(this.sourceScrollPosition, this.targetScrollPosition));
                                    int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hs, point) =>
                                    {
                                        if (!hs.Contains(point.Y))
                                        {
                                            hs.Add(point.Y);
                                        }

                                        return hs;
                                    }).Count;
                                    double totalMessageHeight = (messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count) * this.lineHeight;
                                    int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                    int usedLines = messageLines - minScrollIndex;
                                    Point baseLocation = new Point(0, -this.lineHeight * Math.Min(minScrollIndex, messageLines));
                                    Point location = new Point(0, 0);
                                    double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                    bool isReseted = true;
                                    int inlineIndex1 = 0;
                                    int inlineIndex2 = 0;
                                    int canvasIndex = 0;
                                    HashSet<UIElement> hashSet = new HashSet<UIElement>();

                                    if (usedLines > maxLines)
                                    {
                                        usedLines = maxLines;
                                    }
                                    else if (usedLines < 0)
                                    {
                                        usedLines = 0;
                                    }

                                    int actualLines = usedLines + this.messageCollection[this.historyPoint.Value].Attachments.Count;

                                    if (actualLines > maxLines)
                                    {
                                        actualLines = maxLines;
                                    }

                                    for (int i = 0; i < text.Length; i++)
                                    {
                                        if (!charList.Contains(text[i]) && !Char.IsWhiteSpace(text[i]))
                                        {
                                            charList.Add(text[i]);
                                        }
                                    }

                                    for (int i = 0; i < this.randomMessageLength; i++)
                                    {
                                        if (this.messageCollection[this.historyPoint.Value].Text.IndexOf(Environment.NewLine, i, StringComparison.Ordinal) == i)
                                        {
                                            randomStringBuilder.Append(Environment.NewLine);
                                            i += Environment.NewLine.Length - 1;
                                        }
                                        else if (charList.Count > 0)
                                        {
                                            randomStringBuilder.Append(charList[random.Next(charList.Count)]);
                                        }
                                    }

                                    if (isFragmented || (!isTyped || isReady && this.nextHistoryPoint.HasValue))
                                    {
                                        int index = 0;
                                        StringBuilder stringBuilder = new StringBuilder();

                                        this.inlineList.Clear();

                                        if (!isTyped && (!isReady || !this.nextHistoryPoint.HasValue))
                                        {
                                            waitRequired = true;
                                        }

                                        foreach (object o in this.messageCollection[this.historyPoint.Value])
                                        {
                                            string s = o as string;

                                            if (s == null)
                                            {
                                                Entry entry = o as Entry;

                                                if (entry == null)
                                                {
                                                    s = o.ToString();
                                                }
                                                else
                                                {
                                                    s = entry.Title;
                                                }
                                            }

                                            if (s != null)
                                            {
                                                if (index + s.Length > this.messageBuffer.Length)
                                                {
                                                    if (this.messageBuffer.Length > index)
                                                    {
                                                        stringBuilder.Append(this.messageBuffer.ToString().Substring(index, this.messageBuffer.Length - index));
                                                    }

                                                    waitRequired = false;

                                                    break;
                                                }
                                                else
                                                {
                                                    this.inlineList.Add(o);
                                                }

                                                index += s.Length;
                                            }
                                        }

                                        if (randomStringBuilder.Length > this.messageBuffer.Length)
                                        {
                                            stringBuilder.Append(randomStringBuilder.ToString().Substring(this.messageBuffer.Length, randomStringBuilder.Length - this.messageBuffer.Length));
                                            waitRequired = false;
                                        }

                                        if (stringBuilder.Length > 0)
                                        {
                                            this.inlineList.Add(stringBuilder);
                                        }

                                        if (!isReady || !this.nextHistoryPoint.HasValue)
                                        {
                                            this.inlineList.Add(null);
                                        }
                                    }

                                    this.ScrollCanvas.Width = this.MessageCanvas.Width = this.maxMessageSize.Width;
                                    this.ScrollCanvas.Height = this.lineHeight * actualLines;
                                    this.MessageCanvas.Height = this.lineHeight * (this.counterScrollStep.HasValue ? actualLines + paddingLines + 1 : actualLines + paddingLines);

                                    foreach (object obj in this.inlineList)
                                    {
                                        string inline = null;
                                        Entry entry = null;
                                        Brush brush = this.textBrush;
                                        Nullable<double> scrollStep = null;
                                        bool isMutable = false;

                                        if (obj is string)
                                        {
                                            inline = (string)obj;
                                        }
                                        else if (obj is Entry)
                                        {
                                            bool isScrollable = false;
                                            double step1;
                                            double step2;

                                            entry = (Entry)obj;
                                            inline = entry.Title;

                                            if (this.embedIsScrollableHashSet.Contains(inlineIndex1))
                                            {
                                                isScrollable = true;
                                                this.embedIsScrollableHashSet.Remove(inlineIndex1);
                                            }

                                            if (this.embedScrollStepDictionary.TryGetValue(inlineIndex1, out step1))
                                            {
                                                if (step1 == 1 && isScrollable)
                                                {
                                                    step1 = 0;
                                                }

                                                step1 += 1 / (averageFrameRate / 2);

                                                if (step1 >= 1)
                                                {
                                                    if (this.hoverEmbeddedIndex.HasValue && this.hoverEmbeddedIndex.Value == inlineIndex1)
                                                    {
                                                        scrollStep = new Nullable<double>(1);
                                                        this.embedScrollStepDictionary[inlineIndex1] = 1;
                                                    }
                                                    else
                                                    {
                                                        this.embedScrollStepDictionary.Remove(inlineIndex1);
                                                    }
                                                }
                                                else
                                                {
                                                    scrollStep = new Nullable<double>(step1);
                                                    this.embedScrollStepDictionary[inlineIndex1] = step1;
                                                }
                                            }
                                            else if (((this.hoverEmbeddedIndex.HasValue && this.hoverEmbeddedIndex.Value == inlineIndex1) || isScrollable))
                                            {
                                                double step = 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.hoverEmbeddedIndex.HasValue && this.hoverEmbeddedIndex.Value == inlineIndex1)
                                                    {
                                                        scrollStep = new Nullable<double>(1);
                                                        this.embedScrollStepDictionary.Add(inlineIndex1, 1);
                                                    }
                                                }
                                                else
                                                {
                                                    scrollStep = new Nullable<double>(step);
                                                    this.embedScrollStepDictionary.Add(inlineIndex1, step);
                                                }
                                            }

                                            if (this.embedColorStepDictionary.TryGetValue(inlineIndex1, out step2))
                                            {
                                                if (isReady && this.nextHistoryPoint.HasValue)
                                                {
                                                    if (!this.embedScrollStepDictionary.ContainsKey(inlineIndex1))
                                                    {
                                                        step2 -= 1 / (averageFrameRate / 4);
                                                    }

                                                    if (step2 <= 0)
                                                    {
                                                        entry = null;
                                                        isMutable = true;
                                                        this.embedColorStepDictionary.Remove(inlineIndex1);
                                                    }
                                                    else if (step2 < 1)
                                                    {
                                                        brush = new SolidColorBrush(Color.FromArgb((byte)(this.textColor.A + (this.linkColor.A - this.textColor.A) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.R + (this.linkColor.R - this.textColor.R) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.G + (this.linkColor.G - this.textColor.G) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.B + (this.linkColor.B - this.textColor.B) * Math.Sin(step2 / 2 * Math.PI))));

                                                        if (brush.CanFreeze)
                                                        {
                                                            brush.Freeze();
                                                        }

                                                        isMutable = true;
                                                        this.embedColorStepDictionary[inlineIndex1] = step2;
                                                    }
                                                    else
                                                    {
                                                        brush = this.linkBrush;
                                                    }
                                                }
                                                else if (step2 < 1)
                                                {
                                                    step2 += 1 / (averageFrameRate / 4);

                                                    if (step2 >= 1)
                                                    {
                                                        brush = this.linkBrush;
                                                        isMutable = true;
                                                        this.embedColorStepDictionary[inlineIndex1] = 1;
                                                    }
                                                    else
                                                    {
                                                        brush = new SolidColorBrush(Color.FromArgb((byte)(this.textColor.A + (this.linkColor.A - this.textColor.A) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.R + (this.linkColor.R - this.textColor.R) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.G + (this.linkColor.G - this.textColor.G) * Math.Sin(step2 / 2 * Math.PI)), (byte)(this.textColor.B + (this.linkColor.B - this.textColor.B) * Math.Sin(step2 / 2 * Math.PI))));

                                                        if (brush.CanFreeze)
                                                        {
                                                            brush.Freeze();
                                                        }

                                                        isMutable = true;
                                                        this.embedColorStepDictionary[inlineIndex1] = step2;
                                                    }
                                                }
                                                else
                                                {
                                                    brush = this.linkBrush;
                                                }
                                            }
                                            else
                                            {
                                                entry = null;
                                                isMutable = true;

                                                if (!isReady || !this.nextHistoryPoint.HasValue)
                                                {
                                                    this.embedColorStepDictionary.Add(inlineIndex1, 0);
                                                }
                                            }
                                        }
                                        else if (obj is StringBuilder)
                                        {
                                            inline = ((StringBuilder)obj).ToString();
                                            isMutable = true;
                                        }

                                        if (inline != null)
                                        {
                                            List<KeyValuePair<Point, FormattedText>> list = new List<KeyValuePair<Point, FormattedText>>();
                                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                            StringBuilder lineStringBuilder = new StringBuilder();
                                            Nullable<double> maxY = null;

                                            if (this.messageCollection[this.historyPoint.Value].Cast<object>().Any(o => o == obj))
                                            {
                                                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                                {
                                                    dictionary.Add(match.Index, match.Length);
                                                }
                                            }
                                            else
                                            {
                                                int breaks = 0;
                                                int i = -1;
                                                int j = 0;

                                                for (int k = 0; k < inline.Length; k++)
                                                {
                                                    if (inline.IndexOf(Environment.NewLine, k, StringComparison.Ordinal) == k)
                                                    {
                                                        breaks++;
                                                        k += Environment.NewLine.Length - 1;
                                                    }
                                                }

                                                while ((i = inlinePointList.Value.FindIndex(i + 1, delegate (Point point)
                                                {
                                                    return Double.IsNaN(point.X) && point.Y >= location.Y;
                                                })) >= 0)
                                                {
                                                    if (j == breaks)
                                                    {
                                                        maxY = new Nullable<double>(inlinePointList.Value[i].Y + this.lineHeight);

                                                        break;
                                                    }

                                                    j++;
                                                }

                                                if (maxY == null)
                                                {
                                                    maxY = new Nullable<double>(inlinePointList.Value.Max(point => point.Y) + this.lineHeight);
                                                }
                                            }

                                            for (int i = 0; i < inline.Length; i++)
                                            {
                                                int length;

                                                if (dictionary.TryGetValue(i, out length) && ((maxY.HasValue ? location.Y + this.lineHeight < maxY.Value : true) ? location.X + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inlinePointList.Value.Aggregate<Point, double>(0, (width, point) =>
                                                {
                                                    if (point.X > width && point.Y == location.Y)
                                                    {
                                                        return point.X;
                                                    }

                                                    return width;
                                                }) : location.X + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width) && !isReseted)
                                                {
                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<Point, FormattedText>(location, new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip)));
                                                    }

                                                    lineStringBuilder.Clear();
                                                    location.X = 0;
                                                    location.Y += this.lineHeight;
                                                }

                                                lineStringBuilder.Append(inline[i]);

                                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                {
                                                    lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<Point, FormattedText>(location, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                    }

                                                    location.X = 0;
                                                    location.Y += this.lineHeight;
                                                    isReseted = true;
                                                }
                                                else if (((maxY.HasValue ? location.Y + this.lineHeight < maxY.Value : true) ? location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inlinePointList.Value.Aggregate<Point, double>(0, (width, point) =>
                                                {
                                                    if (point.X > width && point.Y == location.Y)
                                                    {
                                                        return point.X;
                                                    }

                                                    return width;
                                                }) : location.X + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width))
                                                {
                                                    if (lineStringBuilder.Length - 1 > 0)
                                                    {
                                                        list.Add(new KeyValuePair<Point, FormattedText>(location, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                    }

                                                    location.X = 0;
                                                    location.Y += this.lineHeight;
                                                    isReseted = true;
                                                }
                                                else
                                                {
                                                    isReseted = false;
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0 && (maxY.HasValue ? location.Y < maxY.Value : true))
                                            {
                                                FormattedText formattedText = new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                                                list.Add(new KeyValuePair<Point, FormattedText>(location, formattedText));
                                                location.X += Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                                isReseted = false;
                                            }

                                            for (int i = 0; i < list.Count; i++, inlineIndex2++)
                                            {
                                                int lineNumber = (int)Math.Floor((list[i].Key.Y + this.lineHeight) / this.lineHeight) - 1;

                                                if (minScrollIndex <= lineNumber && lineNumber < minScrollIndex + usedLines + paddingLines)
                                                {
                                                    if (entry == null)
                                                    {
                                                        int key = isMutable ? -inlineIndex2 - 1 : inlineIndex2;
                                                        Image image;

                                                        if (this.cachedInlineImageDictionary.TryGetValue(key, out image))
                                                        {
                                                            double width = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                            double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                            DrawingGroup dg = new DrawingGroup();
                                                            DrawingContext dc = dg.Open();

                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                            dc.Close();

                                                            DrawingImage di = new DrawingImage(dg);

                                                            if (di.CanFreeze)
                                                            {
                                                                di.Freeze();
                                                            }

                                                            image.Source = di;
                                                            image.Width = width;
                                                            image.Height = height;

                                                            Canvas canvas = image.Parent as Canvas;

                                                            if (canvas != null)
                                                            {
                                                                canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                                canvas.Width = width;
                                                                canvas.Height = height;

                                                                Canvas.SetLeft(canvas, baseLocation.X + list[i].Key.X);
                                                                Canvas.SetTop(canvas, baseLocation.Y + list[i].Key.Y);

                                                                hashSet.Add(canvas);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double width = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                            double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                            Canvas canvas = new Canvas();
                                                            DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                            DrawingGroup dg = new DrawingGroup();
                                                            DrawingContext dc = dg.Open();

                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                            dc.Close();

                                                            dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                            dropShadowEffect.BlurRadius = 1;
                                                            dropShadowEffect.Direction = 270;
                                                            dropShadowEffect.ShadowDepth = 1;
                                                            dropShadowEffect.Opacity = 0.5;

                                                            if (dropShadowEffect.CanFreeze)
                                                            {
                                                                dropShadowEffect.Freeze();
                                                            }

                                                            canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                            canvas.VerticalAlignment = VerticalAlignment.Top;
                                                            canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                            canvas.Background = Brushes.Transparent;
                                                            canvas.Width = width;
                                                            canvas.Height = height;
                                                            canvas.Effect = dropShadowEffect;

                                                            this.MessageCanvas.Children.Insert(canvasIndex, canvas);

                                                            Canvas.SetLeft(canvas, baseLocation.X + list[i].Key.X);
                                                            Canvas.SetTop(canvas, baseLocation.Y + list[i].Key.Y);

                                                            DrawingImage di = new DrawingImage(dg);

                                                            if (di.CanFreeze)
                                                            {
                                                                di.Freeze();
                                                            }

                                                            image = new Image();
                                                            image.HorizontalAlignment = HorizontalAlignment.Left;
                                                            image.VerticalAlignment = VerticalAlignment.Top;
                                                            image.Source = di;
                                                            image.Stretch = Stretch.None;
                                                            image.Width = width;
                                                            image.Height = height;

                                                            RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                            canvas.Children.Add(image);

                                                            Canvas.SetLeft(image, 0);
                                                            Canvas.SetTop(image, 0);

                                                            this.cachedInlineImageDictionary.Add(key, image);

                                                            hashSet.Add(canvas);
                                                            updateLayoutRequired = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Image image;

                                                        if (this.cachedInlineImageDictionary.TryGetValue(inlineIndex2, out image))
                                                        {
                                                            if (isMutable)
                                                            {
                                                                double width1 = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                                double width2 = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                                double maxWidth = width2;
                                                                double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                                StringBuilder sb = new StringBuilder();
                                                                DrawingGroup dg = new DrawingGroup();
                                                                DrawingContext dc = dg.Open();

                                                                for (int j = i + 1; j < list.Count; j++)
                                                                {
                                                                    sb.Append(list[j].Value.Text);
                                                                }

                                                                for (int j = 0; j < i; j++)
                                                                {
                                                                    sb.Append(list[j].Value.Text);
                                                                }

                                                                if (sb.Length > 0)
                                                                {
                                                                    FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                                                                    height = Math.Max(height, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                                    if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                    {
                                                                        if (ft.OverhangLeading < 0)
                                                                        {
                                                                            maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                            dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                                        }
                                                                        else
                                                                        {
                                                                            maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                            dc.DrawText(ft, new Point(width2, 0));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        double width3 = Math.Ceiling(ft.Width);

                                                                        if (ft.OverhangLeading < 0)
                                                                        {
                                                                            width3 -= Math.Floor(ft.OverhangLeading);

                                                                            if (ft.OverhangTrailing < 0)
                                                                            {
                                                                                width3 -= Math.Floor(ft.OverhangTrailing);
                                                                            }

                                                                            maxWidth += width3;

                                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                            dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                                        }
                                                                        else
                                                                        {
                                                                            if (ft.OverhangTrailing < 0)
                                                                            {
                                                                                width3 -= Math.Floor(ft.OverhangTrailing);
                                                                            }

                                                                            maxWidth += width3;

                                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                            dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                            dc.DrawText(ft, new Point(width2, 0));
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                    dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                }

                                                                dc.DrawText(list[i].Value, new Point(maxWidth - Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.Close();

                                                                DrawingImage di = new DrawingImage(dg);

                                                                if (di.CanFreeze)
                                                                {
                                                                    di.Freeze();
                                                                }

                                                                image.Source = di;
                                                                image.Width = maxWidth + width1;
                                                                image.Height = height;

                                                                TranslateTransform translateTransform = image.RenderTransform as TranslateTransform;

                                                                if (translateTransform != null)
                                                                {
                                                                    translateTransform.X = scrollStep.HasValue ? -maxWidth * Math.Sin(scrollStep.Value / 2 * Math.PI) : 0;
                                                                }

                                                                Canvas canvas = image.Parent as Canvas;

                                                                if (canvas != null)
                                                                {
                                                                    canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                                    canvas.Width = width1;
                                                                    canvas.Height = height;

                                                                    Canvas.SetLeft(canvas, baseLocation.X + list[i].Key.X);
                                                                    Canvas.SetTop(canvas, baseLocation.Y + list[i].Key.Y);

                                                                    hashSet.Add(canvas);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                TranslateTransform translateTransform = image.RenderTransform as TranslateTransform;

                                                                if (translateTransform != null)
                                                                {
                                                                    if (scrollStep.HasValue)
                                                                    {
                                                                        double width = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                                        double maxWidth = width;
                                                                        StringBuilder sb = new StringBuilder();

                                                                        for (int j = i + 1; j < list.Count; j++)
                                                                        {
                                                                            sb.Append(list[j].Value.Text);
                                                                        }

                                                                        for (int j = 0; j < i; j++)
                                                                        {
                                                                            sb.Append(list[j].Value.Text);
                                                                        }

                                                                        if (sb.Length > 0)
                                                                        {
                                                                            FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                                                                            if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                            {
                                                                                if (ft.OverhangLeading < 0)
                                                                                {
                                                                                    maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);
                                                                                }
                                                                                else
                                                                                {
                                                                                    maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                double w = Math.Ceiling(ft.Width);

                                                                                if (ft.OverhangLeading < 0)
                                                                                {
                                                                                    w -= Math.Floor(ft.OverhangLeading);

                                                                                    if (ft.OverhangTrailing < 0)
                                                                                    {
                                                                                        w -= Math.Floor(ft.OverhangTrailing);
                                                                                    }

                                                                                    maxWidth += w;
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (ft.OverhangTrailing < 0)
                                                                                    {
                                                                                        w -= Math.Floor(ft.OverhangTrailing);
                                                                                    }

                                                                                    maxWidth += w;
                                                                                }
                                                                            }
                                                                        }

                                                                        translateTransform.X = -maxWidth * Math.Sin(scrollStep.Value / 2 * Math.PI);
                                                                    }
                                                                    else
                                                                    {
                                                                        translateTransform.X = 0;
                                                                    }
                                                                }

                                                                Canvas canvas = image.Parent as Canvas;

                                                                if (canvas != null)
                                                                {
                                                                    Canvas.SetLeft(canvas, baseLocation.X + list[i].Key.X);
                                                                    Canvas.SetTop(canvas, baseLocation.Y + list[i].Key.Y);

                                                                    hashSet.Add(canvas);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int index = inlineIndex1;
                                                            double width1 = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                            double width2 = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                            double maxWidth = width2;
                                                            double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                            StringBuilder sb = new StringBuilder();
                                                            Canvas canvas = new Canvas();
                                                            DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                            DrawingGroup dg = new DrawingGroup();
                                                            DrawingContext dc = dg.Open();

                                                            for (int j = i + 1; j < list.Count; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            for (int j = 0; j < i; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            if (sb.Length > 0)
                                                            {
                                                                FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                                                                height = Math.Max(height, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                                if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                {
                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                        dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                                    }
                                                                    else
                                                                    {
                                                                        maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                        dc.DrawText(ft, new Point(width2, 0));
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    double width3 = Math.Ceiling(ft.Width);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        width3 -= Math.Floor(ft.OverhangLeading);

                                                                        if (ft.OverhangTrailing < 0)
                                                                        {
                                                                            width3 -= Math.Floor(ft.OverhangTrailing);
                                                                        }

                                                                        maxWidth += width3;

                                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                        dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                                    }
                                                                    else
                                                                    {
                                                                        if (ft.OverhangTrailing < 0)
                                                                        {
                                                                            width3 -= Math.Floor(ft.OverhangTrailing);
                                                                        }

                                                                        maxWidth += width3;

                                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                        dc.DrawText(ft, new Point(width2, 0));
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                            }

                                                            dc.DrawText(list[i].Value, new Point(maxWidth - Math.Floor(list[i].Value.OverhangLeading), 0));
                                                            dc.Close();

                                                            dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                            dropShadowEffect.BlurRadius = 1;
                                                            dropShadowEffect.Direction = 270;
                                                            dropShadowEffect.ShadowDepth = 1;
                                                            dropShadowEffect.Opacity = 0.5;

                                                            if (dropShadowEffect.CanFreeze)
                                                            {
                                                                dropShadowEffect.Freeze();
                                                            }

                                                            canvas = new Canvas();
                                                            canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                            canvas.VerticalAlignment = VerticalAlignment.Top;
                                                            canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                            canvas.Background = Brushes.Transparent;
                                                            canvas.ClipToBounds = true;
                                                            canvas.Width = width1;
                                                            canvas.Height = height;
                                                            canvas.Effect = dropShadowEffect;

                                                            this.MessageCanvas.Children.Insert(canvasIndex, canvas);

                                                            Canvas.SetLeft(canvas, baseLocation.X + list[i].Key.X);
                                                            Canvas.SetTop(canvas, baseLocation.Y + list[i].Key.Y);

                                                            DrawingImage di = new DrawingImage(dg);

                                                            if (di.CanFreeze)
                                                            {
                                                                di.Freeze();
                                                            }

                                                            image = new Image();
                                                            image.HorizontalAlignment = HorizontalAlignment.Left;
                                                            image.VerticalAlignment = VerticalAlignment.Top;
                                                            image.Source = di;
                                                            image.Stretch = Stretch.None;
                                                            image.Width = maxWidth + width1;
                                                            image.Height = height;
                                                            image.RenderTransform = new TranslateTransform(scrollStep.HasValue ? -maxWidth * Math.Sin(scrollStep.Value / 2 * Math.PI) : 0, 0);
                                                            image.MouseEnter += new MouseEventHandler(delegate
                                                            {
                                                                this.hoverEmbeddedIndex = new Nullable<int>(index);
                                                            });
                                                            image.MouseLeave += new MouseEventHandler(delegate
                                                            {
                                                                this.hoverEmbeddedIndex = null;
                                                            });
                                                            image.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                            {
                                                                if (this.hoverEmbeddedIndex.HasValue && !this.embedIsScrollableHashSet.Contains(this.hoverEmbeddedIndex.Value))
                                                                {
                                                                    this.embedIsScrollableHashSet.Add(this.hoverEmbeddedIndex.Value);
                                                                }

                                                                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                                                {
                                                                    if (entry.Resource == null)
                                                                    {
                                                                        Script.Instance.Search(entry.Title);
                                                                    }
                                                                    else
                                                                    {
                                                                        Task.Factory.StartNew(delegate (object state)
                                                                        {
                                                                            NativeMethods.ShellExecute(IntPtr.Zero, "open", (string)state, null, null, 1);
                                                                        }, entry.Resource.ToString());
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    this.previousInspectorEntry = this.inspectorEntry;
                                                                    this.nextInspectorEntry = (Entry)entry.Clone();
                                                                }

                                                                mbea.Handled = true;
                                                            });

                                                            RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                            canvas.Children.Add(image);

                                                            Canvas.SetLeft(image, 0);
                                                            Canvas.SetTop(image, 0);

                                                            this.cachedInlineImageDictionary.Add(inlineIndex2, image);

                                                            hashSet.Add(canvas);
                                                            updateLayoutRequired = true;
                                                        }
                                                    }

                                                    canvasIndex++;
                                                }
                                            }
                                        }

                                        inlineIndex1++;
                                    }

                                    foreach (KeyValuePair<int, Canvas> kvp in (from kvp in this.cachedInlineImageDictionary let canvas = kvp.Value.Parent as Canvas where canvas != null && !hashSet.Contains(canvas) select new KeyValuePair<int, Canvas>(kvp.Key, canvas)).ToArray())
                                    {
                                        this.MessageCanvas.Children.Remove(kvp.Value);
                                        this.cachedInlineImageDictionary.Remove(kvp.Key);
                                    }
                                }

                                if (this.messageCollection[this.historyPoint.Value].HasAttachments)
                                {
                                    waitRequired = false;

                                    if ((isTyped || isReady && this.nextHistoryPoint.HasValue) && (isFragmented || isReady && this.nextHistoryPoint.HasValue && this.attachmentFadeStepDictionary.Values.Any(step => step > 0) || this.attachmentFadeStepDictionary.Count == 0 || this.attachmentFadeStepDictionary.Values.Any(step => step < 1) || this.messageCollection[this.historyPoint.Value].Attachments.Where((entry, i) =>
                                    {
                                        double fadeStep;

                                        if (this.attachmentFadeStepDictionary.TryGetValue(i, out fadeStep) && fadeStep > 0)
                                        {
                                            bool maskRequired = false;
                                            double enableStep;

                                            if (this.sourceScrollPosition == this.targetScrollPosition && this.selectedIndex.HasValue && this.selectedPosition.HasValue && this.selectedIndex.Value != (int)Math.Floor(this.selectedPosition.Value) && i == (int)Math.Floor(this.selectedPosition.Value))
                                            {
                                                return true;
                                            }

                                            if (!entry.ReadOnly)
                                            {
                                                if (this.attachmentEnableStepDictionary.TryGetValue(i, out enableStep))
                                                {
                                                    if (entry.Enabled)
                                                    {
                                                        return true;
                                                    }
                                                    else if (enableStep < 1)
                                                    {
                                                        return true;
                                                    }
                                                }
                                                else if (!entry.Enabled)
                                                {
                                                    return true;
                                                }
                                            }

                                            if (entry.Score.HasValue)
                                            {
                                                if (this.isReversed && entry.Score.Value < this.thresholdScore || !this.isReversed && entry.Score.Value > this.thresholdScore)
                                                {
                                                    double filterStep;

                                                    if (this.attachmentFilterStepDictionary.TryGetValue(i, out filterStep))
                                                    {
                                                        if (filterStep < 1)
                                                        {
                                                            return true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        return true;
                                                    }
                                                }
                                                else if (this.attachmentFilterStepDictionary.ContainsKey(i))
                                                {
                                                    return true;
                                                }
                                            }

                                            if (!this.attachmentsAreHover && this.selectedPosition.HasValue && i != (int)Math.Floor(this.selectedPosition.Value))
                                            {
                                                maskRequired = true;
                                            }

                                            if (maskRequired)
                                            {
                                                double highlightStep;

                                                if (this.attachmentHighlightStepDictionary.TryGetValue(i, out highlightStep))
                                                {
                                                    if (highlightStep < 1)
                                                    {
                                                        return true;
                                                    }
                                                }
                                                else
                                                {
                                                    return true;
                                                }
                                            }
                                            else if (this.attachmentHighlightStepDictionary.ContainsKey(i))
                                            {
                                                return true;
                                            }
                                        }

                                        return false;
                                    }).Any() || !this.attachmentsAreHover && this.selectedPosition.HasValue && this.attachmentHighlightStepDictionary.Count == 0 || this.attachmentImageLoadingStepDictionary.Count > 0 || this.attachmentImagePopupStepDictionary.Count > 0 || this.attachmentIsScrollableHashSet.Count > 0 || this.attachmentScrollStepDictionary.Count > 0 || this.hoverIndex.HasValue && this.attachmentIsScrollableHashSet.Count == 0 && this.attachmentScrollStepDictionary.Count == 0))
                                    {
                                        const int leftMargin = 25;
                                        const int rightMargin = 3;
                                        int minScrollIndex = (int)Math.Floor(Math.Min(this.sourceScrollPosition, this.targetScrollPosition));
                                        int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hs, point) =>
                                        {
                                            if (!hs.Contains(point.Y))
                                            {
                                                hs.Add(point.Y);
                                            }

                                            return hs;
                                        }).Count;
                                        double totalMessageHeight = (messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count) * this.lineHeight;
                                        int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                        int usedLines = messageLines - minScrollIndex;

                                        if (usedLines < 0)
                                        {
                                            usedLines = 0;
                                        }

                                        int actualLines = usedLines + this.messageCollection[this.historyPoint.Value].Attachments.Count;

                                        if (actualLines > maxLines)
                                        {
                                            actualLines = maxLines;
                                        }

                                        bool isVisible = messageLines - minScrollIndex - actualLines - paddingLines < 0;
                                        Rect nextRect = new Rect(leftMargin, minScrollIndex <= messageLines ? this.lineHeight * usedLines : 0, this.maxMessageSize.Width - (leftMargin + rightMargin), this.lineHeight);
                                        int offset = minScrollIndex - messageLines;
                                        int length = maxLines - usedLines + paddingLines;
                                        bool imageIsUpdating = false;
                                        bool isImcomplete = false;
                                        int canvasIndex = this.cachedInlineImageDictionary.Count;
                                        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                        HashSet<UIElement> hashSet = new HashSet<UIElement>();

                                        if (offset < 0)
                                        {
                                            offset = 0;
                                        }

                                        this.ScrollCanvas.Width = this.MessageCanvas.Width = this.maxMessageSize.Width;
                                        this.ScrollCanvas.Height = this.lineHeight * actualLines;
                                        this.MessageCanvas.Height = this.lineHeight * (this.counterScrollStep.HasValue ? actualLines + paddingLines + 1 : actualLines + paddingLines);

                                        for (int i = 0; i < this.messageCollection[this.historyPoint.Value].Attachments.Count; i++)
                                        {
                                            double fadeStep;

                                            if (isVisible && i < offset + length)
                                            {
                                                bool isStepping = false;

                                                if (i > 0)
                                                {
                                                    double step;

                                                    if (this.attachmentFadeStepDictionary.TryGetValue(i - 1, out step) && Math.Sin(step / 2 * Math.PI) >= 0.5)
                                                    {
                                                        isStepping = true;
                                                    }
                                                }
                                                else if (i == 0)
                                                {
                                                    isStepping = true;
                                                }

                                                if (this.attachmentFadeStepDictionary.TryGetValue(i, out fadeStep))
                                                {
                                                    if (isReady && this.nextHistoryPoint.HasValue)
                                                    {
                                                        fadeStep -= 1 / (averageFrameRate / 4);

                                                        if (fadeStep < 0)
                                                        {
                                                            fadeStep = 0;
                                                        }

                                                        this.attachmentFadeStepDictionary[i] = fadeStep;
                                                    }
                                                    else if (fadeStep < 1 && isStepping)
                                                    {
                                                        fadeStep += 1 / (averageFrameRate / 4);

                                                        if (fadeStep >= 1 || i < offset)
                                                        {
                                                            fadeStep = 1;
                                                            waitRequired = true;
                                                        }
                                                        else
                                                        {
                                                            isImcomplete = true;
                                                        }

                                                        this.attachmentFadeStepDictionary[i] = fadeStep;
                                                    }
                                                }
                                                else if (isStepping)
                                                {
                                                    fadeStep = 1 / (averageFrameRate / 4);

                                                    if (fadeStep >= 1 || i < offset)
                                                    {
                                                        fadeStep = 1;
                                                        waitRequired = true;
                                                    }
                                                    else
                                                    {
                                                        isImcomplete = true;
                                                    }

                                                    this.attachmentFadeStepDictionary.Add(i, fadeStep);
                                                }
                                                else
                                                {
                                                    this.attachmentFadeStepDictionary.Add(i, 0);
                                                }
                                            }
                                            else if (this.attachmentFadeStepDictionary.TryGetValue(i, out fadeStep) && isReady && this.nextHistoryPoint.HasValue)
                                            {
                                                fadeStep -= 1 / (averageFrameRate / 4);

                                                if (fadeStep < 0)
                                                {
                                                    fadeStep = 0;
                                                }

                                                this.attachmentFadeStepDictionary[i] = fadeStep;
                                            }

                                            if (fadeStep > 0)
                                            {
                                                Entry attachmentEntry = this.messageCollection[this.historyPoint.Value].Attachments[i];
                                                double opacity = 1;
                                                double enableStep;
                                                bool isLoading = false;
                                                double slideStep = 0;
                                                bool slideStepIsUpdated = false;
                                                double loadStep = 0;
                                                bool isHover = this.hoverIndex.HasValue && this.hoverIndex.Value == i;
                                                double popupStep = 0;
                                                bool popupStepIsUpdated = false;
                                                bool isScrollable = false;
                                                double scrollStep1;
                                                Nullable<double> scrollStep2 = null;

                                                if (this.sourceScrollPosition == this.targetScrollPosition && this.selectedIndex.HasValue && this.selectedPosition.HasValue && this.selectedIndex.Value != (int)Math.Floor(this.selectedPosition.Value) && i == (int)Math.Floor(this.selectedPosition.Value))
                                                {
                                                    this.selectedIndex = new Nullable<int>((int)Math.Floor(this.selectedPosition.Value));
                                                    this.previousInspectorEntry = this.inspectorEntry;
                                                    this.nextInspectorEntry = (Entry)this.messageCollection[this.historyPoint.Value].Attachments[(int)Math.Floor(this.selectedPosition.Value)].Clone();
                                                }

                                                if (!attachmentEntry.ReadOnly)
                                                {
                                                    if (this.attachmentEnableStepDictionary.TryGetValue(i, out enableStep))
                                                    {
                                                        if (attachmentEntry.Enabled)
                                                        {
                                                            enableStep -= 1 / (averageFrameRate / 4);

                                                            if (enableStep <= 0)
                                                            {
                                                                enableStep = 0;
                                                                this.attachmentEnableStepDictionary.Remove(i);
                                                            }
                                                            else
                                                            {
                                                                this.attachmentEnableStepDictionary[i] = enableStep;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (enableStep < 1)
                                                            {
                                                                enableStep += 1 / (averageFrameRate / 4);

                                                                if (enableStep > 1)
                                                                {
                                                                    enableStep = 1;
                                                                }

                                                                this.attachmentEnableStepDictionary[i] = enableStep;
                                                            }
                                                        }

                                                        opacity = (opacity * 50d / 100d) + (opacity * 50d / 100d) * Math.Cos(enableStep / 2 * Math.PI);
                                                    }
                                                    else if (!attachmentEntry.Enabled)
                                                    {
                                                        enableStep = 1 / (averageFrameRate / 4);

                                                        if (enableStep > 1)
                                                        {
                                                            enableStep = 1;
                                                        }

                                                        this.attachmentEnableStepDictionary[i] = enableStep;
                                                        opacity = (opacity * 50d / 100d) + (opacity * 50d / 100d) * Math.Cos(enableStep / 2 * Math.PI);
                                                    }
                                                }

                                                if (attachmentEntry.Score.HasValue)
                                                {
                                                    if (this.isReversed && attachmentEntry.Score.Value < this.thresholdScore || !this.isReversed && attachmentEntry.Score.Value > this.thresholdScore)
                                                    {
                                                        double filterStep;

                                                        if (!this.attachmentFilterStepDictionary.TryGetValue(i, out filterStep))
                                                        {
                                                            this.attachmentFilterStepDictionary.Add(i, 0);
                                                        }

                                                        if (filterStep < 1)
                                                        {
                                                            filterStep += 1 / (averageFrameRate / 4);

                                                            if (filterStep > 1)
                                                            {
                                                                filterStep = 1;
                                                            }

                                                            this.attachmentFilterStepDictionary[i] = filterStep;
                                                        }

                                                        opacity = (opacity * 75d / 100d) + (opacity * 25d / 100d) * Math.Cos(filterStep / 2 * Math.PI);
                                                    }
                                                    else
                                                    {
                                                        double filterStep;

                                                        if (this.attachmentFilterStepDictionary.TryGetValue(i, out filterStep))
                                                        {
                                                            if (filterStep > 0)
                                                            {
                                                                filterStep -= 1 / (averageFrameRate / 4);

                                                                if (filterStep <= 0)
                                                                {
                                                                    filterStep = 0;
                                                                    this.attachmentFilterStepDictionary.Remove(i);
                                                                }
                                                                else
                                                                {
                                                                    this.attachmentFilterStepDictionary[i] = filterStep;
                                                                }
                                                            }

                                                            opacity = (opacity * 75d / 100d) + (opacity * 25d / 100d) * Math.Cos(filterStep / 2 * Math.PI);
                                                        }
                                                    }
                                                }

                                                if (!this.attachmentsAreHover && this.selectedPosition.HasValue && (int)Math.Floor(this.selectedPosition.Value) != i)
                                                {
                                                    double highlightStep;

                                                    if (!this.attachmentHighlightStepDictionary.TryGetValue(i, out highlightStep))
                                                    {
                                                        this.attachmentHighlightStepDictionary.Add(i, 0);
                                                    }

                                                    if (highlightStep < 1)
                                                    {
                                                        highlightStep += 1 / (averageFrameRate / 2);

                                                        if (highlightStep > 1)
                                                        {
                                                            highlightStep = 1;
                                                        }

                                                        this.attachmentHighlightStepDictionary[i] = highlightStep;
                                                    }

                                                    opacity = (opacity * 75d / 100d) + (opacity * 25d / 100d) * Math.Cos(highlightStep / 2 * Math.PI);
                                                }
                                                else
                                                {
                                                    double highlightStep;

                                                    if (this.attachmentHighlightStepDictionary.TryGetValue(i, out highlightStep))
                                                    {
                                                        if (highlightStep > 0)
                                                        {
                                                            highlightStep -= 1 / (averageFrameRate / 2);

                                                            if (highlightStep <= 0)
                                                            {
                                                                highlightStep = 0;
                                                                this.attachmentHighlightStepDictionary.Remove(i);
                                                            }
                                                            else
                                                            {
                                                                this.attachmentHighlightStepDictionary[i] = highlightStep;
                                                            }
                                                        }

                                                        opacity = (opacity * 75d / 100d) + (opacity * 25d / 100d) * Math.Cos(highlightStep / 2 * Math.PI);
                                                    }
                                                }

                                                opacity = opacity * Math.Sin(fadeStep / 2 * Math.PI);

                                                if (i >= offset)
                                                {
                                                    if (attachmentEntry.Image == null)
                                                    {
                                                        attachmentEntry.NextImage();
                                                    }

                                                    if (attachmentEntry.Image != null && !imageIsUpdating)
                                                    {
                                                        if (!this.imageDictionary.ContainsKey(attachmentEntry.Image))
                                                        {
                                                            if (!this.imageUriHashSet.Contains(attachmentEntry.Image))
                                                            {
                                                                this.imageUriHashSet.Add(attachmentEntry.Image);

                                                                UpdateImage(attachmentEntry.Image, false);
                                                            }

                                                            imageIsUpdating = true;
                                                        }
                                                    }
                                                }

                                                if (attachmentEntry.Image != null)
                                                {
                                                    BitmapImage bitmapImage;
                                                    BitmapImage previousBitmapImage;

                                                    if (this.imageDictionary.TryGetValue(attachmentEntry.Image, out bitmapImage))
                                                    {
                                                        if (this.attachmentImageDictionary.TryGetValue(i, out previousBitmapImage) && bitmapImage != previousBitmapImage && this.attachmentImageSlideStepDictionary.TryGetValue(i, out slideStep) && slideStep == 1)
                                                        {
                                                            this.attachmentImageSlideStepDictionary[i] = 0;
                                                        }
                                                    }
                                                    else if (this.attachmentImageDictionary.TryGetValue(i, out previousBitmapImage))
                                                    {
                                                        bitmapImage = previousBitmapImage;
                                                    }

                                                    if (bitmapImage == null)
                                                    {
                                                        isLoading = true;
                                                    }
                                                    else
                                                    {
                                                        if (!this.attachmentImageSlideStepDictionary.TryGetValue(i, out slideStep))
                                                        {
                                                            this.attachmentImageSlideStepDictionary.Add(i, 0);
                                                        }

                                                        if (slideStep < 1)
                                                        {
                                                            slideStep += 1 / (averageFrameRate / 2);

                                                            if (slideStep >= 1)
                                                            {
                                                                slideStep = 1;

                                                                if (bitmapImage != previousBitmapImage)
                                                                {
                                                                    if (this.attachmentImageDictionary.ContainsKey(i))
                                                                    {
                                                                        this.attachmentImageDictionary[i] = bitmapImage;
                                                                    }
                                                                    else
                                                                    {
                                                                        this.attachmentImageDictionary.Add(i, bitmapImage);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                isLoading = true;
                                                            }

                                                            this.attachmentImageSlideStepDictionary[i] = slideStep;
                                                            slideStepIsUpdated = true;
                                                        }
                                                    }
                                                }

                                                if (isLoading)
                                                {
                                                    if (this.attachmentImageLoadingStepDictionary.TryGetValue(i, out loadStep))
                                                    {
                                                        loadStep += 1 / (averageFrameRate / 2);

                                                        if (loadStep >= 1)
                                                        {
                                                            loadStep = 0;
                                                        }

                                                        this.attachmentImageLoadingStepDictionary[i] = loadStep;
                                                    }
                                                    else
                                                    {
                                                        this.attachmentImageLoadingStepDictionary.Add(i, 0);
                                                    }
                                                }
                                                else if (this.attachmentImageLoadingStepDictionary.ContainsKey(i))
                                                {
                                                    this.attachmentImageLoadingStepDictionary.Remove(i);
                                                }

                                                if (isHover)
                                                {
                                                    if (!this.attachmentImagePopupStepDictionary.TryGetValue(i, out popupStep))
                                                    {
                                                        this.attachmentImagePopupStepDictionary.Add(i, 0);
                                                    }

                                                    if (popupStep < 1)
                                                    {
                                                        popupStep += 1 / (averageFrameRate / 4);

                                                        if (popupStep > 1)
                                                        {
                                                            popupStep = 1;
                                                        }

                                                        this.attachmentImagePopupStepDictionary[i] = popupStep;
                                                        popupStepIsUpdated = true;
                                                    }
                                                }
                                                else if (this.attachmentImagePopupStepDictionary.TryGetValue(i, out popupStep))
                                                {
                                                    popupStep -= 1 / (averageFrameRate / 4);

                                                    if (popupStep > 0)
                                                    {
                                                        this.attachmentImagePopupStepDictionary[i] = popupStep;
                                                    }
                                                    else
                                                    {
                                                        popupStep = 0;
                                                        this.attachmentImagePopupStepDictionary.Remove(i);
                                                    }

                                                    popupStepIsUpdated = true;
                                                }

                                                if (this.attachmentIsScrollableHashSet.Contains(i))
                                                {
                                                    isScrollable = true;
                                                    this.attachmentIsScrollableHashSet.Remove(i);
                                                }

                                                if (this.attachmentScrollStepDictionary.TryGetValue(i, out scrollStep1))
                                                {
                                                    if (scrollStep1 == 1 && isScrollable)
                                                    {
                                                        scrollStep1 = 0;
                                                    }

                                                    scrollStep1 += 1 / (averageFrameRate / 2);

                                                    if (scrollStep1 >= 1)
                                                    {
                                                        if (isHover)
                                                        {
                                                            scrollStep2 = new Nullable<double>(1);
                                                            this.attachmentScrollStepDictionary[i] = 1;
                                                        }
                                                        else
                                                        {
                                                            this.attachmentScrollStepDictionary.Remove(i);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        scrollStep2 = new Nullable<double>(scrollStep1);
                                                        this.attachmentScrollStepDictionary[i] = scrollStep1;
                                                    }
                                                }
                                                else if (isHover || isScrollable)
                                                {
                                                    scrollStep1 = 1 / (averageFrameRate / 2);

                                                    if (scrollStep1 >= 1)
                                                    {
                                                        if (isHover)
                                                        {
                                                            scrollStep2 = new Nullable<double>(1);
                                                            this.attachmentScrollStepDictionary.Add(i, 1);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        scrollStep2 = new Nullable<double>(scrollStep1);
                                                        this.attachmentScrollStepDictionary.Add(i, scrollStep1);
                                                    }
                                                }

                                                if (offset <= i && i < offset + length)
                                                {
                                                    Image attachmentThumbnailImage;
                                                    Size imageSize = new Size(8, 8);
                                                    double popupMargin = Math.Round(((this.lineHeight - imageSize.Height) / 2 < 4 ? (this.lineHeight - imageSize.Height) / 2 : 4) * Math.Sin(popupStep / 2 * Math.PI));
                                                    Rect imageRect = new Rect(nextRect.X - (imageSize.Width + 9) - popupMargin, nextRect.Y + (nextRect.Height - imageSize.Height) / 2 - popupMargin, imageSize.Width + popupMargin * 2, imageSize.Height + popupMargin * 2);

                                                    if (!String.IsNullOrEmpty(String.IsNullOrEmpty(attachmentEntry.Title) && attachmentEntry.Resource != null ? attachmentEntry.Resource.ToString() : attachmentEntry.Title))
                                                    {
                                                        Image attachmentTextImage;

                                                        if (this.cachedAttachmentTextImageDictionary.TryGetValue(i, out attachmentTextImage))
                                                        {
                                                            Canvas attachmentCanvas = attachmentTextImage.Parent as Canvas;

                                                            if (attachmentCanvas != null)
                                                            {
                                                                TranslateTransform translateTransform = attachmentTextImage.RenderTransform as TranslateTransform;

                                                                if (translateTransform != null)
                                                                {
                                                                    FormattedText formattedText = new FormattedText(String.IsNullOrEmpty(attachmentEntry.Title) && attachmentEntry.Resource != null ? attachmentEntry.Resource.ToString() : attachmentEntry.Title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                                    translateTransform.X = scrollStep2.HasValue ? -(formattedText.WidthIncludingTrailingWhitespace > formattedText.Width ? Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) - Math.Floor(formattedText.OverhangLeading) : formattedText.OverhangTrailing < 0 ? Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing) : Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading)) * Math.Sin(scrollStep2.Value / 2 * Math.PI) : 0;
                                                                }

                                                                attachmentCanvas.Opacity = opacity;

                                                                Canvas.SetLeft(attachmentCanvas, nextRect.X);
                                                                Canvas.SetTop(attachmentCanvas, nextRect.Y);

                                                                hashSet.Add(attachmentCanvas);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int index = i;
                                                            FormattedText formattedText = new FormattedText(String.IsNullOrEmpty(attachmentEntry.Title) && attachmentEntry.Resource != null ? attachmentEntry.Resource.ToString() : attachmentEntry.Title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);
                                                            double width1 = formattedText.WidthIncludingTrailingWhitespace > formattedText.Width ? Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) - Math.Floor(formattedText.OverhangLeading) : formattedText.OverhangTrailing < 0 ? Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing) : Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading);
                                                            double width2 = Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing);
                                                            double height = formattedText.OverhangAfter > 0 ? formattedText.Height + formattedText.OverhangAfter : formattedText.Height;
                                                            Canvas attachmentCanvas = new Canvas();
                                                            DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                            DrawingGroup dg = new DrawingGroup();
                                                            DrawingContext dc = dg.Open();

                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width1 + width2, height));
                                                            dc.DrawText(formattedText, new Point(-Math.Floor(formattedText.OverhangLeading), 0));
                                                            dc.DrawText(formattedText, new Point(width1 - Math.Floor(formattedText.OverhangLeading), 0));
                                                            dc.Close();

                                                            dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                            dropShadowEffect.BlurRadius = 1;
                                                            dropShadowEffect.Direction = 270;
                                                            dropShadowEffect.ShadowDepth = 1;
                                                            dropShadowEffect.Opacity = 0.5;

                                                            if (dropShadowEffect.CanFreeze)
                                                            {
                                                                dropShadowEffect.Freeze();
                                                            }

                                                            attachmentCanvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                            attachmentCanvas.VerticalAlignment = VerticalAlignment.Top;
                                                            attachmentCanvas.Margin = new Thickness(Math.Floor(formattedText.OverhangLeading), (nextRect.Height - formattedText.Height) / 2, 0, 0);
                                                            attachmentCanvas.Background = Brushes.Transparent;
                                                            attachmentCanvas.ClipToBounds = true;
                                                            attachmentCanvas.Width = width2 > nextRect.Width ? nextRect.Width : width2;
                                                            attachmentCanvas.Height = height;
                                                            attachmentCanvas.Opacity = opacity;
                                                            attachmentCanvas.Effect = dropShadowEffect;

                                                            this.MessageCanvas.Children.Insert(canvasIndex, attachmentCanvas);

                                                            Canvas.SetLeft(attachmentCanvas, nextRect.X);
                                                            Canvas.SetTop(attachmentCanvas, nextRect.Y);

                                                            DrawingImage di = new DrawingImage(dg);

                                                            if (di.CanFreeze)
                                                            {
                                                                di.Freeze();
                                                            }

                                                            attachmentTextImage = new Image();
                                                            attachmentTextImage.HorizontalAlignment = HorizontalAlignment.Left;
                                                            attachmentTextImage.VerticalAlignment = VerticalAlignment.Top;
                                                            attachmentTextImage.Source = di;
                                                            attachmentTextImage.Stretch = Stretch.None;
                                                            attachmentTextImage.Width = width1 + width2;
                                                            attachmentTextImage.Height = height;
                                                            attachmentTextImage.RenderTransform = new TranslateTransform(scrollStep2.HasValue ? -width1 * Math.Sin(scrollStep2.Value / 2 * Math.PI) : 0, 0);
                                                            attachmentTextImage.MouseEnter += new MouseEventHandler(delegate
                                                            {
                                                                this.hoverIndex = new Nullable<int>(index);
                                                            });
                                                            attachmentTextImage.MouseLeave += new MouseEventHandler(delegate
                                                            {
                                                                this.hoverIndex = null;
                                                            });
                                                            attachmentTextImage.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                            {
                                                                if (this.hoverIndex.HasValue && !this.attachmentIsScrollableHashSet.Contains(this.hoverIndex.Value))
                                                                {
                                                                    this.attachmentIsScrollableHashSet.Add(this.hoverIndex.Value);
                                                                }

                                                                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                                                {
                                                                    if (attachmentEntry.Resource != null)
                                                                    {
                                                                        Task.Factory.StartNew(delegate (object state)
                                                                        {
                                                                            NativeMethods.ShellExecute(IntPtr.Zero, "open", (string)state, null, null, 1);
                                                                        }, attachmentEntry.Resource.ToString());
                                                                    }
                                                                    else if (!String.IsNullOrEmpty(attachmentEntry.Title))
                                                                    {
                                                                        if (attachmentEntry.ReadOnly)
                                                                        {
                                                                            Script.Instance.Search(attachmentEntry.Title);
                                                                        }
                                                                        else
                                                                        {
                                                                            if (attachmentEntry.Enabled)
                                                                            {
                                                                                foreach (object o in this.messageCollection[this.historyPoint.Value])
                                                                                {
                                                                                    Entry e1 = o as Entry;

                                                                                    if (e1 != null)
                                                                                    {
                                                                                        string term = e1.Title;
                                                                                        string attribute = attachmentEntry.Title;
                                                                                        bool isNewWord = true;
                                                                                        List<Word> wordList = new List<Word>();

                                                                                        foreach (Word word in from word in Script.Instance.Words where word.Name.Equals(term) select word)
                                                                                        {
                                                                                            if (this.messageCollection[this.historyPoint.Value].Attachments.Any(e2 => word.Attributes.Contains(e2.Title)) && !word.Attributes.Contains(attribute))
                                                                                            {
                                                                                                word.Attributes.Add(attribute);
                                                                                            }

                                                                                            if (!word.HasAttributes)
                                                                                            {
                                                                                                wordList.Add(word);
                                                                                            }

                                                                                            isNewWord = false;
                                                                                        }

                                                                                        if (isNewWord)
                                                                                        {
                                                                                            Word newWord = new Word();

                                                                                            newWord.Name = term;
                                                                                            newWord.Attributes.Add(attribute);

                                                                                            Script.Instance.Words.Add(newWord);
                                                                                        }

                                                                                        wordList.ForEach(delegate (Word word)
                                                                                        {
                                                                                            Script.Instance.Words.Remove(word);
                                                                                        });
                                                                                    }
                                                                                }

                                                                                attachmentEntry.Enabled = false;
                                                                            }
                                                                            else
                                                                            {
                                                                                foreach (object o in this.messageCollection[this.historyPoint.Value])
                                                                                {
                                                                                    Entry e1 = o as Entry;

                                                                                    if (e1 != null)
                                                                                    {
                                                                                        string term = e1.Title;
                                                                                        string attribute = attachmentEntry.Title;
                                                                                        bool isNewWord = true;
                                                                                        List<Word> wordList = new List<Word>();

                                                                                        foreach (Word word in from word in Script.Instance.Words where word.Name.Equals(term) select word)
                                                                                        {
                                                                                            if (this.messageCollection[this.historyPoint.Value].Attachments.Any(e2 => word.Attributes.Contains(e2.Title)))
                                                                                            {
                                                                                                word.Attributes.Remove(attribute);
                                                                                            }

                                                                                            if (!word.HasAttributes)
                                                                                            {
                                                                                                wordList.Add(word);
                                                                                            }

                                                                                            isNewWord = false;
                                                                                        }

                                                                                        if (isNewWord)
                                                                                        {
                                                                                            Word newWord = new Word();

                                                                                            newWord.Name = term;
                                                                                            newWord.Attributes.Add(attribute);

                                                                                            Script.Instance.Words.Add(newWord);
                                                                                        }

                                                                                        wordList.ForEach(delegate (Word word)
                                                                                        {
                                                                                            Script.Instance.Words.Remove(word);
                                                                                        });
                                                                                    }
                                                                                }

                                                                                attachmentEntry.Enabled = true;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    this.selectedIndex = index;
                                                                    this.selectedPosition = new Nullable<double>(index);
                                                                    this.previousInspectorEntry = this.inspectorEntry;
                                                                    this.nextInspectorEntry = (Entry)attachmentEntry.Clone();
                                                                }

                                                                mbea.Handled = true;
                                                            });

                                                            RenderOptions.SetClearTypeHint(attachmentTextImage, ClearTypeHint.Enabled);

                                                            attachmentCanvas.Children.Add(attachmentTextImage);

                                                            Canvas.SetLeft(attachmentTextImage, 0);
                                                            Canvas.SetTop(attachmentTextImage, 0);

                                                            this.cachedAttachmentTextImageDictionary.Add(i, attachmentTextImage);
                                                            hashSet.Add(attachmentCanvas);
                                                            updateLayoutRequired = true;
                                                        }

                                                        canvasIndex++;
                                                    }

                                                    if (this.cachedAttachmentThumbnailImageDictionary.TryGetValue(i, out attachmentThumbnailImage))
                                                    {
                                                        if (attachmentEntry.Image == null)
                                                        {
                                                            if (attachmentThumbnailImage.Source == null || popupStepIsUpdated)
                                                            {
                                                                Pen pen = new Pen(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, 1);
                                                                DrawingGroup dg = new DrawingGroup();
                                                                DrawingContext dc = dg.Open();
                                                                GeometryGroup gg = new GeometryGroup();
                                                                GuidelineSet gs1 = new GuidelineSet();
                                                                GuidelineSet gs2 = new GuidelineSet();

                                                                gg.FillRule = FillRule.Nonzero;
                                                                gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, pen.Thickness / 2), new Point(imageRect.Width - pen.Thickness / 2, imageRect.Height - pen.Thickness / 2)));
                                                                gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, imageRect.Height - pen.Thickness / 2), new Point(imageRect.Width - pen.Thickness / 2, pen.Thickness / 2)));

                                                                gs1.GuidelinesX.Add(0);
                                                                gs1.GuidelinesX.Add(imageRect.Width);
                                                                gs1.GuidelinesY.Add(0);
                                                                gs1.GuidelinesY.Add(imageRect.Height);

                                                                dc.PushGuidelineSet(gs1);
                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                dc.DrawGeometry(null, pen, gg);
                                                                dc.DrawRectangle(null, pen, new Rect(pen.Thickness / 2, pen.Thickness / 2, imageRect.Width - pen.Thickness, imageRect.Height - pen.Thickness));
                                                                dc.Close();

                                                                gs2.GuidelinesX.Add(dg.Bounds.X);
                                                                gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                                                gs2.GuidelinesY.Add(dg.Bounds.Y);
                                                                gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2)));

                                                                dg.GuidelineSet = gs2;
                                                                dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2))))));

                                                                DrawingImage di = new DrawingImage(dg);

                                                                if (di.CanFreeze)
                                                                {
                                                                    di.Freeze();
                                                                }

                                                                attachmentThumbnailImage.Source = di;
                                                                attachmentThumbnailImage.Width = Math.Ceiling(dg.Bounds.Width);
                                                                attachmentThumbnailImage.Height = Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (attachmentThumbnailImage.Source == null || slideStepIsUpdated || isLoading || popupStepIsUpdated)
                                                            {
                                                                BitmapImage attachedBitmapImage;
                                                                BitmapImage previousAttachedBitmapImage;
                                                                Pen pen = new Pen(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, 1);
                                                                DrawingGroup dg = new DrawingGroup();
                                                                DrawingContext dc = dg.Open();
                                                                GeometryGroup gg = new GeometryGroup();
                                                                GuidelineSet gs1 = new GuidelineSet();
                                                                GuidelineSet gs2 = new GuidelineSet();

                                                                gs1.GuidelinesX.Add(0);
                                                                gs1.GuidelinesX.Add(imageRect.Width);
                                                                gs1.GuidelinesY.Add(0);
                                                                gs1.GuidelinesY.Add(imageRect.Height);

                                                                dc.PushGuidelineSet(gs1);

                                                                if (this.imageDictionary.TryGetValue(attachmentEntry.Image, out attachedBitmapImage))
                                                                {
                                                                    if (this.attachmentImageDictionary.TryGetValue(i, out previousAttachedBitmapImage) && attachedBitmapImage == null)
                                                                    {
                                                                        attachedBitmapImage = previousAttachedBitmapImage;
                                                                    }
                                                                }
                                                                else if (this.attachmentImageDictionary.TryGetValue(i, out previousAttachedBitmapImage))
                                                                {
                                                                    attachedBitmapImage = previousAttachedBitmapImage;
                                                                }

                                                                if (attachedBitmapImage == previousAttachedBitmapImage && attachedBitmapImage != null)
                                                                {
                                                                    ImageBrush imageBrush = new ImageBrush(attachedBitmapImage);

                                                                    imageBrush.TileMode = TileMode.None;
                                                                    imageBrush.Stretch = Stretch.Fill;
                                                                    imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                    imageBrush.Viewbox = new Rect(attachedBitmapImage.Width > attachedBitmapImage.Height ? (attachedBitmapImage.Width - attachedBitmapImage.Height) / 2 : 0, attachedBitmapImage.Width > attachedBitmapImage.Height ? 0 : (attachedBitmapImage.Height - attachedBitmapImage.Width) / 2, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width);
                                                                    imageBrush.AlignmentX = AlignmentX.Left;
                                                                    imageBrush.AlignmentY = AlignmentY.Top;

                                                                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                }
                                                                else
                                                                {
                                                                    if (previousAttachedBitmapImage == null)
                                                                    {
                                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                        dc.PushTransform(new TranslateTransform(-loadStep * 20, 0));
                                                                        dc.PushClip(new RectangleGeometry(new Rect(loadStep * 20, 0, imageRect.Width, imageRect.Height)));
                                                                        dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 10 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B)), new Size(20, 20)), null, new Rect(0, 0, imageRect.Width + 20, imageRect.Height));
                                                                        dc.Pop();
                                                                        dc.Pop();
                                                                    }
                                                                    else
                                                                    {
                                                                        ImageBrush previousImageBrush = new ImageBrush(previousAttachedBitmapImage);

                                                                        previousImageBrush.TileMode = TileMode.None;
                                                                        previousImageBrush.Stretch = Stretch.Fill;
                                                                        previousImageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                        previousImageBrush.Viewbox = new Rect(previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? (previousAttachedBitmapImage.Width - previousAttachedBitmapImage.Height) / 2 : 0, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? 0 : (previousAttachedBitmapImage.Height - previousAttachedBitmapImage.Width) / 2, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? previousAttachedBitmapImage.Height : previousAttachedBitmapImage.Width, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? previousAttachedBitmapImage.Height : previousAttachedBitmapImage.Width);
                                                                        previousImageBrush.AlignmentX = AlignmentX.Left;
                                                                        previousImageBrush.AlignmentY = AlignmentY.Top;

                                                                        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                        dc.DrawRectangle(previousImageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    }

                                                                    if (attachedBitmapImage != null)
                                                                    {
                                                                        double step = Math.Sin(slideStep / 2 * Math.PI);
                                                                        StreamGeometry streamGeometry = new StreamGeometry();

                                                                        streamGeometry.FillRule = FillRule.Nonzero;

                                                                        using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                                                                        {
                                                                            streamGeometryContext.BeginFigure(new Point(0, 0), true, true);

                                                                            if (step <= 0.5)
                                                                            {
                                                                                streamGeometryContext.LineTo(new Point(imageRect.Width * step / 0.5, 0), true, false);
                                                                                streamGeometryContext.LineTo(new Point(0, imageRect.Height * step / 0.5), true, false);
                                                                            }
                                                                            else
                                                                            {
                                                                                streamGeometryContext.LineTo(new Point(imageRect.Width, 0), true, false);
                                                                                streamGeometryContext.LineTo(new Point(imageRect.Width, imageRect.Height * (step - 0.5) / 0.5), true, false);
                                                                                streamGeometryContext.LineTo(new Point(imageRect.Width * (step - 0.5) / 0.5, imageRect.Height), true, false);
                                                                                streamGeometryContext.LineTo(new Point(0, imageRect.Height), true, false);
                                                                            }
                                                                        }

                                                                        ImageBrush imageBrush = new ImageBrush(attachedBitmapImage);

                                                                        imageBrush.TileMode = TileMode.None;
                                                                        imageBrush.Stretch = Stretch.Fill;
                                                                        imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                        imageBrush.Viewbox = new Rect(attachedBitmapImage.Width > attachedBitmapImage.Height ? (attachedBitmapImage.Width - attachedBitmapImage.Height) / 2 : 0, attachedBitmapImage.Width > attachedBitmapImage.Height ? 0 : (attachedBitmapImage.Height - attachedBitmapImage.Width) / 2, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width);
                                                                        imageBrush.AlignmentX = AlignmentX.Left;
                                                                        imageBrush.AlignmentY = AlignmentY.Top;

                                                                        dc.PushClip(streamGeometry);
                                                                        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                        dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                        dc.Pop();
                                                                    }
                                                                }

                                                                dc.DrawRectangle(null, pen, new Rect(pen.Thickness / 2, pen.Thickness / 2, imageRect.Width - pen.Thickness, imageRect.Height - pen.Thickness));
                                                                dc.Close();

                                                                gs2.GuidelinesX.Add(dg.Bounds.X);
                                                                gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                                                gs2.GuidelinesY.Add(dg.Bounds.Y);
                                                                gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2)));

                                                                dg.GuidelineSet = gs2;
                                                                dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2))))));

                                                                DrawingImage di = new DrawingImage(dg);

                                                                if (di.CanFreeze)
                                                                {
                                                                    di.Freeze();
                                                                }

                                                                attachmentThumbnailImage.Source = di;
                                                                attachmentThumbnailImage.Width = Math.Ceiling(dg.Bounds.Width);
                                                                attachmentThumbnailImage.Height = Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2));
                                                            }
                                                        }

                                                        attachmentThumbnailImage.Opacity = opacity;

                                                        Canvas.SetLeft(attachmentThumbnailImage, imageRect.X);
                                                        Canvas.SetTop(attachmentThumbnailImage, imageRect.Y);

                                                        hashSet.Add(attachmentThumbnailImage);
                                                    }
                                                    else
                                                    {
                                                        int index = i;
                                                        Pen pen = new Pen(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, 1);
                                                        DrawingGroup dg = new DrawingGroup();
                                                        DrawingContext dc = dg.Open();
                                                        GuidelineSet gs1 = new GuidelineSet();
                                                        GuidelineSet gs2 = new GuidelineSet();

                                                        if (attachmentEntry.Image == null)
                                                        {
                                                            GeometryGroup gg = new GeometryGroup();

                                                            gg.FillRule = FillRule.Nonzero;
                                                            gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, pen.Thickness / 2), new Point(imageRect.Width - pen.Thickness / 2, imageRect.Height - pen.Thickness / 2)));
                                                            gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, imageRect.Height - pen.Thickness / 2), new Point(imageRect.Width - pen.Thickness / 2, pen.Thickness / 2)));

                                                            gs1.GuidelinesX.Add(0);
                                                            gs1.GuidelinesX.Add(imageRect.Width);
                                                            gs1.GuidelinesY.Add(0);
                                                            gs1.GuidelinesY.Add(imageRect.Height);

                                                            dc.PushGuidelineSet(gs1);
                                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                            dc.DrawGeometry(null, pen, gg);
                                                            dc.DrawRectangle(null, pen, new Rect(pen.Thickness / 2, pen.Thickness / 2, imageRect.Width - pen.Thickness, imageRect.Height - pen.Thickness));
                                                            dc.Close();

                                                            gs2.GuidelinesX.Add(dg.Bounds.X);
                                                            gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                                            gs2.GuidelinesY.Add(dg.Bounds.Y);
                                                            gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2)));

                                                            dg.GuidelineSet = gs2;
                                                            dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2))))));
                                                        }
                                                        else
                                                        {
                                                            BitmapImage attachedBitmapImage;
                                                            BitmapImage previousAttachedBitmapImage;

                                                            gs1.GuidelinesX.Add(0);
                                                            gs1.GuidelinesX.Add(imageRect.Width);
                                                            gs1.GuidelinesY.Add(0);
                                                            gs1.GuidelinesY.Add(imageRect.Height);

                                                            dc.PushGuidelineSet(gs1);

                                                            if (this.imageDictionary.TryGetValue(attachmentEntry.Image, out attachedBitmapImage))
                                                            {
                                                                if (this.attachmentImageDictionary.TryGetValue(i, out previousAttachedBitmapImage) && attachedBitmapImage == null)
                                                                {
                                                                    attachedBitmapImage = previousAttachedBitmapImage;
                                                                }
                                                            }
                                                            else if (this.attachmentImageDictionary.TryGetValue(i, out previousAttachedBitmapImage))
                                                            {
                                                                attachedBitmapImage = previousAttachedBitmapImage;
                                                            }

                                                            if (attachedBitmapImage == previousAttachedBitmapImage && attachedBitmapImage != null)
                                                            {
                                                                ImageBrush imageBrush = new ImageBrush(attachedBitmapImage);

                                                                imageBrush.TileMode = TileMode.None;
                                                                imageBrush.Stretch = Stretch.Fill;
                                                                imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                imageBrush.Viewbox = new Rect(attachedBitmapImage.Width > attachedBitmapImage.Height ? (attachedBitmapImage.Width - attachedBitmapImage.Height) / 2 : 0, attachedBitmapImage.Width > attachedBitmapImage.Height ? 0 : (attachedBitmapImage.Height - attachedBitmapImage.Width) / 2, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width);
                                                                imageBrush.AlignmentX = AlignmentX.Left;
                                                                imageBrush.AlignmentY = AlignmentY.Top;

                                                                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                            }
                                                            else
                                                            {
                                                                if (previousAttachedBitmapImage == null)
                                                                {
                                                                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    dc.PushTransform(new TranslateTransform(-loadStep * 20, 0));
                                                                    dc.PushClip(new RectangleGeometry(new Rect(loadStep * 20, 0, imageRect.Width, imageRect.Height)));
                                                                    dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 10 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B)), new Size(20, 20)), null, new Rect(0, 0, imageRect.Width + 20, imageRect.Height));
                                                                    dc.Pop();
                                                                    dc.Pop();
                                                                }
                                                                else
                                                                {
                                                                    ImageBrush previousImageBrush = new ImageBrush(previousAttachedBitmapImage);

                                                                    previousImageBrush.TileMode = TileMode.None;
                                                                    previousImageBrush.Stretch = Stretch.Fill;
                                                                    previousImageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                    previousImageBrush.Viewbox = new Rect(previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? (previousAttachedBitmapImage.Width - previousAttachedBitmapImage.Height) / 2 : 0, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? 0 : (previousAttachedBitmapImage.Height - previousAttachedBitmapImage.Width) / 2, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? previousAttachedBitmapImage.Height : previousAttachedBitmapImage.Width, previousAttachedBitmapImage.Width > previousAttachedBitmapImage.Height ? previousAttachedBitmapImage.Height : previousAttachedBitmapImage.Width);
                                                                    previousImageBrush.AlignmentX = AlignmentX.Left;
                                                                    previousImageBrush.AlignmentY = AlignmentY.Top;

                                                                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    dc.DrawRectangle(previousImageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                }

                                                                if (attachedBitmapImage != null)
                                                                {
                                                                    double step = Math.Sin(slideStep / 2 * Math.PI);
                                                                    StreamGeometry streamGeometry = new StreamGeometry();

                                                                    streamGeometry.FillRule = FillRule.Nonzero;

                                                                    using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                                                                    {
                                                                        streamGeometryContext.BeginFigure(new Point(0, 0), true, true);

                                                                        if (step <= 0.5)
                                                                        {
                                                                            streamGeometryContext.LineTo(new Point(imageRect.Width * step / 0.5, 0), true, false);
                                                                            streamGeometryContext.LineTo(new Point(0, imageRect.Height * step / 0.5), true, false);
                                                                        }
                                                                        else
                                                                        {
                                                                            streamGeometryContext.LineTo(new Point(imageRect.Width, 0), true, false);
                                                                            streamGeometryContext.LineTo(new Point(imageRect.Width, imageRect.Height * (step - 0.5) / 0.5), true, false);
                                                                            streamGeometryContext.LineTo(new Point(imageRect.Width * (step - 0.5) / 0.5, imageRect.Height), true, false);
                                                                            streamGeometryContext.LineTo(new Point(0, imageRect.Height), true, false);
                                                                        }
                                                                    }

                                                                    ImageBrush imageBrush = new ImageBrush(attachedBitmapImage);

                                                                    imageBrush.TileMode = TileMode.None;
                                                                    imageBrush.Stretch = Stretch.Fill;
                                                                    imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                                    imageBrush.Viewbox = new Rect(attachedBitmapImage.Width > attachedBitmapImage.Height ? (attachedBitmapImage.Width - attachedBitmapImage.Height) / 2 : 0, attachedBitmapImage.Width > attachedBitmapImage.Height ? 0 : (attachedBitmapImage.Height - attachedBitmapImage.Width) / 2, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width, attachedBitmapImage.Width > attachedBitmapImage.Height ? attachedBitmapImage.Height : attachedBitmapImage.Width);
                                                                    imageBrush.AlignmentX = AlignmentX.Left;
                                                                    imageBrush.AlignmentY = AlignmentY.Top;

                                                                    dc.PushClip(streamGeometry);
                                                                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageRect.Width, imageRect.Height));
                                                                    dc.Pop();
                                                                }
                                                            }

                                                            dc.DrawRectangle(null, pen, new Rect(pen.Thickness / 2, pen.Thickness / 2, imageRect.Width - pen.Thickness, imageRect.Height - pen.Thickness));
                                                            dc.Close();

                                                            gs2.GuidelinesX.Add(dg.Bounds.X);
                                                            gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                                            gs2.GuidelinesY.Add(dg.Bounds.Y);
                                                            gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2)));

                                                            dg.GuidelineSet = gs2;
                                                            dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2))))));
                                                        }

                                                        DrawingImage di = new DrawingImage(dg);

                                                        if (di.CanFreeze)
                                                        {
                                                            di.Freeze();
                                                        }

                                                        attachmentThumbnailImage = new Image();
                                                        attachmentThumbnailImage.HorizontalAlignment = HorizontalAlignment.Left;
                                                        attachmentThumbnailImage.VerticalAlignment = VerticalAlignment.Top;
                                                        attachmentThumbnailImage.Stretch = Stretch.None;
                                                        attachmentThumbnailImage.Source = di;
                                                        attachmentThumbnailImage.Width = Math.Ceiling(dg.Bounds.Width);
                                                        attachmentThumbnailImage.Height = Math.Ceiling(dg.Bounds.Height) + Math.Ceiling((nextRect.Height - imageRect.Height) / 2 - Math.Floor((nextRect.Height - imageRect.Height) / 2));
                                                        attachmentThumbnailImage.Opacity = opacity;
                                                        attachmentThumbnailImage.MouseEnter += new MouseEventHandler(delegate
                                                        {
                                                            this.hoverIndex = new Nullable<int>(index);
                                                        });
                                                        attachmentThumbnailImage.MouseLeave += new MouseEventHandler(delegate
                                                        {
                                                            this.hoverIndex = null;
                                                        });
                                                        attachmentThumbnailImage.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                        {
                                                            if (this.hoverIndex.HasValue && !this.attachmentIsScrollableHashSet.Contains(this.hoverIndex.Value))
                                                            {
                                                                this.attachmentIsScrollableHashSet.Add(this.hoverIndex.Value);
                                                            }

                                                            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                                            {
                                                                if (attachmentEntry.Resource != null)
                                                                {
                                                                    Task.Factory.StartNew(delegate (object state)
                                                                    {
                                                                        NativeMethods.ShellExecute(IntPtr.Zero, "open", (string)state, null, null, 1);
                                                                    }, attachmentEntry.Resource.ToString());
                                                                }
                                                                else if (!String.IsNullOrEmpty(attachmentEntry.Title))
                                                                {
                                                                    if (attachmentEntry.ReadOnly)
                                                                    {
                                                                        Script.Instance.Search(attachmentEntry.Title);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (attachmentEntry.Enabled)
                                                                        {
                                                                            foreach (object o in this.messageCollection[this.historyPoint.Value])
                                                                            {
                                                                                Entry e1 = o as Entry;

                                                                                if (e1 != null)
                                                                                {
                                                                                    string term = e1.Title;
                                                                                    string attribute = attachmentEntry.Title;
                                                                                    bool isNewWord = true;
                                                                                    List<Word> wordList = new List<Word>();

                                                                                    foreach (Word word in from word in Script.Instance.Words where word.Name.Equals(term) select word)
                                                                                    {
                                                                                        if (this.messageCollection[this.historyPoint.Value].Attachments.Any(e2 => word.Attributes.Contains(e2.Title)) && !word.Attributes.Contains(attribute))
                                                                                        {
                                                                                            word.Attributes.Add(attribute);
                                                                                        }

                                                                                        if (!word.HasAttributes)
                                                                                        {
                                                                                            wordList.Add(word);
                                                                                        }

                                                                                        isNewWord = false;
                                                                                    }

                                                                                    if (isNewWord)
                                                                                    {
                                                                                        Word newWord = new Word();

                                                                                        newWord.Name = term;
                                                                                        newWord.Attributes.Add(attribute);

                                                                                        Script.Instance.Words.Add(newWord);
                                                                                    }

                                                                                    wordList.ForEach(delegate (Word word)
                                                                                    {
                                                                                        Script.Instance.Words.Remove(word);
                                                                                    });
                                                                                }
                                                                            }

                                                                            attachmentEntry.Enabled = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            foreach (object o in this.messageCollection[this.historyPoint.Value])
                                                                            {
                                                                                Entry e1 = o as Entry;

                                                                                if (e1 != null)
                                                                                {
                                                                                    string term = e1.Title;
                                                                                    string attribute = attachmentEntry.Title;
                                                                                    bool isNewWord = true;
                                                                                    List<Word> wordList = new List<Word>();

                                                                                    foreach (Word word in from word in Script.Instance.Words where word.Name.Equals(term) select word)
                                                                                    {
                                                                                        if (this.messageCollection[this.historyPoint.Value].Attachments.Any(e2 => word.Attributes.Contains(e2.Title)))
                                                                                        {
                                                                                            word.Attributes.Remove(attribute);
                                                                                        }

                                                                                        if (!word.HasAttributes)
                                                                                        {
                                                                                            wordList.Add(word);
                                                                                        }

                                                                                        isNewWord = false;
                                                                                    }

                                                                                    if (isNewWord)
                                                                                    {
                                                                                        Word newWord = new Word();

                                                                                        newWord.Name = term;
                                                                                        newWord.Attributes.Add(attribute);

                                                                                        Script.Instance.Words.Add(newWord);
                                                                                    }

                                                                                    wordList.ForEach(delegate (Word word)
                                                                                    {
                                                                                        Script.Instance.Words.Remove(word);
                                                                                    });
                                                                                }
                                                                            }

                                                                            attachmentEntry.Enabled = true;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                this.selectedIndex = index;
                                                                this.selectedPosition = new Nullable<double>(index);
                                                                this.previousInspectorEntry = this.inspectorEntry;
                                                                this.nextInspectorEntry = (Entry)attachmentEntry.Clone();
                                                            }

                                                            mbea.Handled = true;
                                                        });

                                                        PresentationSource presentationSource = PresentationSource.FromVisual(this);

                                                        if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
                                                        {
                                                            RenderOptions.SetEdgeMode(attachmentThumbnailImage, EdgeMode.Aliased);
                                                        }

                                                        this.MessageCanvas.Children.Insert(canvasIndex, attachmentThumbnailImage);

                                                        Canvas.SetLeft(attachmentThumbnailImage, imageRect.X);
                                                        Canvas.SetTop(attachmentThumbnailImage, imageRect.Y);

                                                        this.cachedAttachmentThumbnailImageDictionary.Add(i, attachmentThumbnailImage);
                                                        hashSet.Add(attachmentThumbnailImage);
                                                        updateLayoutRequired = true;
                                                    }

                                                    nextRect.Y += this.lineHeight;
                                                    canvasIndex++;
                                                }
                                            }
                                            else if (offset <= i && i < offset + length)
                                            {
                                                nextRect.Y += this.lineHeight;
                                            }
                                        }

                                        if (isImcomplete)
                                        {
                                            if (waitRequired)
                                            {
                                                waitRequired = false;
                                            }
                                        }
                                        else
                                        {
                                            waitRequired = true;
                                        }

                                        foreach (KeyValuePair<int, Image> kvp in (from kvp in this.cachedAttachmentThumbnailImageDictionary where !hashSet.Contains(kvp.Value) select kvp).ToArray())
                                        {
                                            this.MessageCanvas.Children.Remove(kvp.Value);
                                            this.cachedAttachmentThumbnailImageDictionary.Remove(kvp.Key);
                                        }

                                        foreach (KeyValuePair<int, Canvas> kvp in (from kvp in this.cachedAttachmentTextImageDictionary let canvas = kvp.Value.Parent as Canvas where canvas != null && !hashSet.Contains(kvp.Value.Parent) select new KeyValuePair<int, Canvas>(kvp.Key, canvas)).ToArray())
                                        {
                                            this.MessageCanvas.Children.Remove(kvp.Value);
                                            this.cachedAttachmentTextImageDictionary.Remove(kvp.Key);
                                        }
                                    }
                                }

                                if (this.counterScrollStep.HasValue && this.cachedCounterCanvas == null || this.counterScrollStep == null && this.cachedCounterCanvas != null)
                                {
                                    int minScrollIndex = (int)Math.Floor(Math.Min(this.sourceScrollPosition, this.targetScrollPosition));
                                    int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hashSet, point) =>
                                    {
                                        if (!hashSet.Contains(point.Y))
                                        {
                                            hashSet.Add(point.Y);
                                        }

                                        return hashSet;
                                    }).Count;
                                    int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                    int usedLines = messageLines - minScrollIndex;

                                    if (usedLines < 0)
                                    {
                                        usedLines = 0;
                                    }

                                    int actualLines = usedLines + this.messageCollection[this.historyPoint.Value].Attachments.Count;

                                    if (actualLines > maxLines)
                                    {
                                        actualLines = maxLines;
                                    }

                                    this.ScrollCanvas.Width = this.MessageCanvas.Width = this.maxMessageSize.Width;
                                    this.ScrollCanvas.Height = this.lineHeight * actualLines;
                                    this.MessageCanvas.Height = this.lineHeight * (this.counterScrollStep.HasValue ? actualLines + paddingLines + 1 : actualLines + paddingLines);

                                    if (this.cachedCounterCanvas == null)
                                    {
                                        FormattedText formattedText = new FormattedText(this.messageCollection[this.historyPoint.Value].Attachments.Count.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                                        double width = Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing);
                                        double height = formattedText.OverhangAfter > 0 ? formattedText.Height + formattedText.OverhangAfter : formattedText.Height;
                                        DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                        Image image = new Image();
                                        DrawingGroup dg = new DrawingGroup();
                                        DrawingContext dc = dg.Open();

                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                                        dc.DrawText(formattedText, new Point(-Math.Floor(formattedText.OverhangLeading), 0));
                                        dc.Close();

                                        DrawingImage di = new DrawingImage(dg);

                                        if (di.CanFreeze)
                                        {
                                            di.Freeze();
                                        }

                                        image.HorizontalAlignment = HorizontalAlignment.Left;
                                        image.VerticalAlignment = VerticalAlignment.Top;
                                        image.Source = di;
                                        image.Stretch = Stretch.None;
                                        image.Width = width;
                                        image.Height = height;

                                        RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                        dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                        dropShadowEffect.BlurRadius = 1;
                                        dropShadowEffect.Direction = 270;
                                        dropShadowEffect.ShadowDepth = 1;
                                        dropShadowEffect.Opacity = 0.5;

                                        if (dropShadowEffect.CanFreeze)
                                        {
                                            dropShadowEffect.Freeze();
                                        }

                                        this.cachedCounterCanvas = new Canvas();
                                        this.cachedCounterCanvas.HorizontalAlignment = HorizontalAlignment.Left;
                                        this.cachedCounterCanvas.VerticalAlignment = VerticalAlignment.Top;
                                        this.cachedCounterCanvas.Background = Brushes.Transparent;
                                        this.cachedCounterCanvas.Width = width;
                                        this.cachedCounterCanvas.Height = height;
                                        this.cachedCounterCanvas.Effect = dropShadowEffect;
                                        this.cachedCounterCanvas.Children.Add(image);

                                        Canvas.SetLeft(image, 0);
                                        Canvas.SetTop(image, 0);

                                        this.MessageCanvas.Children.Add(this.cachedCounterCanvas);

                                        Canvas.SetLeft(this.cachedCounterCanvas, (this.Canvas.Width - this.cachedCounterCanvas.Width) / 2 - Canvas.GetLeft(this.ScrollCanvas) + Math.Floor(formattedText.OverhangLeading));
                                        Canvas.SetTop(this.cachedCounterCanvas, this.lineHeight * (actualLines + paddingLines) + (this.lineHeight - formattedText.Height) / 2);
                                    }
                                    else
                                    {
                                        this.MessageCanvas.Children.Remove(this.cachedCounterCanvas);
                                        this.cachedCounterCanvas = null;
                                    }

                                    updateLayoutRequired = true;
                                }

                                if (paddingLines > 0)
                                {
                                    if (this.sourceScrollPosition > this.targetScrollPosition)
                                    {
                                        this.TranslateTransform.Y = -Math.Floor(this.counterScrollStep.HasValue ? this.lineHeight * paddingLines * ((this.targetScrollPosition - Math.Floor(this.targetScrollPosition)) / paddingLines) + this.lineHeight * paddingLines * (Math.Abs(this.targetScrollPosition - this.sourceScrollPosition) / paddingLines) * this.scrollIndexStep + this.lineHeight * Math.Sin(this.counterScrollStep.Value * Math.PI) : this.lineHeight * paddingLines * ((this.targetScrollPosition - Math.Floor(this.targetScrollPosition)) / paddingLines) + this.lineHeight * paddingLines * (Math.Abs(this.targetScrollPosition - this.sourceScrollPosition) / paddingLines) * this.scrollIndexStep);
                                    }
                                    else
                                    {
                                        this.TranslateTransform.Y = -Math.Floor(this.counterScrollStep.HasValue ? this.lineHeight * paddingLines * ((this.sourceScrollPosition - Math.Floor(this.sourceScrollPosition)) / paddingLines) + this.lineHeight * paddingLines * (Math.Abs(this.targetScrollPosition - this.sourceScrollPosition) / paddingLines) * this.scrollIndexStep + this.lineHeight * Math.Sin(this.counterScrollStep.Value * Math.PI) : this.lineHeight * paddingLines * ((this.sourceScrollPosition - Math.Floor(this.sourceScrollPosition)) / paddingLines) + this.lineHeight * paddingLines * (Math.Abs(this.targetScrollPosition - this.sourceScrollPosition) / paddingLines) * this.scrollIndexStep);
                                    }
                                }
                                else
                                {
                                    this.TranslateTransform.Y = this.counterScrollStep.HasValue ? -Math.Floor(this.lineHeight * Math.Sin(this.counterScrollStep.Value * Math.PI)) : 0;
                                }

                                if (isReady && this.nextHistoryPoint.HasValue)
                                {
                                    if (this.filterStep.HasValue)
                                    {
                                        this.filterStep -= 1 / (averageFrameRate / 4);

                                        if (this.filterStep <= 0)
                                        {
                                            this.filterStep = null;
                                            this.FilterImage.Visibility = Visibility.Collapsed;
                                            this.FilterImage.Width = this.FilterImage.Height = Double.NaN;
                                            this.FilterImage.Source = null;
                                            this.FilterImage.Opacity = 0;
                                        }
                                        else
                                        {
                                            this.FilterImage.Opacity = Math.Sin(this.filterStep.Value / 2 * Math.PI);
                                        }
                                    }

                                    if (this.scrollStep.HasValue)
                                    {
                                        this.scrollStep -= 1 / (averageFrameRate / 4);

                                        if (this.scrollStep <= 0)
                                        {
                                            this.scrollStep = null;
                                            this.ScrollImage.Visibility = Visibility.Collapsed;
                                            this.ScrollImage.Width = this.ScrollImage.Height = Double.NaN;
                                            this.ScrollImage.Source = null;
                                            this.ScrollImage.Opacity = 0;
                                        }
                                        else
                                        {
                                            this.ScrollImage.Opacity = Math.Sin(this.scrollStep.Value / 2 * Math.PI);
                                        }
                                    }
                                }
                                else
                                {
                                    if (this.enableFilter)
                                    {
                                        if (this.filterStep == null)
                                        {
                                            this.filterStep = new Nullable<double>(0);
                                            this.FilterImage.Visibility = Visibility.Visible;
                                            this.FilterImage.Opacity = Math.Sin(this.filterStep.Value / 2 * Math.PI);
                                        }

                                        if (this.filterStep < 1)
                                        {
                                            this.filterStep += 1 / (averageFrameRate / 4);

                                            if (this.filterStep >= 1)
                                            {
                                                this.filterStep = 1;
                                                this.FilterImage.Opacity = 1;
                                            }
                                            else
                                            {
                                                this.FilterImage.Opacity = Math.Sin(this.filterStep.Value / 2 * Math.PI);
                                            }
                                        }
                                    }
                                    else if (this.filterStep.HasValue)
                                    {
                                        this.filterStep -= 1 / (averageFrameRate / 4);

                                        if (this.filterStep <= 0)
                                        {
                                            this.filterStep = null;
                                            this.FilterImage.Visibility = Visibility.Collapsed;
                                            this.FilterImage.Width = this.FilterImage.Height = Double.NaN;
                                            this.FilterImage.Source = null;
                                            this.FilterImage.Opacity = 0;
                                        }
                                        else
                                        {
                                            this.FilterImage.Opacity = Math.Sin(this.filterStep.Value / 2 * Math.PI);
                                        }
                                    }

                                    if (this.messageIsScrollable)
                                    {
                                        if (this.enableFilter)
                                        {
                                            if (this.scrollStep.HasValue)
                                            {
                                                this.scrollStep -= 1 / (averageFrameRate / 4);

                                                if (this.scrollStep <= 0)
                                                {
                                                    this.scrollStep = null;
                                                    this.ScrollImage.Visibility = Visibility.Collapsed;
                                                    this.ScrollImage.Width = this.ScrollImage.Height = Double.NaN;
                                                    this.ScrollImage.Source = null;
                                                    this.ScrollImage.Opacity = 0;
                                                }
                                                else
                                                {
                                                    this.ScrollImage.Opacity = Math.Sin(this.scrollStep.Value / 2 * Math.PI);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (this.scrollStep == null)
                                            {
                                                this.scrollStep = new Nullable<double>(0);
                                                this.ScrollImage.Visibility = Visibility.Visible;
                                                this.ScrollImage.Opacity = Math.Sin(this.scrollStep.Value / 2 * Math.PI);
                                            }

                                            if (this.scrollStep < 1)
                                            {
                                                this.scrollStep += 1 / (averageFrameRate / 4);

                                                if (this.scrollStep >= 1)
                                                {
                                                    this.scrollStep = 1;
                                                    this.ScrollImage.Opacity = 1;
                                                }
                                                else
                                                {
                                                    this.ScrollImage.Opacity = Math.Sin(this.scrollStep.Value / 2 * Math.PI);
                                                }
                                            }
                                        }
                                    }
                                    else if (this.scrollStep.HasValue)
                                    {
                                        this.scrollStep -= 1 / (averageFrameRate / 4);

                                        if (this.scrollStep <= 0)
                                        {
                                            this.scrollStep = null;
                                            this.ScrollImage.Visibility = Visibility.Collapsed;
                                            this.ScrollImage.Width = this.ScrollImage.Height = Double.NaN;
                                            this.ScrollImage.Source = null;
                                            this.ScrollImage.Opacity = 0;
                                        }
                                        else
                                        {
                                            this.ScrollImage.Opacity = Math.Sin(this.scrollStep.Value / 2 * Math.PI);
                                        }
                                    }
                                }

                                if (this.enableFilter && this.filterStep.HasValue)
                                {
                                    if (this.previousThresholdScore == this.thresholdScore && this.thresholdQueue.Count > 0)
                                    {
                                        do
                                        {
                                            this.thresholdScore += this.thresholdQueue.Dequeue();
                                        } while (this.thresholdQueue.Count > 0);
                                    }

                                    if (this.previousThresholdScore != this.thresholdScore || this.FilterImage.Source == null)
                                    {
                                        int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hashSet, point) =>
                                        {
                                            if (!hashSet.Contains(point.Y))
                                            {
                                                hashSet.Add(point.Y);
                                            }

                                            return hashSet;
                                        }).Count;
                                        double totalMessageHeight = this.lineHeight * (messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count);
                                        int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                        int usedLines = messageLines - (int)Math.Floor(Math.Min(this.sourceScrollPosition, this.targetScrollPosition));
                                        int maxVisibleLines = this.numberOfLines;
                                        double maxVisibleHeight = 0;

                                        if (usedLines < 0)
                                        {
                                            usedLines = 0;
                                        }

                                        int actualLines = usedLines + this.messageCollection[this.historyPoint.Value].Attachments.Count;

                                        if (actualLines > maxLines)
                                        {
                                            actualLines = maxLines;
                                        }

                                        double height = this.lineHeight * actualLines;

                                        if (this.Canvas.Height - this.baseFooterHeight < Canvas.GetTop(this.ScrollCanvas) + height)
                                        {
                                            height = this.Canvas.Height - this.baseFooterHeight - Canvas.GetTop(this.ScrollCanvas);
                                        }

                                        height -= 14;

                                        if (height > 0)
                                        {
                                            maxVisibleHeight = height;
                                        }

                                        if (this.previousThresholdScore != this.thresholdScore)
                                        {
                                            this.thresholdScoreStep += 1 / (averageFrameRate / 4);

                                            if (this.thresholdScoreStep >= 1)
                                            {
                                                this.thresholdScoreStep = 0;
                                                this.previousThresholdScore = this.thresholdScore;
                                            }
                                        }

                                        Size blockSize = new Size(3, maxVisibleHeight);
                                        Rect sourceRect = new Rect(Canvas.GetLeft(this.FilterImage), Canvas.GetTop(this.FilterImage) + (blockSize.Height - 1) * (this.previousThresholdScore - this.minScore) / (this.maxScore - this.minScore), blockSize.Width, 1);
                                        Rect targetRect = new Rect(Canvas.GetLeft(this.FilterImage), Canvas.GetTop(this.FilterImage) + (blockSize.Height - 1) * (this.thresholdScore - this.minScore) / (this.maxScore - this.minScore), blockSize.Width, 1);
                                        Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                        GeometryGroup gg = new GeometryGroup();
                                        DrawingGroup dg = new DrawingGroup();
                                        DrawingContext dc = dg.Open();
                                        GuidelineSet gs1 = new GuidelineSet();

                                        gs1.GuidelinesX.Add(0);
                                        gs1.GuidelinesX.Add(blockSize.Width);
                                        gs1.GuidelinesY.Add(0);
                                        gs1.GuidelinesY.Add(blockSize.Height);

                                        dc.PushGuidelineSet(gs1);
                                        dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(color.A * 25 / 100), color.R, color.G, color.B)), new Size(3, 3)), null, new Rect(0, 0, blockSize.Width, blockSize.Height));
                                        dc.Pop();

                                        if (this.maxScore - this.minScore != 0)
                                        {
                                            Rect filterOverlayRect = new Rect(new Point(0, sourceRect.Y - Canvas.GetTop(this.FilterImage)), sourceRect.Size);
                                            GuidelineSet gs2 = new GuidelineSet();

                                            filterOverlayRect.Y += (targetRect.Y - sourceRect.Y) * this.thresholdScoreStep;

                                            gs2.GuidelinesX.Add(filterOverlayRect.X);
                                            gs2.GuidelinesX.Add(filterOverlayRect.Width);
                                            gs2.GuidelinesY.Add(filterOverlayRect.Y);
                                            gs2.GuidelinesY.Add(filterOverlayRect.Height);

                                            dc.PushGuidelineSet(gs2);
                                            dc.DrawRectangle(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, null, filterOverlayRect);
                                        }

                                        dc.Close();

                                        DrawingImage di = new DrawingImage(dg);

                                        if (di.CanFreeze)
                                        {
                                            di.Freeze();
                                        }

                                        this.FilterImage.Source = di;
                                        this.FilterImage.Width = blockSize.Width;
                                        this.FilterImage.Height = blockSize.Height;
                                    }
                                }

                                if ((isScrolled || this.ScrollImage.Source == null) && this.scrollStep.HasValue)
                                {
                                    int messageLines = inlinePointList.Value.Aggregate<Point, HashSet<double>>(new HashSet<double>(), (hashSet, point) =>
                                    {
                                        if (!hashSet.Contains(point.Y))
                                        {
                                            hashSet.Add(point.Y);
                                        }

                                        return hashSet;
                                    }).Count;
                                    double totalMessageHeight = (messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count) * this.lineHeight;
                                    int maxLines = messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count < this.numberOfLines ? messageLines + this.messageCollection[this.historyPoint.Value].Attachments.Count : this.numberOfLines;
                                    int usedLines = messageLines - (int)Math.Floor(Math.Min(this.sourceScrollPosition, this.targetScrollPosition));
                                    int maxVisibleLines = this.numberOfLines;
                                    double maxVisibleHeight = 0;

                                    if (usedLines < 0)
                                    {
                                        usedLines = 0;
                                    }

                                    int actualLines = usedLines + this.messageCollection[this.historyPoint.Value].Attachments.Count;

                                    if (actualLines > maxLines)
                                    {
                                        actualLines = maxLines;
                                    }

                                    double height = this.lineHeight * actualLines;

                                    if (this.Canvas.Height - this.baseFooterHeight < Canvas.GetTop(this.ScrollCanvas) + height)
                                    {
                                        height = this.Canvas.Height - this.baseFooterHeight - Canvas.GetTop(this.ScrollCanvas);
                                    }

                                    height -= 14;

                                    if (height > 0)
                                    {
                                        maxVisibleHeight = height;
                                    }

                                    double y = this.baseHeaderHeight + 2;
                                    double previousY = y;
                                    Size size = new Size(3, maxVisibleHeight);
                                    double remainingHeight = 0;

                                    if (this.messageCollection[this.historyPoint.Value].HasAttachments)
                                    {
                                        messageLines += this.messageCollection[this.historyPoint.Value].Attachments.Count;
                                    }

                                    if (messageLines > maxVisibleLines)
                                    {
                                        size.Height = size.Height * maxVisibleLines / messageLines;
                                        remainingHeight = maxVisibleHeight - size.Height;

                                        if (this.targetScrollPosition >= 0)
                                        {
                                            y += remainingHeight * this.targetScrollPosition / (messageLines - maxVisibleLines);
                                            previousY += remainingHeight * this.sourceScrollPosition / (messageLines - maxVisibleLines);
                                        }
                                    }

                                    if (this.ScrollImage.Source == null)
                                    {
                                        GeometryGroup gg = new GeometryGroup();
                                        GuidelineSet gs = new GuidelineSet();
                                        DrawingGroup dg = new DrawingGroup();
                                        DrawingContext dc = dg.Open();

                                        gs.GuidelinesX.Add(0);
                                        gs.GuidelinesX.Add(size.Width);
                                        gs.GuidelinesY.Add(0);
                                        gs.GuidelinesY.Add(size.Height);

                                        dc.PushGuidelineSet(gs);
                                        dc.DrawRectangle(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, null, new Rect(0, 0, size.Width, size.Height));
                                        dc.Close();

                                        DrawingImage di = new DrawingImage(dg);

                                        if (di.CanFreeze)
                                        {
                                            di.Freeze();
                                        }

                                        this.ScrollImage.Source = di;
                                        this.ScrollImage.Width = size.Width;
                                        this.ScrollImage.Height = size.Height;
                                    }

                                    Canvas.SetTop(this.ScrollImage, Math.Min(previousY, y) + Math.Abs(y - previousY) * this.scrollIndexStep);
                                }

                                if (updateLayoutRequired)
                                {
                                    this.MessageCanvas.UpdateLayout();
                                }

                                if (isReady && this.nextHistoryPoint.HasValue && this.liftStep.HasValue)
                                {
                                    this.liftStep -= 1 / (averageFrameRate / 4);

                                    if (this.liftStep <= 0)
                                    {
                                        this.liftStep = null;
                                        this.upBlinkStep = this.downBlinkStep = 0;
                                        this.UpImage.Visibility = this.DownImage.Visibility = Visibility.Collapsed;
                                        this.UpImage.Source = this.DownImage.Source = null;
                                        this.UpImage.Opacity = this.DownImage.Opacity = 0;
                                    }
                                    else
                                    {
                                        this.UpImage.Opacity = this.DownImage.Opacity = Math.Sin(this.liftStep.Value / 2 * Math.PI);
                                    }
                                }
                                else if (!isReady || !this.nextHistoryPoint.HasValue)
                                {
                                    if (this.filterStep.HasValue || this.scrollStep.HasValue || this.selectedPosition.HasValue)
                                    {
                                        if (this.liftStep == null)
                                        {
                                            this.liftStep = new Nullable<double>(0);
                                            this.UpImage.Visibility = this.DownImage.Visibility = Visibility.Visible;
                                            this.UpImage.Opacity = this.DownImage.Opacity = Math.Sin(this.liftStep.Value / 2 * Math.PI);
                                        }

                                        if (this.liftStep < 1)
                                        {
                                            this.liftStep += 1 / (averageFrameRate / 4);

                                            if (this.liftStep >= 1)
                                            {
                                                this.liftStep = 1;
                                                this.UpImage.Opacity = this.DownImage.Opacity = 1;
                                            }
                                            else
                                            {
                                                this.UpImage.Opacity = this.DownImage.Opacity = Math.Sin(this.liftStep.Value / 2 * Math.PI);
                                            }
                                        }
                                    }
                                    else if (this.liftStep.HasValue)
                                    {
                                        this.liftStep -= 1 / (averageFrameRate / 4);

                                        if (this.liftStep <= 0)
                                        {
                                            this.liftStep = null;
                                            this.upBlinkStep = this.downBlinkStep = 0;
                                            this.UpImage.Visibility = this.DownImage.Visibility = Visibility.Collapsed;
                                            this.UpImage.Source = this.DownImage.Source = null;
                                            this.UpImage.Opacity = this.DownImage.Opacity = 0;
                                        }
                                        else
                                        {
                                            this.UpImage.Opacity = this.DownImage.Opacity = Math.Sin(this.liftStep.Value / 2 * Math.PI);
                                        }
                                    }
                                }

                                if (this.liftStep.HasValue && (this.upIsHover || this.upBlinkStep > 0 || this.UpImage.Source == null))
                                {
                                    Pen pen = new Pen();

                                    if (this.upIsHover || this.upBlinkStep > 0)
                                    {
                                        this.upBlinkStep += 1 / (averageFrameRate / 2);

                                        if (this.upBlinkStep >= 1)
                                        {
                                            this.upBlinkStep = 0;
                                            pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                        }
                                        else
                                        {
                                            Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;

                                            pen.Brush = new SolidColorBrush(Color.FromArgb((byte)(color.A + (this.linkColor.A - color.A) * Math.Sin(this.upBlinkStep * Math.PI)), (byte)(color.R + (this.linkColor.R - color.R) * Math.Sin(this.upBlinkStep * Math.PI)), (byte)(color.G + (this.linkColor.G - color.G) * Math.Sin(this.upBlinkStep * Math.PI)), (byte)(color.B + (this.linkColor.B - color.B) * Math.Sin(this.upBlinkStep * Math.PI))));
                                        }
                                    }
                                    else
                                    {
                                        pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                    }

                                    pen.Thickness = 2;
                                    pen.StartLineCap = PenLineCap.Square;
                                    pen.EndLineCap = PenLineCap.Square;

                                    DrawingGroup upDrawingGroup = new DrawingGroup();
                                    GuidelineSet upGuidelineSet = new GuidelineSet();

                                    upDrawingGroup.Children.Add(new GeometryDrawing(null, pen, CreateUpGeometry(new Rect(0, 0, 6, 3))));

                                    upGuidelineSet.GuidelinesX.Add(upDrawingGroup.Bounds.X);
                                    upGuidelineSet.GuidelinesX.Add(upDrawingGroup.Bounds.X + Math.Ceiling(upDrawingGroup.Bounds.Width));
                                    upGuidelineSet.GuidelinesY.Add(upDrawingGroup.Bounds.Y);
                                    upGuidelineSet.GuidelinesY.Add(upDrawingGroup.Bounds.Y + Math.Ceiling(upDrawingGroup.Bounds.Height));

                                    upDrawingGroup.GuidelineSet = upGuidelineSet;
                                    upDrawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(upDrawingGroup.Bounds.X, upDrawingGroup.Bounds.Y, Math.Ceiling(upDrawingGroup.Bounds.Width), Math.Ceiling(upDrawingGroup.Bounds.Height)))));

                                    DrawingImage upDrawingImage = new DrawingImage(upDrawingGroup);

                                    if (upDrawingImage.CanFreeze)
                                    {
                                        upDrawingImage.Freeze();
                                    }

                                    this.UpImage.Source = upDrawingImage;
                                    this.UpImage.Width = Math.Ceiling(upDrawingGroup.Bounds.Width);
                                    this.UpImage.Height = Math.Ceiling(upDrawingGroup.Bounds.Height);
                                }

                                if (this.liftStep.HasValue && (this.downIsHover || this.downBlinkStep > 0 || this.DownImage.Source == null))
                                {
                                    Pen pen = new Pen();

                                    if (this.downIsHover || this.downBlinkStep > 0)
                                    {
                                        this.downBlinkStep += 1 / (averageFrameRate / 2);

                                        if (this.downBlinkStep >= 1)
                                        {
                                            this.downBlinkStep = 0;
                                            pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                        }
                                        else
                                        {
                                            Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;

                                            pen.Brush = new SolidColorBrush(Color.FromArgb((byte)(color.A + (this.linkColor.A - color.A) * Math.Sin(this.downBlinkStep * Math.PI)), (byte)(color.R + (this.linkColor.R - color.R) * Math.Sin(this.downBlinkStep * Math.PI)), (byte)(color.G + (this.linkColor.G - color.G) * Math.Sin(this.downBlinkStep * Math.PI)), (byte)(color.B + (this.linkColor.B - color.B) * Math.Sin(this.downBlinkStep * Math.PI))));
                                        }
                                    }
                                    else
                                    {
                                        pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                    }

                                    pen.Thickness = 2;
                                    pen.StartLineCap = PenLineCap.Square;
                                    pen.EndLineCap = PenLineCap.Square;

                                    DrawingGroup downDrawingGroup = new DrawingGroup();
                                    GuidelineSet downGuidelineSet = new GuidelineSet();

                                    downDrawingGroup.Children.Add(new GeometryDrawing(null, pen, CreateDownGeometry(new Rect(0, 0, 6, 3))));

                                    downGuidelineSet.GuidelinesX.Add(downDrawingGroup.Bounds.X);
                                    downGuidelineSet.GuidelinesX.Add(downDrawingGroup.Bounds.X + Math.Ceiling(downDrawingGroup.Bounds.Width));
                                    downGuidelineSet.GuidelinesY.Add(downDrawingGroup.Bounds.Y);
                                    downGuidelineSet.GuidelinesY.Add(downDrawingGroup.Bounds.Y + Math.Ceiling(downDrawingGroup.Bounds.Height));

                                    downDrawingGroup.GuidelineSet = downGuidelineSet;
                                    downDrawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(downDrawingGroup.Bounds.X, downDrawingGroup.Bounds.Y, Math.Ceiling(downDrawingGroup.Bounds.Width), Math.Ceiling(downDrawingGroup.Bounds.Height)))));

                                    DrawingImage downDrawingImage = new DrawingImage(downDrawingGroup);

                                    if (downDrawingImage.CanFreeze)
                                    {
                                        downDrawingImage.Freeze();
                                    }

                                    this.DownImage.Source = downDrawingImage;
                                    this.DownImage.Width = Math.Ceiling(downDrawingGroup.Bounds.Width);
                                    this.DownImage.Height = Math.Ceiling(downDrawingGroup.Bounds.Height);
                                }

                                if (this.targetSize.Width == this.Canvas.Width && this.targetSize.Height == this.Canvas.Height && this.inspectorFadeStep == null && this.inspectorEntry != this.nextInspectorEntry && this.nextInspectorEntry != null)
                                {
                                    Size size = GetBalloonSize(this.messageCollection[this.historyPoint.Value], ref this.messageIsScrollable);

                                    this.inspectorEntry = this.nextInspectorEntry;

                                    size.Height += GetInspectorSize(this.inspectorEntry).Height - 12;

                                    if (size.Width != this.targetSize.Width || size.Height != this.targetSize.Height)
                                    {
                                        this.targetSize = size;

                                        if (this.targetSize.Width != this.Canvas.Width || this.targetSize.Height != this.Canvas.Height)
                                        {
                                            this.sourceSize.Width = this.Canvas.Width;
                                            this.sourceSize.Height = this.Canvas.Height;
                                            this.resizeStep = 0;
                                        }
                                    }
                                }

                                if (this.targetSize.Width == this.Canvas.Width && this.targetSize.Height == this.Canvas.Height)
                                {
                                    if (this.inspectorEntry == this.nextInspectorEntry && this.inspectorEntry != null)
                                    {
                                        if (this.inspectorFadeStep == null)
                                        {
                                            if (this.inspectorEntry.Image == null)
                                            {
                                                this.inspectorEntry.NextImage();
                                            }

                                            if (this.inspectorEntry.Image != null)
                                            {
                                                if (!this.imageDictionary.ContainsKey(this.inspectorEntry.Image) && !this.imageUriHashSet.Contains(this.inspectorEntry.Image))
                                                {
                                                    this.imageUriHashSet.Add(this.inspectorEntry.Image);

                                                    UpdateImage(this.inspectorEntry.Image, false);
                                                }

                                                this.imageUriQueue.Enqueue(this.inspectorEntry.Image);
                                            }

                                            if (this.inspectorEntry.HasMultipleImages)
                                            {
                                                this.switchTimer.Start();
                                            }
                                            else
                                            {
                                                const double space = 10;
                                                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                                double x = 0;

                                                foreach (string tag in this.inspectorEntry.Tags)
                                                {
                                                    Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                                    StringBuilder lineStringBuilder = new StringBuilder();

                                                    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(tag, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                                    {
                                                        dictionary.Add(match.Index, match.Length);
                                                    }

                                                    if (x + space + Math.Ceiling(new FormattedText(tag, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && x != 0)
                                                    {
                                                        this.switchTimer.Start();

                                                        break;
                                                    }

                                                    for (int i = 0; i < tag.Length; i++)
                                                    {
                                                        int length;

                                                        if (dictionary.TryGetValue(i, out length) && x + space + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), tag.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74 && lineStringBuilder.Length > 0)
                                                        {
                                                            this.switchTimer.Start();

                                                            break;
                                                        }

                                                        lineStringBuilder.Append(tag[i]);

                                                        if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                        {
                                                            this.switchTimer.Start();

                                                            break;
                                                        }
                                                        else if (lineStringBuilder.Length > 0 && x + space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74)
                                                        {
                                                            this.switchTimer.Start();

                                                            break;
                                                        }
                                                    }

                                                    if (this.switchTimer.IsEnabled)
                                                    {
                                                        break;
                                                    }

                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        x += space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace);
                                                    }
                                                }
                                            }

                                            this.inspectorFadeStep = new Nullable<double>(0);
                                            this.InspectorCanvas.Visibility = this.InspectorImage.Visibility = Visibility.Visible;
                                            this.InspectorCanvas.Opacity = this.InspectorImage.Opacity = Math.Sin(this.inspectorFadeStep.Value / 2 * Math.PI);
                                        }

                                        if (this.inspectorFadeStep < 1)
                                        {
                                            this.inspectorFadeStep += 1 / (averageFrameRate / 4);

                                            if (this.inspectorFadeStep >= 1)
                                            {
                                                this.inspectorFadeStep = 1;
                                                this.InspectorCanvas.Opacity = this.InspectorImage.Opacity = 1;
                                            }
                                            else
                                            {
                                                this.InspectorCanvas.Opacity = this.InspectorImage.Opacity = Math.Sin(this.inspectorFadeStep.Value / 2 * Math.PI);
                                            }
                                        }
                                    }
                                    else if (this.inspectorEntry != this.nextInspectorEntry && this.inspectorFadeStep.HasValue)
                                    {
                                        this.inspectorFadeStep -= 1 / (averageFrameRate / 4);

                                        if (this.inspectorFadeStep <= 0)
                                        {
                                            this.titleIsHover = subtitleIsHover = this.imageIsHover = this.authorIsHover = false;
                                            this.hoverTagIndex = null;
                                            this.imageLoadStep = 0;
                                            this.imageSlideStep = 0;
                                            this.imagePopupStep = 0;
                                            this.imageBlinkStep = 0;
                                            this.imageUriQueue.Clear();
                                            this.imageUri = null;
                                            this.cachedBitmapImage = null;
                                            this.titleScrollStep = null;
                                            this.subtitleScrollStep = null;
                                            this.authorScrollStep = null;
                                            this.tagScrollStepDictionary.Clear();
                                            this.titleIsScrollable = false;
                                            this.subtitleIsScrollable = false;
                                            this.authorIsScrollable = false;
                                            this.tagIsScrollableHashSet.Clear();
                                            this.circulationQueue.Clear();
                                            this.previousCirculationIndex = 0;
                                            this.nextCirculationIndex = 0;
                                            this.circulationStep = 0;
                                            this.mouseDownPosition = null;
                                            this.cachedTitleImageDictionary.Clear();
                                            this.cachedSubtitleImageDictionary.Clear();
                                            this.cachedAuthorImageDictionary.Clear();
                                            this.cachedTagImageDictionary.Clear();
                                            this.inspectorFadeStep = null;
                                            this.InspectorCanvas.Visibility = this.InspectorImage.Visibility = Visibility.Collapsed;
                                            this.InspectorCanvas.Width = this.InspectorCanvas.Height = this.InspectorImage.Width = this.InspectorImage.Height = Double.NaN;
                                            this.InspectorImage.Source = null;
                                            this.InspectorCanvas.Background = null;
                                            this.InspectorCanvas.Opacity = this.InspectorImage.Opacity = 0;
                                            this.InspectorCanvas.Children.Clear();
                                            this.switchTimer.Stop();
                                        }
                                        else
                                        {
                                            this.InspectorCanvas.Opacity = this.InspectorImage.Opacity = Math.Sin(this.inspectorFadeStep.Value / 2 * Math.PI);
                                        }
                                    }
                                }

                                if (this.inspectorEntry != null && this.inspectorFadeStep.HasValue)
                                {
                                    bool isCirculating = false;
                                    bool isSliding = false;
                                    bool isLoading = false;
                                    bool isPopupping = false;
                                    bool isBlinking = false;
                                    BitmapImage bitmapImage;

                                    if (this.previousCirculationIndex == this.nextCirculationIndex && this.circulationQueue.Count > 0)
                                    {
                                        do
                                        {
                                            this.nextCirculationIndex += this.circulationQueue.Dequeue();
                                        } while (this.circulationQueue.Count > 0);

                                        if (this.nextCirculationIndex < 0)
                                        {
                                            this.nextCirculationIndex = 0;
                                        }
                                        else
                                        {
                                            const double space = 10;
                                            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                            double x = 0;
                                            int lines = 0;

                                            foreach (string tag in this.inspectorEntry.Tags)
                                            {
                                                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                                FormattedText formattedText = new FormattedText(tag, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);
                                                StringBuilder lineStringBuilder = new StringBuilder();

                                                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(tag, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                                {
                                                    dictionary.Add(match.Index, match.Length);
                                                }

                                                for (int i = 0; i < tag.Length; i++)
                                                {
                                                    int length;

                                                    if (dictionary.TryGetValue(i, out length) && x + space + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), tag.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74)
                                                    {
                                                        lineStringBuilder.Clear();
                                                        x = 0;
                                                        lines++;
                                                    }

                                                    lineStringBuilder.Append(tag[i]);

                                                    if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                    {
                                                        lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                        if (lineStringBuilder.Length > 0)
                                                        {
                                                            lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                        }

                                                        x = 0;
                                                        lines++;
                                                    }
                                                    else if (lineStringBuilder.Length > 0 && x + space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.baseWidth - 74)
                                                    {
                                                        if (lineStringBuilder.Length - 1 > 0)
                                                        {
                                                            lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                        }

                                                        x = 0;
                                                        lines++;
                                                    }
                                                }

                                                if (lineStringBuilder.Length > 0)
                                                {
                                                    double width = space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace);

                                                    if (x + width > this.baseWidth - 74)
                                                    {
                                                        x = width;
                                                        lines++;
                                                    }
                                                    else
                                                    {
                                                        x += width;
                                                    }
                                                }
                                            }

                                            if (this.nextCirculationIndex > lines)
                                            {
                                                this.nextCirculationIndex = 0;
                                            }
                                        }
                                    }

                                    if (this.previousCirculationIndex != this.nextCirculationIndex)
                                    {
                                        this.circulationStep += 1 / (averageFrameRate / 2);

                                        if (this.circulationStep >= 1)
                                        {
                                            this.circulationStep = 0;
                                            this.previousCirculationIndex = this.nextCirculationIndex;
                                        }

                                        isCirculating = true;
                                    }

                                    if (this.inspectorEntry == this.nextInspectorEntry)
                                    {
                                        if (this.imageUriQueue.Count > 0 && (this.imageSlideStep == 0 || this.imageSlideStep == 1))
                                        {
                                            this.imageUri = this.imageUriQueue.Dequeue();
                                            this.imageSlideStep = 0;
                                        }

                                        if (this.imageUri == null)
                                        {
                                            bitmapImage = null;

                                            if (this.cachedBitmapImage != null && this.imagePopupStep > 0)
                                            {
                                                this.imagePopupStep -= 1 / (averageFrameRate / 4);

                                                if (this.imagePopupStep < 0)
                                                {
                                                    this.imagePopupStep = 0;
                                                }

                                                isSliding = true;
                                            }
                                        }
                                        else if (this.imageDictionary.TryGetValue(this.imageUri, out bitmapImage))
                                        {
                                            if (this.cachedBitmapImage != bitmapImage)
                                            {
                                                if (this.imagePopupStep > 0)
                                                {
                                                    this.imagePopupStep -= 1 / (averageFrameRate / 4);

                                                    if (this.imagePopupStep < 0)
                                                    {
                                                        this.imagePopupStep = 0;
                                                    }

                                                    isSliding = true;
                                                }
                                                else if (this.imagePopupStep == 0 && bitmapImage != null)
                                                {
                                                    this.imageSlideStep += 1 / (averageFrameRate / 2);

                                                    if (this.imageSlideStep >= 1)
                                                    {
                                                        this.imageSlideStep = 1;
                                                        this.cachedBitmapImage = bitmapImage;
                                                    }
                                                    else
                                                    {
                                                        isLoading = true;
                                                    }

                                                    isSliding = true;
                                                }
                                            }
                                        }
                                        else if (this.cachedBitmapImage != null && this.imagePopupStep > 0)
                                        {
                                            this.imagePopupStep -= 1 / (averageFrameRate / 4);

                                            if (this.imagePopupStep < 0)
                                            {
                                                this.imagePopupStep = 0;
                                            }

                                            isSliding = true;
                                        }
                                    }
                                    else
                                    {
                                        bitmapImage = null;

                                        if (this.cachedBitmapImage != null && this.imagePopupStep > 0)
                                        {
                                            this.imagePopupStep -= 1 / (averageFrameRate / 4);

                                            if (this.imagePopupStep < 0)
                                            {
                                                this.imagePopupStep = 0;
                                            }

                                            isSliding = true;
                                        }
                                    }

                                    isPopupping = isSliding;

                                    if (!isSliding && bitmapImage != null)
                                    {
                                        if (this.imageIsHover)
                                        {
                                            if (this.imagePopupStep < 1)
                                            {
                                                this.imagePopupStep += 1 / (averageFrameRate / 4);

                                                if (this.imagePopupStep > 1)
                                                {
                                                    this.imagePopupStep = 1;
                                                }

                                                isPopupping = true;
                                            }
                                        }
                                        else
                                        {
                                            if (this.imagePopupStep > 0)
                                            {
                                                this.imagePopupStep -= 1 / (averageFrameRate / 4);

                                                if (this.imagePopupStep < 0)
                                                {
                                                    this.imagePopupStep = 0;
                                                }

                                                isPopupping = true;
                                            }
                                        }
                                    }

                                    if (this.imageSlideStep < 1 && this.imageUri != null)
                                    {
                                        isLoading = true;
                                    }

                                    if (isLoading)
                                    {
                                        this.imageLoadStep += 1 / (averageFrameRate / 2);

                                        if (this.imageLoadStep >= 1)
                                        {
                                            this.imageLoadStep = 0;
                                        }
                                    }

                                    if (!(this.imageUri == null && bitmapImage == null && this.cachedBitmapImage == null) && this.imageIsHover && this.inspectorEntry.HasMultipleImages || this.imageBlinkStep > 0)
                                    {
                                        this.imageBlinkStep += 1 / (averageFrameRate / 2);

                                        if (this.imageBlinkStep >= 1)
                                        {
                                            this.imageBlinkStep = 0;
                                        }

                                        isBlinking = true;
                                    }

                                    if (this.InspectorCanvas.Children.Count == 0 || isSliding)
                                    {
                                        const double radiusX = 5;
                                        const double radiusY = 5;
                                        Rect imageRect = new Rect(8, 0, 32, 32);
                                        Size size = new Size(this.baseWidth - 12, this.Canvas.Height - Canvas.GetTop(this.ScrollCanvas) - this.ScrollCanvas.Height - this.baseFooterHeight + 12);
                                        SolidColorBrush brush = new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 25 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B));
                                        DrawingGroup dg = new DrawingGroup();
                                        GuidelineSet gs = new GuidelineSet();

                                        gs.GuidelinesX.Add(0);
                                        gs.GuidelinesX.Add(3);
                                        gs.GuidelinesY.Add(0);
                                        gs.GuidelinesY.Add(3);

                                        dg.GuidelineSet = gs;
                                        dg.Children.Add(new GeometryDrawing(brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
                                        dg.Children.Add(new GeometryDrawing(brush, null, new RectangleGeometry(new Rect(2, 1, 1, 1))));
                                        dg.Children.Add(new GeometryDrawing(brush, null, new RectangleGeometry(new Rect(1, 2, 1, 1))));

                                        PresentationSource presentationSource = PresentationSource.FromVisual(this);

                                        if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
                                        {
                                            RenderOptions.SetEdgeMode(dg, EdgeMode.Aliased);
                                        }

                                        DrawingBrush drawingBrush = new DrawingBrush(dg);

                                        drawingBrush.TileMode = TileMode.Tile;
                                        drawingBrush.ViewportUnits = BrushMappingMode.Absolute;
                                        drawingBrush.Viewport = new Rect(0, 0, 3, 3);
                                        drawingBrush.Stretch = Stretch.None;

                                        if (drawingBrush.CanFreeze)
                                        {
                                            drawingBrush.Freeze();
                                        }

                                        double step = Math.Sin(this.imageSlideStep / 2 * Math.PI);
                                        StreamGeometry streamGeometry = new StreamGeometry();

                                        streamGeometry.FillRule = FillRule.Nonzero;

                                        using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                                        {
                                            streamGeometryContext.BeginFigure(new Point(0, imageRect.Height / 2), true, true);
                                            streamGeometryContext.LineTo(new Point(imageRect.X, imageRect.Height / 2), true, false);
                                            streamGeometryContext.LineTo(new Point(imageRect.X, imageRect.Height / 2 * step), true, false);
                                            streamGeometryContext.LineTo(new Point(imageRect.X + imageRect.Width, imageRect.Height / 2 * step), true, false);
                                            streamGeometryContext.LineTo(new Point(imageRect.X + imageRect.Width, imageRect.Height / 2), true, false);
                                            streamGeometryContext.LineTo(new Point(size.Width, imageRect.Height / 2), true, false);
                                            streamGeometryContext.LineTo(new Point(size.Width, size.Height - radiusY), true, false);
                                            streamGeometryContext.QuadraticBezierTo(new Point(size.Width, size.Height), new Point(size.Width - radiusX, size.Height), true, false);
                                            streamGeometryContext.LineTo(new Point(radiusX, size.Height), true, false);
                                            streamGeometryContext.QuadraticBezierTo(new Point(0, size.Height), new Point(0, size.Height - radiusY), true, false);
                                        }

                                        if (streamGeometry.CanFreeze)
                                        {
                                            streamGeometry.Freeze();
                                        }

                                        this.InspectorCanvas.Background = drawingBrush;
                                        this.InspectorCanvas.Width = size.Width;
                                        this.InspectorCanvas.Height = size.Height;
                                        this.InspectorCanvas.Clip = streamGeometry;

                                        Canvas.SetLeft(this.InspectorCanvas, Canvas.GetLeft(this.ScrollCanvas) - 4);
                                        Canvas.SetTop(this.InspectorCanvas, Canvas.GetTop(this.ScrollCanvas) + this.ScrollCanvas.Height);
                                    }

                                    if (this.InspectorCanvas.Children.Count == 0 || this.titleIsHover && !this.titleIsScrollable && this.titleScrollStep == null || this.titleIsScrollable || this.titleScrollStep.HasValue || this.subtitleIsHover && !this.subtitleIsScrollable && this.subtitleScrollStep == null || this.subtitleIsScrollable || this.subtitleScrollStep.HasValue || this.authorIsHover && !this.authorIsScrollable && this.authorScrollStep == null || this.authorIsScrollable || this.authorScrollStep.HasValue || isCirculating || this.hoverTagIndex.HasValue && this.tagIsScrollableHashSet.Count == 0 && this.tagScrollStepDictionary.Count == 0 || this.tagIsScrollableHashSet.Count > 0 || this.tagScrollStepDictionary.Count > 0)
                                    {
                                        const double space = 10;
                                        Rect imageRect = new Rect(8, 0, 32, 32);
                                        Size iconSize = new Size(6, 6);
                                        double baseY = imageRect.Height / 2;
                                        Point offsetPoint = new Point(16 + imageRect.Width, baseY);
                                        Point nextPoint = new Point(0, 0);
                                        string title = String.IsNullOrEmpty(this.inspectorEntry.Title) && this.inspectorEntry.Resource != null ? this.inspectorEntry.Resource.ToString() : this.inspectorEntry.Title;
                                        bool isEmpty = this.InspectorCanvas.Children.Count == 0;
                                        Size size = new Size(this.baseWidth - 12, this.Canvas.Height - Canvas.GetTop(this.ScrollCanvas) - this.ScrollCanvas.Height - this.baseFooterHeight + 12);
                                        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                                        double textWidth = size.Width - 30 - imageRect.Width;

                                        if (!String.IsNullOrEmpty(title))
                                        {
                                            List<KeyValuePair<double, FormattedText>> list = new List<KeyValuePair<double, FormattedText>>();
                                            Size textBlockSize = new Size(0, 0);
                                            bool isReseted = true;
                                            double y = 0;
                                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                            StringBuilder lineStringBuilder = new StringBuilder();
                                            bool isBreaked = false;
                                            double titleWidth = this.inspectorEntry.HasSimilarEntries ? size.Width - 30 - imageRect.Width - (space - iconSize.Width) / 2 - iconSize.Width : size.Width - 30 - imageRect.Width;

                                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(title, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                            {
                                                dictionary.Add(match.Index, match.Length);
                                            }

                                            for (int i = 0; i < title.Length; i++)
                                            {
                                                int length;

                                                if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), title.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > titleWidth && !isReseted)
                                                {
                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        FormattedText formattedText = new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        list.Add(new KeyValuePair<double, FormattedText>(y, formattedText));

                                                        if (Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > textBlockSize.Width)
                                                        {
                                                            textBlockSize.Width = Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                                        }
                                                    }

                                                    textBlockSize.Height += this.lineHeight;
                                                    lineStringBuilder.Clear();
                                                    y += lineHeight;
                                                    isBreaked = true;
                                                }

                                                lineStringBuilder.Append(title[i]);

                                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                {
                                                    lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        FormattedText formattedText = new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        list.Add(new KeyValuePair<double, FormattedText>(y, formattedText));

                                                        if (Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > textBlockSize.Width)
                                                        {
                                                            textBlockSize.Width = Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                                        }

                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                    }

                                                    textBlockSize.Height += this.lineHeight;
                                                    isReseted = true;
                                                    y += lineHeight;
                                                    isBreaked = true;
                                                }
                                                else if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > titleWidth)
                                                {
                                                    if (lineStringBuilder.Length - 1 > 0)
                                                    {
                                                        FormattedText formattedText = new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        list.Add(new KeyValuePair<double, FormattedText>(y, formattedText));

                                                        if (Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > textBlockSize.Width)
                                                        {
                                                            textBlockSize.Width = Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                                        }

                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                    }

                                                    textBlockSize.Height += this.lineHeight;
                                                    isReseted = true;
                                                    y += lineHeight;
                                                    isBreaked = true;
                                                }
                                                else
                                                {
                                                    isReseted = false;
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                FormattedText formattedText = new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                list.Add(new KeyValuePair<double, FormattedText>(y, formattedText));

                                                if (Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > textBlockSize.Width)
                                                {
                                                    textBlockSize.Width = Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                                }

                                                textBlockSize.Height += this.lineHeight;
                                                isBreaked = false;
                                            }

                                            if (isBreaked)
                                            {
                                                textBlockSize.Height += this.lineHeight;
                                            }

                                            if (this.titleScrollStep.HasValue)
                                            {
                                                double step = this.titleScrollStep.Value;

                                                if (step == 1 && this.titleIsScrollable)
                                                {
                                                    step = 0;
                                                }

                                                step += 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.titleIsHover)
                                                    {
                                                        this.titleScrollStep = 1;
                                                    }
                                                    else
                                                    {
                                                        this.titleScrollStep = null;
                                                    }
                                                }
                                                else
                                                {
                                                    this.titleScrollStep = step;
                                                }
                                            }
                                            else if (this.titleIsHover || this.titleIsScrollable)
                                            {
                                                double step = 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.titleIsHover)
                                                    {
                                                        this.titleScrollStep = new Nullable<double>(1);
                                                    }
                                                }
                                                else
                                                {
                                                    this.titleScrollStep = new Nullable<double>(step);
                                                }
                                            }

                                            if (this.titleIsScrollable)
                                            {
                                                this.titleIsScrollable = false;
                                            }

                                            for (int i = 0; i < list.Count; i++)
                                            {
                                                Image image;

                                                if (this.cachedTitleImageDictionary.TryGetValue(i, out image))
                                                {
                                                    TranslateTransform translateTransform = image.RenderTransform as TranslateTransform;

                                                    if (translateTransform != null)
                                                    {
                                                        if (this.titleScrollStep.HasValue)
                                                        {
                                                            double maxWidth = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                            StringBuilder sb = new StringBuilder();

                                                            for (int j = i + 1; j < list.Count; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            for (int j = 0; j < i; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            if (sb.Length > 0)
                                                            {
                                                                FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                                if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                {
                                                                    double w = Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                                else
                                                                {
                                                                    double w = Math.Ceiling(ft.Width);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    if (ft.OverhangTrailing < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangTrailing);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                            }

                                                            translateTransform.X = -maxWidth * Math.Sin(this.titleScrollStep.Value / 2 * Math.PI);
                                                        }
                                                        else
                                                        {
                                                            translateTransform.X = 0;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    double width1 = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                    double width2 = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                    double maxWidth = width2;
                                                    double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                    Canvas canvas = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    StringBuilder sb = new StringBuilder();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    for (int j = i + 1; j < list.Count; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    for (int j = 0; j < i; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    if (sb.Length > 0)
                                                    {
                                                        FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        height = Math.Max(height, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                        if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                        {
                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double width3 = Math.Ceiling(ft.Width);

                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                width3 -= Math.Floor(ft.OverhangLeading);

                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    }

                                                    dc.DrawText(list[i].Value, new Point(maxWidth - Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    dc.Close();

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                    canvas.Background = Brushes.Transparent;
                                                    canvas.ClipToBounds = true;
                                                    canvas.Width = width1;
                                                    canvas.Height = height;
                                                    canvas.Effect = dropShadowEffect;

                                                    this.InspectorCanvas.Children.Add(canvas);

                                                    Canvas.SetLeft(canvas, offsetPoint.X + nextPoint.X);
                                                    Canvas.SetTop(canvas, offsetPoint.Y + nextPoint.Y + list[i].Key);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image = new Image();
                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = maxWidth + width1;
                                                    image.Height = height;
                                                    image.RenderTransform = new TranslateTransform(0, 0);
                                                    image.MouseEnter += new MouseEventHandler(delegate
                                                    {
                                                        this.titleIsHover = true;
                                                    });
                                                    image.MouseLeave += new MouseEventHandler(delegate
                                                    {
                                                        this.titleIsHover = false;
                                                    });
                                                    image.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                    {
                                                        if (this.titleIsHover)
                                                        {
                                                            this.titleIsScrollable = true;
                                                        }

                                                        if (!String.IsNullOrEmpty(this.inspectorEntry.Title))
                                                        {
                                                            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                                            {
                                                                if (this.inspectorEntry.HasSimilarEntries)
                                                                {
                                                                    Script.Instance.Suggest(this.inspectorEntry.Title, this.inspectorEntry.SimilarEntries);
                                                                }
                                                                else if (this.inspectorEntry.Resource == null)
                                                                {
                                                                    Script.Instance.Search(this.inspectorEntry.Title);
                                                                }
                                                            }
                                                            else if (this.inspectorEntry.Resource == null)
                                                            {
                                                                Script.Instance.Learn(this.inspectorEntry.Title);

                                                            }
                                                            else
                                                            {
                                                                Task.Factory.StartNew(delegate (object state)
                                                                {
                                                                    NativeMethods.ShellExecute(IntPtr.Zero, "open", (string)state, null, null, 1);
                                                                }, this.inspectorEntry.Resource.ToString());
                                                            }
                                                        }

                                                        mbea.Handled = true;
                                                    });

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    canvas.Children.Add(image);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);

                                                    this.cachedTitleImageDictionary.Add(i, image);
                                                }
                                            }

                                            if (this.inspectorEntry.HasSimilarEntries && isEmpty)
                                            {
                                                Image plusImage = new Image();
                                                Pen pen = new Pen(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, 2);
                                                DrawingGroup plusDrawingGroup = new DrawingGroup();
                                                GuidelineSet plusGuidelineSet = new GuidelineSet();

                                                pen.StartLineCap = PenLineCap.Square;
                                                pen.EndLineCap = PenLineCap.Square;

                                                plusDrawingGroup.Children.Add(new GeometryDrawing(null, pen, CreatePlusGeometry(new Rect(0, 0, iconSize.Width, iconSize.Height))));

                                                plusGuidelineSet.GuidelinesX.Add(plusDrawingGroup.Bounds.X);
                                                plusGuidelineSet.GuidelinesX.Add(plusDrawingGroup.Bounds.X + Math.Ceiling(plusDrawingGroup.Bounds.Width));
                                                plusGuidelineSet.GuidelinesY.Add(plusDrawingGroup.Bounds.Y);
                                                plusGuidelineSet.GuidelinesY.Add(plusDrawingGroup.Bounds.Y + Math.Ceiling(plusDrawingGroup.Bounds.Height));

                                                plusDrawingGroup.GuidelineSet = plusGuidelineSet;
                                                plusDrawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(plusDrawingGroup.Bounds.X, plusDrawingGroup.Bounds.Y, Math.Ceiling(plusDrawingGroup.Bounds.Width), Math.Ceiling(plusDrawingGroup.Bounds.Height)))));

                                                DrawingImage plusDrawingImage = new DrawingImage(plusDrawingGroup);

                                                if (plusDrawingImage.CanFreeze)
                                                {
                                                    plusDrawingImage.Freeze();
                                                }

                                                plusImage.HorizontalAlignment = HorizontalAlignment.Left;
                                                plusImage.VerticalAlignment = VerticalAlignment.Top;
                                                plusImage.Source = plusDrawingImage;
                                                plusImage.Stretch = Stretch.None;
                                                plusImage.Width = Math.Ceiling(plusDrawingGroup.Bounds.Width);
                                                plusImage.Height = Math.Ceiling(plusDrawingGroup.Bounds.Height);

                                                PresentationSource presentationSource = PresentationSource.FromVisual(this);

                                                if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
                                                {
                                                    RenderOptions.SetEdgeMode(plusImage, EdgeMode.Aliased);
                                                }

                                                this.InspectorCanvas.Children.Add(plusImage);

                                                Canvas.SetLeft(plusImage, offsetPoint.X + nextPoint.X + textBlockSize.Width + (space - iconSize.Width) / 2);
                                                Canvas.SetTop(plusImage, offsetPoint.Y + nextPoint.Y + (this.lineHeight - iconSize.Height) / 2);
                                            }

                                            nextPoint.Y += textBlockSize.Height;
                                        }

                                        if (this.inspectorEntry.Resource != null && this.inspectorEntry.Resource.IsAbsoluteUri)
                                        {
                                            string authority = this.inspectorEntry.Resource.Authority;
                                            List<FormattedText> list = new List<FormattedText>();
                                            StringBuilder lineStringBuilder = new StringBuilder();

                                            for (int i = 0; i < authority.Length; i++)
                                            {
                                                int safeLineLength = lineStringBuilder.Length;

                                                lineStringBuilder.Append(authority[i]);

                                                if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth && safeLineLength > 0)
                                                {
                                                    list.Add(new FormattedText(lineStringBuilder.ToString().Substring(0, safeLineLength), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip));
                                                    lineStringBuilder.Remove(0, safeLineLength);
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                list.Add(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip));
                                            }

                                            if (this.subtitleScrollStep.HasValue)
                                            {
                                                double step = this.subtitleScrollStep.Value;

                                                if (step == 1 && this.subtitleIsScrollable)
                                                {
                                                    step = 0;
                                                }

                                                step += 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.subtitleIsHover)
                                                    {
                                                        this.subtitleScrollStep = 1;
                                                    }
                                                    else
                                                    {
                                                        this.subtitleScrollStep = null;
                                                    }
                                                }
                                                else
                                                {
                                                    this.subtitleScrollStep = step;
                                                }
                                            }
                                            else if (this.subtitleIsHover || this.subtitleIsScrollable)
                                            {
                                                double step = 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.subtitleIsHover)
                                                    {
                                                        this.subtitleScrollStep = new Nullable<double>(1);
                                                    }
                                                }
                                                else
                                                {
                                                    this.subtitleScrollStep = new Nullable<double>(step);
                                                }
                                            }

                                            if (this.subtitleIsScrollable)
                                            {
                                                this.subtitleIsScrollable = false;
                                            }

                                            for (int i = 0; i < list.Count; i++)
                                            {
                                                Image image;

                                                if (this.cachedSubtitleImageDictionary.TryGetValue(i, out image))
                                                {
                                                    TranslateTransform translateTransform = image.RenderTransform as TranslateTransform;

                                                    if (translateTransform != null)
                                                    {
                                                        if (this.subtitleScrollStep.HasValue)
                                                        {
                                                            double maxWidth = list[i].WidthIncludingTrailingWhitespace > list[i].Width ? Math.Ceiling(list[i].WidthIncludingTrailingWhitespace) - Math.Floor(list[i].OverhangLeading) : list[i].OverhangTrailing < 0 ? Math.Ceiling(list[i].Width) - Math.Floor(list[i].OverhangLeading) - Math.Floor(list[i].OverhangTrailing) : Math.Ceiling(list[i].Width) - Math.Floor(list[i].OverhangLeading);
                                                            StringBuilder sb = new StringBuilder();

                                                            for (int j = i + 1; j < list.Count; j++)
                                                            {
                                                                sb.Append(list[j].Text);
                                                            }

                                                            for (int j = 0; j < i; j++)
                                                            {
                                                                sb.Append(list[j].Text);
                                                            }

                                                            if (sb.Length > 0)
                                                            {
                                                                FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                                if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                {
                                                                    double w = Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                                else
                                                                {
                                                                    double w = Math.Ceiling(ft.Width);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    if (ft.OverhangTrailing < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangTrailing);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                            }

                                                            translateTransform.X = -maxWidth * Math.Sin(this.subtitleScrollStep.Value / 2 * Math.PI);
                                                        }
                                                        else
                                                        {
                                                            translateTransform.X = 0;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    double width1 = Math.Ceiling(list[i].Width) - Math.Floor(list[i].OverhangLeading) - Math.Floor(list[i].OverhangTrailing);
                                                    double width2 = list[i].WidthIncludingTrailingWhitespace > list[i].Width ? Math.Ceiling(list[i].WidthIncludingTrailingWhitespace) - Math.Floor(list[i].OverhangLeading) : list[i].OverhangTrailing < 0 ? Math.Ceiling(list[i].Width) - Math.Floor(list[i].OverhangLeading) - Math.Floor(list[i].OverhangTrailing) : Math.Ceiling(list[i].Width) - Math.Floor(list[i].OverhangLeading);
                                                    double maxWidth = width2;
                                                    double height = list[i].OverhangAfter > 0 ? list[i].Height + list[i].OverhangAfter : list[i].Height;
                                                    Canvas canvas = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    StringBuilder sb = new StringBuilder();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    for (int j = i + 1; j < list.Count; j++)
                                                    {
                                                        sb.Append(list[j].Text);
                                                    }

                                                    for (int j = 0; j < i; j++)
                                                    {
                                                        sb.Append(list[j].Text);
                                                    }

                                                    if (sb.Length > 0)
                                                    {
                                                        FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        height = Math.Max(height, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                        if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                        {
                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i], new Point(-Math.Floor(list[i].OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i], new Point(-Math.Floor(list[i].OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double width3 = Math.Ceiling(ft.Width);

                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                width3 -= Math.Floor(ft.OverhangLeading);

                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i], new Point(-Math.Floor(list[i].OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i], new Point(-Math.Floor(list[i].OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                        dc.DrawText(list[i], new Point(-Math.Floor(list[i].OverhangLeading), 0));
                                                    }

                                                    dc.DrawText(list[i], new Point(maxWidth - Math.Floor(list[i].OverhangLeading), 0));
                                                    dc.Close();

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas.Margin = new Thickness(Math.Floor(list[i].OverhangLeading), (this.lineHeight - list[i].Height) / 2, 0, 0);
                                                    canvas.Background = Brushes.Transparent;
                                                    canvas.ClipToBounds = true;
                                                    canvas.Width = width1;
                                                    canvas.Height = height;
                                                    canvas.Effect = dropShadowEffect;

                                                    this.InspectorCanvas.Children.Add(canvas);

                                                    Canvas.SetLeft(canvas, offsetPoint.X + nextPoint.X);
                                                    Canvas.SetTop(canvas, offsetPoint.Y + nextPoint.Y);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image = new Image();
                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = maxWidth + width1;
                                                    image.Height = height;
                                                    image.RenderTransform = new TranslateTransform(0, 0);
                                                    image.MouseEnter += new MouseEventHandler(delegate
                                                    {
                                                        this.subtitleIsHover = true;
                                                    });
                                                    image.MouseLeave += new MouseEventHandler(delegate
                                                    {
                                                        this.subtitleIsHover = false;
                                                    });
                                                    image.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                    {
                                                        if (this.subtitleIsHover)
                                                        {
                                                            this.subtitleIsScrollable = true;
                                                        }

                                                        Task.Factory.StartNew(delegate (object state)
                                                        {
                                                            NativeMethods.ShellExecute(IntPtr.Zero, "open", (string)state, null, null, 1);
                                                        }, this.inspectorEntry.Resource.GetLeftPart(UriPartial.Authority).ToString());

                                                        mbea.Handled = true;
                                                    });

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    canvas.Children.Add(image);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);

                                                    this.cachedSubtitleImageDictionary.Add(i, image);
                                                }

                                                nextPoint.Y += this.lineHeight;
                                            }
                                        }

                                        if (!String.IsNullOrEmpty(this.inspectorEntry.Author))
                                        {
                                            List<KeyValuePair<double, FormattedText>> list = new List<KeyValuePair<double, FormattedText>>();
                                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                            double y = 0;
                                            double height1 = 0;
                                            StringBuilder lineStringBuilder = new StringBuilder();
                                            bool isBreaked = false;

                                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(this.inspectorEntry.Author, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                            {
                                                dictionary.Add(match.Index, match.Length);
                                            }

                                            for (int i = 0; i < this.inspectorEntry.Author.Length; i++)
                                            {
                                                int length;

                                                if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), this.inspectorEntry.Author.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth)
                                                {
                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(y, new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                    }

                                                    lineStringBuilder.Clear();
                                                    y += this.lineHeight;
                                                    height1 += this.lineHeight;
                                                    isBreaked = true;
                                                }

                                                lineStringBuilder.Append(this.inspectorEntry.Author[i]);

                                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                {
                                                    lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(y, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                    }

                                                    y += this.lineHeight;
                                                    height1 += this.lineHeight;
                                                    isBreaked = true;
                                                }
                                                else if (lineStringBuilder.Length > 0 && Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth)
                                                {
                                                    if (lineStringBuilder.Length - 1 > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(y, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                    }

                                                    y += this.lineHeight;
                                                    height1 += this.lineHeight;
                                                    isBreaked = true;
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                list.Add(new KeyValuePair<double, FormattedText>(y, new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                height1 += this.lineHeight;
                                                isBreaked = false;
                                            }

                                            if (isBreaked)
                                            {
                                                height1 += this.lineHeight;
                                            }

                                            if (this.authorScrollStep.HasValue)
                                            {
                                                double step = this.authorScrollStep.Value;

                                                if (step == 1 && this.authorIsScrollable)
                                                {
                                                    step = 0;
                                                }

                                                step += 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.authorIsHover)
                                                    {
                                                        this.authorScrollStep = 1;
                                                    }
                                                    else
                                                    {
                                                        this.authorScrollStep = null;
                                                    }
                                                }
                                                else
                                                {
                                                    this.authorScrollStep = step;
                                                }
                                            }
                                            else if (this.authorIsHover || this.authorIsScrollable)
                                            {
                                                double step = 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.authorIsHover)
                                                    {
                                                        this.authorScrollStep = new Nullable<double>(1);
                                                    }
                                                }
                                                else
                                                {
                                                    this.authorScrollStep = new Nullable<double>(step);
                                                }
                                            }

                                            if (this.authorIsScrollable)
                                            {
                                                this.authorIsScrollable = false;
                                            }

                                            for (int i = 0; i < list.Count; i++)
                                            {
                                                Image image;

                                                if (this.cachedAuthorImageDictionary.TryGetValue(i, out image))
                                                {
                                                    TranslateTransform translateTransform = image.RenderTransform as TranslateTransform;

                                                    if (translateTransform != null)
                                                    {
                                                        if (this.authorScrollStep.HasValue)
                                                        {
                                                            double maxWidth = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                            StringBuilder sb = new StringBuilder();

                                                            for (int j = i + 1; j < list.Count; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            for (int j = 0; j < i; j++)
                                                            {
                                                                sb.Append(list[j].Value.Text);
                                                            }

                                                            if (sb.Length > 0)
                                                            {
                                                                FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                                if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                {
                                                                    double w = Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                                else
                                                                {
                                                                    double w = Math.Ceiling(ft.Width);

                                                                    if (ft.OverhangLeading < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangLeading);
                                                                    }

                                                                    if (ft.OverhangTrailing < 0)
                                                                    {
                                                                        w -= Math.Floor(ft.OverhangTrailing);
                                                                    }

                                                                    maxWidth += w;
                                                                }
                                                            }

                                                            translateTransform.X = -maxWidth * Math.Sin(this.authorScrollStep.Value / 2 * Math.PI);
                                                        }
                                                        else
                                                        {
                                                            translateTransform.X = 0;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    double width1 = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                    double width2 = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                    double maxWidth = width2;
                                                    double height2 = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                    Canvas canvas = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    StringBuilder sb = new StringBuilder();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    for (int j = i + 1; j < list.Count; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    for (int j = 0; j < i; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    if (sb.Length > 0)
                                                    {
                                                        FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        height2 = Math.Max(height2, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                        if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                        {
                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height2));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height2));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double width3 = Math.Ceiling(ft.Width);

                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                width3 -= Math.Floor(ft.OverhangLeading);

                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height2));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height2));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height2));
                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    }

                                                    dc.DrawText(list[i].Value, new Point(maxWidth - Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    dc.Close();

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                    canvas.Background = Brushes.Transparent;
                                                    canvas.ClipToBounds = true;
                                                    canvas.Width = width1;
                                                    canvas.Height = height2;
                                                    canvas.Effect = dropShadowEffect;

                                                    this.InspectorCanvas.Children.Add(canvas);

                                                    Canvas.SetLeft(canvas, offsetPoint.X + nextPoint.X);
                                                    Canvas.SetTop(canvas, offsetPoint.Y + nextPoint.Y + list[i].Key);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image = new Image();
                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = maxWidth + width1;
                                                    image.Height = height2;
                                                    image.RenderTransform = new TranslateTransform(0, 0);
                                                    image.MouseEnter += new MouseEventHandler(delegate
                                                    {
                                                        this.authorIsHover = true;
                                                    });
                                                    image.MouseLeave += new MouseEventHandler(delegate
                                                    {
                                                        this.authorIsHover = false;
                                                    });
                                                    image.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                    {
                                                        if (this.authorIsHover)
                                                        {
                                                            this.authorIsScrollable = true;
                                                        }

                                                        Script.Instance.Search(this.inspectorEntry.Author);

                                                        mbea.Handled = true;
                                                    });

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    canvas.Children.Add(image);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);

                                                    this.cachedAuthorImageDictionary.Add(i, image);
                                                }
                                            }

                                            nextPoint.Y += height1;
                                        }

                                        if (this.inspectorEntry.Modified.Ticks > 0)
                                        {
                                            string modified = this.inspectorEntry.Modified.ToString("G", CultureInfo.CurrentCulture);
                                            List<FormattedText> list = new List<FormattedText>();
                                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                            StringBuilder lineStringBuilder = new StringBuilder();

                                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(modified, @"\S+\s?"))
                                            {
                                                dictionary.Add(match.Index, match.Length);
                                            }

                                            for (int i = 0; i < modified.Length; i++)
                                            {
                                                int length;

                                                if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), modified.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth)
                                                {
                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip));
                                                    }

                                                    lineStringBuilder.Clear();
                                                }

                                                int safeLineLength = lineStringBuilder.Length;

                                                lineStringBuilder.Append(modified[i]);

                                                if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth && safeLineLength > 0)
                                                {
                                                    list.Add(new FormattedText(lineStringBuilder.ToString().Substring(0, safeLineLength), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip));
                                                    lineStringBuilder.Remove(0, safeLineLength);
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                list.Add(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip));
                                            }

                                            list.ForEach(delegate (FormattedText formattedText)
                                            {
                                                if (isEmpty)
                                                {
                                                    double width = Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing);
                                                    double height = formattedText.OverhangAfter > 0 ? formattedText.Height + formattedText.OverhangAfter : formattedText.Height;
                                                    Canvas canvas = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    Image image = new Image();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                                                    dc.DrawText(formattedText, new Point(-Math.Floor(formattedText.OverhangLeading), 0));
                                                    dc.Close();

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas.Margin = new Thickness(Math.Floor(formattedText.OverhangLeading), (this.lineHeight - formattedText.Height) / 2, 0, 0);
                                                    canvas.Background = Brushes.Transparent;
                                                    canvas.Width = width;
                                                    canvas.Height = height;
                                                    canvas.Effect = dropShadowEffect;
                                                    canvas.Children.Add(image);

                                                    this.InspectorCanvas.Children.Add(canvas);

                                                    Canvas.SetLeft(canvas, offsetPoint.X + nextPoint.X);
                                                    Canvas.SetTop(canvas, offsetPoint.Y + nextPoint.Y);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = width;
                                                    image.Height = height;

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);
                                                }

                                                nextPoint.Y += this.lineHeight;
                                            });
                                        }

                                        if (this.inspectorEntry.Score.HasValue)
                                        {
                                            string score = this.inspectorEntry.Score.Value.ToString("F3", CultureInfo.CurrentCulture);
                                            List<FormattedText> list = new List<FormattedText>();
                                            StringBuilder lineStringBuilder = new StringBuilder();
                                            bool isFirst = true;

                                            for (int i = 0; i < score.Length; i++)
                                            {
                                                int safeLineLength = lineStringBuilder.Length;

                                                lineStringBuilder.Append(score[i]);

                                                if (space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth && safeLineLength > 0)
                                                {
                                                    list.Add(new FormattedText(lineStringBuilder.ToString().Substring(0, safeLineLength), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip));
                                                    lineStringBuilder.Remove(0, safeLineLength);
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                list.Add(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip));
                                            }

                                            list.ForEach(delegate (FormattedText formattedText)
                                            {
                                                if (isEmpty)
                                                {
                                                    double width = Math.Ceiling(formattedText.Width) - Math.Floor(formattedText.OverhangLeading) - Math.Floor(formattedText.OverhangTrailing);
                                                    double height = formattedText.OverhangAfter > 0 ? formattedText.Height + formattedText.OverhangAfter : formattedText.Height;
                                                    Canvas canvas = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    Image image = new Image();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                                                    dc.DrawText(formattedText, new Point(-Math.Floor(formattedText.OverhangLeading), 0));
                                                    dc.Close();

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas.Margin = new Thickness(Math.Floor(formattedText.OverhangLeading), (this.lineHeight - formattedText.Height) / 2, 0, 0);
                                                    canvas.Background = Brushes.Transparent;
                                                    canvas.Width = width;
                                                    canvas.Height = height;
                                                    canvas.Effect = dropShadowEffect;

                                                    this.InspectorCanvas.Children.Add(canvas);

                                                    Canvas.SetLeft(canvas, offsetPoint.X + nextPoint.X + space / 2);
                                                    Canvas.SetTop(canvas, offsetPoint.Y + nextPoint.Y);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = width;
                                                    image.Height = height;

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    canvas.Children.Add(image);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);

                                                    if (isFirst)
                                                    {
                                                        Rectangle rectangle = new Rectangle();
                                                        Rect blockRect = new Rect(offsetPoint.X + nextPoint.X, offsetPoint.Y + nextPoint.Y + (this.lineHeight - 3) / 2, 3, 3);

                                                        rectangle.CacheMode = new BitmapCache(1);
                                                        rectangle.HorizontalAlignment = HorizontalAlignment.Left;
                                                        rectangle.VerticalAlignment = VerticalAlignment.Top;
                                                        rectangle.Fill = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                                        rectangle.Width = blockRect.Width;
                                                        rectangle.Height = blockRect.Height;

                                                        PresentationSource presentationSource = PresentationSource.FromVisual(this);

                                                        if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
                                                        {
                                                            RenderOptions.SetEdgeMode(rectangle, EdgeMode.Aliased);
                                                        }

                                                        this.InspectorCanvas.Children.Add(rectangle);

                                                        Canvas.SetLeft(rectangle, blockRect.X);
                                                        Canvas.SetTop(rectangle, blockRect.Y);

                                                        isFirst = false;
                                                    }
                                                }

                                                nextPoint.Y += this.lineHeight;
                                            });
                                        }

                                        for (int tagIndex = 0, innerTagIndex = 0, lines = 0, canvasIndex = this.InspectorCanvas.Children.Count - this.cachedTagImageDictionary.Count; tagIndex < this.inspectorEntry.Tags.Count; tagIndex++)
                                        {
                                            string tag = this.inspectorEntry.Tags[tagIndex];
                                            List<KeyValuePair<double, FormattedText>> list = new List<KeyValuePair<double, FormattedText>>();
                                            Dictionary<int, int> dictionary = new Dictionary<int, int>();
                                            StringBuilder lineStringBuilder = new StringBuilder();
                                            bool isScrollable = false;
                                            double step;
                                            Nullable<double> scrollStep = null;

                                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(tag, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                                            {
                                                dictionary.Add(match.Index, match.Length);
                                            }

                                            if (nextPoint.X + space + Math.Ceiling(new FormattedText(tag, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth && nextPoint.X != 0)
                                            {
                                                nextPoint.X = 0;
                                                lines++;
                                            }

                                            for (int i = 0; i < tag.Length; i++)
                                            {
                                                int length;

                                                if (dictionary.TryGetValue(i, out length) && nextPoint.X + space + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), tag.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth)
                                                {
                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(nextPoint.X, new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                    }

                                                    lineStringBuilder.Clear();
                                                    nextPoint.X = 0;
                                                }

                                                lineStringBuilder.Append(tag[i]);

                                                if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                                                {
                                                    lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                                                    if (lineStringBuilder.Length > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(nextPoint.X, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length);
                                                    }

                                                    nextPoint.X = 0;
                                                }
                                                else if (lineStringBuilder.Length > 0 && nextPoint.X + space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > textWidth)
                                                {
                                                    if (lineStringBuilder.Length - 1 > 0)
                                                    {
                                                        list.Add(new KeyValuePair<double, FormattedText>(nextPoint.X, new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip)));
                                                        lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                                                    }

                                                    nextPoint.X = 0;
                                                }
                                            }

                                            if (lineStringBuilder.Length > 0)
                                            {
                                                FormattedText formattedText = new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                list.Add(new KeyValuePair<double, FormattedText>(nextPoint.X, formattedText));
                                                nextPoint.X += space + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                                            }

                                            if (this.tagIsScrollableHashSet.Contains(tagIndex))
                                            {
                                                isScrollable = true;
                                                this.tagIsScrollableHashSet.Remove(tagIndex);
                                            }

                                            if (this.tagScrollStepDictionary.TryGetValue(tagIndex, out step))
                                            {
                                                if (step == 1 && isScrollable)
                                                {
                                                    step = 0;
                                                }

                                                step += 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.hoverTagIndex.HasValue && this.hoverTagIndex.Value == tagIndex)
                                                    {
                                                        scrollStep = new Nullable<double>(1);
                                                        this.tagScrollStepDictionary[tagIndex] = 1;
                                                    }
                                                    else
                                                    {
                                                        this.tagScrollStepDictionary.Remove(tagIndex);
                                                    }
                                                }
                                                else
                                                {
                                                    scrollStep = new Nullable<double>(step);
                                                    this.tagScrollStepDictionary[tagIndex] = step;
                                                }
                                            }
                                            else if (this.hoverTagIndex.HasValue && this.hoverTagIndex.Value == tagIndex || isScrollable)
                                            {
                                                step = 1 / (averageFrameRate / 2);

                                                if (step >= 1)
                                                {
                                                    if (this.hoverTagIndex.HasValue && this.hoverTagIndex.Value == tagIndex)
                                                    {
                                                        scrollStep = new Nullable<double>(1);
                                                        this.tagScrollStepDictionary.Add(tagIndex, 1);
                                                    }
                                                }
                                                else
                                                {
                                                    scrollStep = new Nullable<double>(step);
                                                    this.tagScrollStepDictionary.Add(tagIndex, step);
                                                }
                                            }

                                            for (int i = 0; i < list.Count; i++)
                                            {
                                                Image image;

                                                if (this.cachedTagImageDictionary.TryGetValue(innerTagIndex, out image))
                                                {
                                                    if (Math.Min(this.previousCirculationIndex, this.nextCirculationIndex) <= lines && lines <= Math.Max(this.previousCirculationIndex, this.nextCirculationIndex))
                                                    {
                                                        TranslateTransform translateTransform1 = image.RenderTransform as TranslateTransform;

                                                        if (translateTransform1 != null)
                                                        {
                                                            if (scrollStep.HasValue)
                                                            {
                                                                double maxWidth = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                                StringBuilder sb = new StringBuilder();

                                                                for (int j = i + 1; j < list.Count; j++)
                                                                {
                                                                    sb.Append(list[j].Value.Text);
                                                                }

                                                                for (int j = 0; j < i; j++)
                                                                {
                                                                    sb.Append(list[j].Value.Text);
                                                                }

                                                                if (sb.Length > 0)
                                                                {
                                                                    FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                                    if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                                    {
                                                                        double w = Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                        if (ft.OverhangLeading < 0)
                                                                        {
                                                                            w -= Math.Floor(ft.OverhangLeading);
                                                                        }

                                                                        maxWidth += w;
                                                                    }
                                                                    else
                                                                    {
                                                                        double w = Math.Ceiling(ft.Width);

                                                                        if (ft.OverhangLeading < 0)
                                                                        {
                                                                            w -= Math.Floor(ft.OverhangLeading);
                                                                        }

                                                                        if (ft.OverhangTrailing < 0)
                                                                        {
                                                                            w -= Math.Floor(ft.OverhangTrailing);
                                                                        }

                                                                        maxWidth += w;
                                                                    }
                                                                }

                                                                translateTransform1.X = -maxWidth * Math.Sin(scrollStep.Value / 2 * Math.PI);
                                                            }
                                                            else
                                                            {
                                                                translateTransform1.X = 0;
                                                            }
                                                        }

                                                        Canvas canvas1 = image.Parent as Canvas;

                                                        if (canvas1 != null)
                                                        {
                                                            Canvas canvas2 = canvas1.Parent as Canvas;

                                                            if (canvas2 != null)
                                                            {
                                                                if (isCirculating)
                                                                {
                                                                    double y = 0;

                                                                    if (Math.Min(this.previousCirculationIndex, this.nextCirculationIndex) <= lines && lines <= Math.Max(this.previousCirculationIndex, this.nextCirculationIndex))
                                                                    {
                                                                        if (this.previousCirculationIndex < this.nextCirculationIndex)
                                                                        {
                                                                            y = (lines - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight - (Math.Max(this.previousCirculationIndex, this.nextCirculationIndex) - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight * Math.Sin(this.circulationStep / 2 * Math.PI);
                                                                        }
                                                                        else if (this.previousCirculationIndex > this.nextCirculationIndex)
                                                                        {
                                                                            y = (lines - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight - (Math.Max(this.previousCirculationIndex, this.nextCirculationIndex) - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight * Math.Cos(this.circulationStep / 2 * Math.PI);
                                                                        }
                                                                    }

                                                                    TranslateTransform translateTransform2 = canvas2.RenderTransform as TranslateTransform;

                                                                    if (translateTransform2 != null)
                                                                    {
                                                                        translateTransform2.Y = y;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        canvasIndex++;
                                                    }
                                                    else
                                                    {
                                                        Canvas canvas1 = image.Parent as Canvas;

                                                        if (canvas1 != null)
                                                        {
                                                            Canvas canvas2 = canvas1.Parent as Canvas;

                                                            if (canvas2 != null)
                                                            {
                                                                Canvas canvas3 = canvas2.Parent as Canvas;

                                                                if (canvas3 != null)
                                                                {
                                                                    this.InspectorCanvas.Children.Remove(canvas3);
                                                                }
                                                            }
                                                        }

                                                        this.cachedTagImageDictionary.Remove(innerTagIndex);
                                                    }
                                                }
                                                else if (Math.Min(this.previousCirculationIndex, this.nextCirculationIndex) <= lines && lines <= Math.Max(this.previousCirculationIndex, this.nextCirculationIndex))
                                                {
                                                    int index = tagIndex;
                                                    double width1 = Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing);
                                                    double width2 = list[i].Value.WidthIncludingTrailingWhitespace > list[i].Value.Width ? Math.Ceiling(list[i].Value.WidthIncludingTrailingWhitespace) - Math.Floor(list[i].Value.OverhangLeading) : list[i].Value.OverhangTrailing < 0 ? Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading) - Math.Floor(list[i].Value.OverhangTrailing) : Math.Ceiling(list[i].Value.Width) - Math.Floor(list[i].Value.OverhangLeading);
                                                    double maxWidth = width2;
                                                    double height = list[i].Value.OverhangAfter > 0 ? list[i].Value.Height + list[i].Value.OverhangAfter : list[i].Value.Height;
                                                    double y = 0;
                                                    Canvas canvas1 = new Canvas();
                                                    Canvas canvas2 = new Canvas();
                                                    Canvas canvas3 = new Canvas();
                                                    DropShadowEffect dropShadowEffect = new DropShadowEffect();
                                                    Rectangle rectangle = new Rectangle();
                                                    StringBuilder sb = new StringBuilder();
                                                    DrawingGroup dg = new DrawingGroup();
                                                    DrawingContext dc = dg.Open();

                                                    for (int j = i + 1; j < list.Count; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    for (int j = 0; j < i; j++)
                                                    {
                                                        sb.Append(list[j].Value.Text);
                                                    }

                                                    if (sb.Length > 0)
                                                    {
                                                        FormattedText ft = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip);

                                                        height = Math.Max(height, ft.OverhangAfter > 0 ? ft.Height + ft.OverhangAfter : ft.Height);

                                                        if (ft.WidthIncludingTrailingWhitespace > ft.Width)
                                                        {
                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace) - Math.Floor(ft.OverhangLeading);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                maxWidth += Math.Ceiling(ft.WidthIncludingTrailingWhitespace);

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            double width3 = Math.Ceiling(ft.Width);

                                                            if (ft.OverhangLeading < 0)
                                                            {
                                                                width3 -= Math.Floor(ft.OverhangLeading);

                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2 - Math.Floor(ft.OverhangLeading), 0));
                                                            }
                                                            else
                                                            {
                                                                if (ft.OverhangTrailing < 0)
                                                                {
                                                                    width3 -= Math.Floor(ft.OverhangTrailing);
                                                                }

                                                                maxWidth += width3;

                                                                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                                dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                                dc.DrawText(ft, new Point(width2, 0));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, maxWidth + width1, height));
                                                        dc.DrawText(list[i].Value, new Point(-Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    }

                                                    dc.DrawText(list[i].Value, new Point(maxWidth - Math.Floor(list[i].Value.OverhangLeading), 0));
                                                    dc.Close();

                                                    canvas3.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas3.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas3.Background = Brushes.Transparent;
                                                    canvas3.ClipToBounds = true;
                                                    canvas3.Width = space + Math.Floor(list[i].Value.OverhangLeading) + width1;
                                                    canvas3.Height = this.lineHeight;

                                                    this.InspectorCanvas.Children.Insert(canvasIndex, canvas3);

                                                    Canvas.SetLeft(canvas3, offsetPoint.X + list[i].Key);
                                                    Canvas.SetTop(canvas3, offsetPoint.Y + nextPoint.Y);

                                                    if (Math.Min(this.previousCirculationIndex, this.nextCirculationIndex) <= lines && lines <= Math.Max(this.previousCirculationIndex, this.nextCirculationIndex))
                                                    {
                                                        if (this.previousCirculationIndex < this.nextCirculationIndex)
                                                        {
                                                            y = (lines - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight - (Math.Max(this.previousCirculationIndex, this.nextCirculationIndex) - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight * Math.Sin(this.circulationStep / 2 * Math.PI);
                                                        }
                                                        else if (this.previousCirculationIndex > this.nextCirculationIndex)
                                                        {
                                                            y = (lines - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight - (Math.Max(this.previousCirculationIndex, this.nextCirculationIndex) - Math.Min(this.previousCirculationIndex, this.nextCirculationIndex)) * this.lineHeight * Math.Cos(this.circulationStep / 2 * Math.PI);
                                                        }
                                                    }

                                                    canvas2.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas2.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas2.Background = Brushes.Transparent;
                                                    canvas2.Width = width1;
                                                    canvas2.Height = height;
                                                    canvas2.RenderTransform = new TranslateTransform(0, y);

                                                    canvas3.Children.Add(canvas2);

                                                    Canvas.SetLeft(canvas2, 0);
                                                    Canvas.SetTop(canvas2, 0);

                                                    dropShadowEffect.Color = Math.Max(Math.Max(this.textColor.R, this.textColor.G), this.textColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                                    dropShadowEffect.BlurRadius = 1;
                                                    dropShadowEffect.Direction = 270;
                                                    dropShadowEffect.ShadowDepth = 1;
                                                    dropShadowEffect.Opacity = 0.5;

                                                    if (dropShadowEffect.CanFreeze)
                                                    {
                                                        dropShadowEffect.Freeze();
                                                    }

                                                    canvas1.HorizontalAlignment = HorizontalAlignment.Left;
                                                    canvas1.VerticalAlignment = VerticalAlignment.Top;
                                                    canvas1.Margin = new Thickness(Math.Floor(list[i].Value.OverhangLeading), (this.lineHeight - list[i].Value.Height) / 2, 0, 0);
                                                    canvas1.Background = Brushes.Transparent;
                                                    canvas1.ClipToBounds = true;
                                                    canvas1.Width = width1;
                                                    canvas1.Height = height;
                                                    canvas1.Effect = dropShadowEffect;

                                                    canvas2.Children.Add(canvas1);

                                                    Canvas.SetLeft(canvas1, space / 2);
                                                    Canvas.SetTop(canvas1, 0);

                                                    DrawingImage di = new DrawingImage(dg);

                                                    if (di.CanFreeze)
                                                    {
                                                        di.Freeze();
                                                    }

                                                    image = new Image();
                                                    image.HorizontalAlignment = HorizontalAlignment.Left;
                                                    image.VerticalAlignment = VerticalAlignment.Top;
                                                    image.Source = di;
                                                    image.Stretch = Stretch.None;
                                                    image.Width = maxWidth + width1;
                                                    image.Height = height;
                                                    image.RenderTransform = new TranslateTransform(0, 0);
                                                    image.MouseEnter += new MouseEventHandler(delegate
                                                    {
                                                        this.hoverTagIndex = new Nullable<int>(index);

                                                        if (lines > 0)
                                                        {
                                                            this.switchTimer.Stop();
                                                        }
                                                    });
                                                    image.MouseLeave += new MouseEventHandler(delegate
                                                    {
                                                        this.hoverTagIndex = null;

                                                        if (lines > 0)
                                                        {
                                                            this.switchTimer.Start();
                                                        }
                                                    });
                                                    image.MouseLeftButtonDown += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                    {
                                                        if (lines > 0)
                                                        {
                                                            this.mouseDownPosition = new Nullable<Point>(PointToScreen(mbea.GetPosition(this)));

                                                            image.CaptureMouse();
                                                        }
                                                    });
                                                    image.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s, MouseButtonEventArgs mbea)
                                                    {
                                                        if (this.mouseDownPosition.HasValue)
                                                        {
                                                            Point point = PointToScreen(mbea.GetPosition(this));

                                                            if (Math.Sign(point.Y - this.mouseDownPosition.Value.Y) > 0)
                                                            {
                                                                this.circulationQueue.Enqueue(-1);
                                                            }
                                                            else if (Math.Sign(point.Y - this.mouseDownPosition.Value.Y) < 0)
                                                            {
                                                                this.circulationQueue.Enqueue(1);
                                                            }

                                                            this.mouseDownPosition = null;
                                                        }

                                                        if (image.IsMouseCaptured)
                                                        {
                                                            image.ReleaseMouseCapture();
                                                        }

                                                        if (this.hoverTagIndex.HasValue)
                                                        {
                                                            if (!this.tagIsScrollableHashSet.Contains(this.hoverTagIndex.Value))
                                                            {
                                                                this.tagIsScrollableHashSet.Add(this.hoverTagIndex.Value);
                                                            }

                                                            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                                            {
                                                                Script.Instance.Search(tag);
                                                            }
                                                            else
                                                            {
                                                                Script.Instance.Learn(tag);
                                                            }
                                                        }

                                                        mbea.Handled = true;
                                                    });

                                                    RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);

                                                    canvas1.Children.Add(image);

                                                    Canvas.SetLeft(image, 0);
                                                    Canvas.SetTop(image, 0);

                                                    if (i == 0)
                                                    {
                                                        Rect blockRect = new Rect(0, (this.lineHeight - 3) / 2, 3, 3);

                                                        rectangle.CacheMode = new BitmapCache(1);
                                                        rectangle.HorizontalAlignment = HorizontalAlignment.Left;
                                                        rectangle.VerticalAlignment = VerticalAlignment.Top;
                                                        rectangle.Fill = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                                        rectangle.Width = blockRect.Width;
                                                        rectangle.Height = blockRect.Height;

                                                        PresentationSource presentationSource = PresentationSource.FromVisual(this);

                                                        if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
                                                        {
                                                            RenderOptions.SetEdgeMode(rectangle, EdgeMode.Aliased);
                                                        }

                                                        canvas2.Children.Add(rectangle);

                                                        Canvas.SetLeft(rectangle, blockRect.X);
                                                        Canvas.SetTop(rectangle, blockRect.Y);
                                                    }

                                                    this.cachedTagImageDictionary.Add(innerTagIndex, image);
                                                    canvasIndex++;
                                                }

                                                if (i != list.Count - 1)
                                                {
                                                    lines++;
                                                }

                                                innerTagIndex++;
                                            }
                                        }

                                        if (isEmpty)
                                        {
                                            this.InspectorCanvas.UpdateLayout();
                                        }
                                    }

                                    if (this.InspectorImage.Source == null || isLoading || isSliding || isPopupping || isBlinking)
                                    {
                                        Size imageSize = new Size(32, 32);

                                        if (this.imageUri == null && this.cachedBitmapImage == null)
                                        {
                                            Pen pen = new Pen(Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White, 1);
                                            DrawingGroup dg = new DrawingGroup();
                                            DrawingContext dc = dg.Open();
                                            GeometryGroup gg = new GeometryGroup();
                                            GuidelineSet gs1 = new GuidelineSet();
                                            GuidelineSet gs2 = new GuidelineSet();

                                            gg.FillRule = FillRule.Nonzero;
                                            gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, pen.Thickness / 2), new Point(imageSize.Width - pen.Thickness / 2, imageSize.Height - pen.Thickness / 2)));
                                            gg.Children.Add(new LineGeometry(new Point(pen.Thickness / 2, imageSize.Height - pen.Thickness / 2), new Point(imageSize.Width - pen.Thickness / 2, pen.Thickness / 2)));

                                            gs1.GuidelinesX.Add(0);
                                            gs1.GuidelinesX.Add(imageSize.Width);
                                            gs1.GuidelinesY.Add(0);
                                            gs1.GuidelinesY.Add(imageSize.Height);

                                            dc.PushGuidelineSet(gs1);
                                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                            dc.DrawGeometry(null, pen, gg);
                                            dc.DrawRectangle(null, pen, new Rect(pen.Thickness / 2, pen.Thickness / 2, imageSize.Width - pen.Thickness, imageSize.Height - pen.Thickness));
                                            dc.Close();

                                            gs2.GuidelinesX.Add(dg.Bounds.X);
                                            gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                            gs2.GuidelinesY.Add(dg.Bounds.Y);
                                            gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height));

                                            dg.GuidelineSet = gs2;
                                            dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height)))));

                                            DrawingImage di = new DrawingImage(dg);

                                            if (di.CanFreeze)
                                            {
                                                di.Freeze();
                                            }

                                            this.InspectorImage.Source = di;
                                            this.InspectorImage.Width = Math.Ceiling(dg.Bounds.Width);
                                            this.InspectorImage.Height = Math.Ceiling(dg.Bounds.Height);

                                            Canvas.SetLeft(this.InspectorImage, Canvas.GetLeft(this.InspectorCanvas) + 8);
                                            Canvas.SetTop(this.InspectorImage, Canvas.GetTop(this.InspectorCanvas));
                                        }
                                        else if (this.imagePopupStep > 0 && this.cachedBitmapImage != null)
                                        {
                                            double thickness = 1;
                                            Pen pen = new Pen();
                                            ImageBrush previousImageBrush = new ImageBrush(this.cachedBitmapImage);
                                            DrawingGroup dg = new DrawingGroup();
                                            DrawingContext dc = dg.Open();
                                            GeometryGroup gg = new GeometryGroup();
                                            GuidelineSet gs1 = new GuidelineSet();
                                            GuidelineSet gs2 = new GuidelineSet();
                                            double step = Math.Sin(this.imagePopupStep / 2 * Math.PI);

                                            pen.Thickness = thickness;

                                            if (this.imageBlinkStep > 0)
                                            {
                                                Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;

                                                pen.Brush = new SolidColorBrush(Color.FromArgb((byte)(color.A + (this.linkColor.A - color.A) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.R + (this.linkColor.R - color.R) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.G + (this.linkColor.G - color.G) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.B + (this.linkColor.B - color.B) * Math.Sin(this.imageBlinkStep * Math.PI))));
                                            }
                                            else
                                            {
                                                pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                            }

                                            Point imageLocation = new Point(Canvas.GetLeft(this.InspectorCanvas) + 8, Canvas.GetTop(this.InspectorCanvas));
                                            Rect drawableRect = new Rect(imageLocation.X, this.baseHeaderHeight, this.Canvas.Width - imageLocation.X * 2, this.Canvas.Height - this.baseHeaderHeight - this.baseFooterHeight);
                                            Size popupImageSize = new Size(this.cachedBitmapImage.Width, this.cachedBitmapImage.Height);

                                            if ((Canvas.GetTop(this.InspectorCanvas) + imageSize.Height) > (drawableRect.Y + drawableRect.Height))
                                            {
                                                drawableRect.Height += (Canvas.GetTop(this.InspectorCanvas) + imageSize.Height) - (drawableRect.Y + drawableRect.Height);
                                            }

                                            if (this.cachedBitmapImage.Width > drawableRect.Width || this.cachedBitmapImage.Height > drawableRect.Height)
                                            {
                                                if (this.cachedBitmapImage.Width > this.cachedBitmapImage.Height)
                                                {
                                                    popupImageSize.Width = drawableRect.Width;
                                                    popupImageSize.Height = drawableRect.Width * this.cachedBitmapImage.PixelHeight / this.cachedBitmapImage.PixelWidth;

                                                    if (popupImageSize.Height > drawableRect.Height)
                                                    {
                                                        popupImageSize.Width = drawableRect.Height * this.cachedBitmapImage.PixelWidth / this.cachedBitmapImage.PixelHeight;
                                                        popupImageSize.Height = drawableRect.Height;
                                                    }
                                                }
                                                else
                                                {
                                                    popupImageSize.Width = drawableRect.Height * this.cachedBitmapImage.PixelWidth / this.cachedBitmapImage.PixelHeight;
                                                    popupImageSize.Height = drawableRect.Height;

                                                    if (popupImageSize.Width > drawableRect.Width)
                                                    {
                                                        popupImageSize.Width = drawableRect.Width;
                                                        popupImageSize.Height = drawableRect.Width * this.cachedBitmapImage.PixelHeight / this.cachedBitmapImage.PixelWidth;
                                                    }
                                                }
                                            }

                                            Rect popupImageRect = new Rect(drawableRect.X, imageLocation.Y - (popupImageSize.Height - imageSize.Height) / 2, popupImageSize.Width, popupImageSize.Height);

                                            if (popupImageRect.Y < Canvas.GetTop(this.ScrollCanvas))
                                            {
                                                popupImageRect.Y += Canvas.GetTop(this.ScrollCanvas) - popupImageRect.Y;
                                            }
                                            else if (popupImageRect.Y + popupImageRect.Height > drawableRect.Y + drawableRect.Height)
                                            {
                                                popupImageRect.Y -= (popupImageRect.Y + popupImageRect.Height) - (drawableRect.Y + drawableRect.Height);
                                            }

                                            popupImageRect = new Rect(imageLocation.X + (popupImageRect.X - imageLocation.X) * step, imageLocation.Y + (popupImageRect.Y - imageLocation.Y) * step, imageSize.Width + (popupImageRect.Width - imageSize.Width) * step, imageSize.Height + (popupImageRect.Height - imageSize.Height) * step);

                                            gs1.GuidelinesX.Add(0);
                                            gs1.GuidelinesX.Add(popupImageRect.Width);
                                            gs1.GuidelinesY.Add(0);
                                            gs1.GuidelinesY.Add(popupImageRect.Height);

                                            previousImageBrush.TileMode = TileMode.None;
                                            previousImageBrush.Stretch = Stretch.Fill;
                                            previousImageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                            previousImageBrush.Viewbox = new Rect(this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) / 2 - (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) / 2 * step : 0, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? 0 : (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) / 2 - (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) / 2 * step, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height + (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) * step : this.cachedBitmapImage.Width, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width + (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) * step);
                                            previousImageBrush.AlignmentX = AlignmentX.Left;
                                            previousImageBrush.AlignmentY = AlignmentY.Top;

                                            dc.PushGuidelineSet(gs1);
                                            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, popupImageRect.Width, popupImageRect.Height));
                                            dc.DrawRectangle(previousImageBrush, null, new Rect(0, 0, popupImageRect.Width, popupImageRect.Height));
                                            dc.DrawRectangle(null, pen, new Rect(thickness / 2, thickness / 2, popupImageRect.Width - thickness, popupImageRect.Height - thickness));
                                            dc.Close();

                                            gs2.GuidelinesX.Add(dg.Bounds.X);
                                            gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                            gs2.GuidelinesY.Add(dg.Bounds.Y);
                                            gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height));

                                            dg.GuidelineSet = gs2;
                                            dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height)))));

                                            DrawingImage di = new DrawingImage(dg);

                                            if (di.CanFreeze)
                                            {
                                                di.Freeze();
                                            }

                                            this.InspectorImage.Source = di;
                                            this.InspectorImage.Width = Math.Ceiling(dg.Bounds.Width);
                                            this.InspectorImage.Height = Math.Ceiling(dg.Bounds.Height);

                                            Canvas.SetLeft(this.InspectorImage, popupImageRect.X);
                                            Canvas.SetTop(this.InspectorImage, popupImageRect.Y);
                                        }
                                        else
                                        {
                                            double thickness = 1;
                                            Pen pen = new Pen();
                                            DrawingGroup dg = new DrawingGroup();
                                            DrawingContext dc = dg.Open();
                                            GeometryGroup gg = new GeometryGroup();
                                            GuidelineSet gs = new GuidelineSet();
                                            GuidelineSet gs2 = new GuidelineSet();


                                            pen.Thickness = thickness;

                                            if (this.imageBlinkStep > 0)
                                            {
                                                Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;

                                                pen.Brush = new SolidColorBrush(Color.FromArgb((byte)(color.A + (this.linkColor.A - color.A) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.R + (this.linkColor.R - color.R) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.G + (this.linkColor.G - color.G) * Math.Sin(this.imageBlinkStep * Math.PI)), (byte)(color.B + (this.linkColor.B - color.B) * Math.Sin(this.imageBlinkStep * Math.PI))));
                                            }
                                            else
                                            {
                                                pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                            }

                                            gs.GuidelinesX.Add(0);
                                            gs.GuidelinesX.Add(imageSize.Width);
                                            gs.GuidelinesY.Add(0);
                                            gs.GuidelinesY.Add(imageSize.Height);

                                            dc.PushGuidelineSet(gs);

                                            if (this.imageUri == null)
                                            {
                                                if (this.cachedBitmapImage == null)
                                                {
                                                    dc.PushTransform(new TranslateTransform(-this.imageLoadStep * 20, 0));
                                                    dc.PushClip(new RectangleGeometry(new Rect(this.imageLoadStep * 20, 0, imageSize.Width, imageSize.Height)));
                                                    dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 10 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B)), new Size(20, 20)), null, new Rect(0, 0, imageSize.Width + 20, imageSize.Height));
                                                    dc.Pop();
                                                    dc.Pop();
                                                }
                                                else
                                                {
                                                    ImageBrush imageBrush = new ImageBrush(this.cachedBitmapImage);

                                                    imageBrush.TileMode = TileMode.None;
                                                    imageBrush.Stretch = Stretch.Fill;
                                                    imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                    imageBrush.Viewbox = new Rect(this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) / 2 : 0, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? 0 : (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) / 2, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width);
                                                    imageBrush.AlignmentX = AlignmentX.Left;
                                                    imageBrush.AlignmentY = AlignmentY.Top;

                                                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                    dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                }
                                            }
                                            else
                                            {
                                                BitmapImage bi;

                                                if (this.imageDictionary.TryGetValue(this.imageUri, out bi))
                                                {
                                                    if (bi == this.cachedBitmapImage && bi != null)
                                                    {
                                                        ImageBrush imageBrush = new ImageBrush(bi);

                                                        imageBrush.TileMode = TileMode.None;
                                                        imageBrush.Stretch = Stretch.Fill;
                                                        imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                        imageBrush.Viewbox = new Rect(bi.Width > bi.Height ? (bi.Width - bi.Height) / 2 : 0, bi.Width > bi.Height ? 0 : (bi.Height - bi.Width) / 2, bi.Width > bi.Height ? bi.Height : bi.Width, bi.Width > bi.Height ? bi.Height : bi.Width);
                                                        imageBrush.AlignmentX = AlignmentX.Left;
                                                        imageBrush.AlignmentY = AlignmentY.Top;

                                                        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                        dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                    }
                                                    else
                                                    {
                                                        if (this.cachedBitmapImage == null)
                                                        {
                                                            dc.PushTransform(new TranslateTransform(-this.imageLoadStep * 20, 0));
                                                            dc.PushClip(new RectangleGeometry(new Rect(this.imageLoadStep * 20, 0, imageSize.Width, imageSize.Height)));
                                                            dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 10 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B)), new Size(20, 20)), null, new Rect(0, 0, imageSize.Width + 20, imageSize.Height));
                                                            dc.Pop();
                                                            dc.Pop();
                                                        }
                                                        else
                                                        {
                                                            ImageBrush imageBrush = new ImageBrush(this.cachedBitmapImage);

                                                            imageBrush.TileMode = TileMode.None;
                                                            imageBrush.Stretch = Stretch.Fill;
                                                            imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                            imageBrush.Viewbox = new Rect(this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) / 2 : 0, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? 0 : (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) / 2, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width);
                                                            imageBrush.AlignmentX = AlignmentX.Left;
                                                            imageBrush.AlignmentY = AlignmentY.Top;

                                                            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                            dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                        }

                                                        if (bi != null)
                                                        {
                                                            double step = Math.Sin(this.imageSlideStep / 2 * Math.PI);
                                                            StreamGeometry streamGeometry = new StreamGeometry();

                                                            streamGeometry.FillRule = FillRule.Nonzero;

                                                            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
                                                            {
                                                                streamGeometryContext.BeginFigure(new Point(0, 0), true, true);

                                                                if (step <= 0.5)
                                                                {
                                                                    streamGeometryContext.LineTo(new Point(imageSize.Width * step / 0.5, 0), true, false);
                                                                    streamGeometryContext.LineTo(new Point(0, imageSize.Height * step / 0.5), true, false);
                                                                }
                                                                else
                                                                {
                                                                    streamGeometryContext.LineTo(new Point(imageSize.Width, 0), true, false);
                                                                    streamGeometryContext.LineTo(new Point(imageSize.Width, imageSize.Height * (step - 0.5) / 0.5), true, false);
                                                                    streamGeometryContext.LineTo(new Point(imageSize.Width * (step - 0.5) / 0.5, imageSize.Height), true, false);
                                                                    streamGeometryContext.LineTo(new Point(0, imageSize.Height), true, false);
                                                                }
                                                            }

                                                            ImageBrush imageBrush = new ImageBrush(bi);

                                                            imageBrush.TileMode = TileMode.None;
                                                            imageBrush.Stretch = Stretch.Fill;
                                                            imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                            imageBrush.Viewbox = new Rect(bi.Width > bi.Height ? (bi.Width - bi.Height) / 2 : 0, bi.Width > bi.Height ? 0 : (bi.Height - bi.Width) / 2, bi.Width > bi.Height ? bi.Height : bi.Width, bi.Width > bi.Height ? bi.Height : bi.Width);
                                                            imageBrush.AlignmentX = AlignmentX.Left;
                                                            imageBrush.AlignmentY = AlignmentY.Top;

                                                            dc.PushClip(streamGeometry);
                                                            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                            dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                            dc.Pop();
                                                        }
                                                    }
                                                }
                                                else if (this.cachedBitmapImage == null)
                                                {
                                                    dc.PushTransform(new TranslateTransform(-this.imageLoadStep * 20, 0));
                                                    dc.PushClip(new RectangleGeometry(new Rect(this.imageLoadStep * 20, 0, imageSize.Width, imageSize.Height)));
                                                    dc.DrawRectangle(CreateStripeBrush(new SolidColorBrush(Color.FromArgb((byte)(Colors.Black.A * 10 / 100), Colors.Black.R, Colors.Black.G, Colors.Black.B)), new Size(20, 20)), null, new Rect(0, 0, imageSize.Width + 20, imageSize.Height));
                                                    dc.Pop();
                                                    dc.Pop();
                                                }
                                                else
                                                {
                                                    ImageBrush imageBrush = new ImageBrush(this.cachedBitmapImage);

                                                    imageBrush.TileMode = TileMode.None;
                                                    imageBrush.Stretch = Stretch.Fill;
                                                    imageBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                                    imageBrush.Viewbox = new Rect(this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? (this.cachedBitmapImage.Width - this.cachedBitmapImage.Height) / 2 : 0, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? 0 : (this.cachedBitmapImage.Height - this.cachedBitmapImage.Width) / 2, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width, this.cachedBitmapImage.Width > this.cachedBitmapImage.Height ? this.cachedBitmapImage.Height : this.cachedBitmapImage.Width);
                                                    imageBrush.AlignmentX = AlignmentX.Left;
                                                    imageBrush.AlignmentY = AlignmentY.Top;

                                                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                    dc.DrawRectangle(imageBrush, null, new Rect(0, 0, imageSize.Width, imageSize.Height));
                                                }
                                            }

                                            dc.DrawRectangle(null, pen, new Rect(thickness / 2, thickness / 2, imageSize.Width - thickness, imageSize.Height - thickness));
                                            dc.Close();

                                            gs2.GuidelinesX.Add(dg.Bounds.X);
                                            gs2.GuidelinesX.Add(dg.Bounds.X + Math.Ceiling(dg.Bounds.Width));
                                            gs2.GuidelinesY.Add(dg.Bounds.Y);
                                            gs2.GuidelinesY.Add(dg.Bounds.Y + Math.Ceiling(dg.Bounds.Height));

                                            dg.GuidelineSet = gs2;
                                            dg.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(dg.Bounds.X, dg.Bounds.Y, Math.Ceiling(dg.Bounds.Width), Math.Ceiling(dg.Bounds.Height)))));

                                            DrawingImage di = new DrawingImage(dg);

                                            if (di.CanFreeze)
                                            {
                                                di.Freeze();
                                            }

                                            this.InspectorImage.Source = di;
                                            this.InspectorImage.Width = Math.Ceiling(dg.Bounds.Width);
                                            this.InspectorImage.Height = Math.Ceiling(dg.Bounds.Height);

                                            Canvas.SetLeft(this.InspectorImage, Canvas.GetLeft(this.InspectorCanvas) + 8);
                                            Canvas.SetTop(this.InspectorImage, Canvas.GetTop(this.InspectorCanvas));
                                        }
                                    }
                                }

                                if (isReady && this.nextHistoryPoint.HasValue && this.inlineList.Count == 0 && this.messageBuffer.Length == 0 && this.randomMessageLength == 0 && this.attachmentFadeStepDictionary.Values.All(step => step == 0) && this.filterStep == null && this.scrollStep == null && this.liftStep == null && this.counterScrollStep == null && this.inspectorEntry == null)
                                {
                                    this.isReady = false;
                                    this.hoverEmbeddedIndex = null;
                                    this.enableFilter = false;
                                    this.minScore = 0;
                                    this.maxScore = 0;
                                    this.thresholdQueue.Clear();
                                    this.thresholdScore = 0;
                                    this.previousThresholdScore = 0;
                                    this.nextThresholdScore = 0;
                                    this.thresholdScoreStep = 0;
                                    this.isReversed = false;
                                    this.scrollQueue.Clear();
                                    this.selectedPositionQueue.Clear();
                                    this.sourceScrollPosition = this.targetScrollPosition = 0;
                                    this.scrollIndexStep = 0;
                                    this.hoverIndex = null;
                                    this.selectedIndex = null;
                                    this.selectedPosition = null;
                                    this.embedColorStepDictionary.Clear();
                                    this.embedScrollStepDictionary.Clear();
                                    this.embedIsScrollableHashSet.Clear();
                                    this.attachmentFadeStepDictionary.Clear();
                                    this.attachmentImageLoadingStepDictionary.Clear();
                                    this.attachmentImageSlideStepDictionary.Clear();
                                    this.attachmentImagePopupStepDictionary.Clear();
                                    this.attachmentHighlightStepDictionary.Clear();
                                    this.attachmentEnableStepDictionary.Clear();
                                    this.attachmentFilterStepDictionary.Clear();
                                    this.attachmentScrollStepDictionary.Clear();
                                    this.attachmentIsScrollableHashSet.Clear();
                                    this.attachmentImageDictionary.Clear();
                                    this.cachedInlineImageDictionary.Clear();
                                    this.cachedAttachmentThumbnailImageDictionary.Clear();
                                    this.cachedAttachmentTextImageDictionary.Clear();
                                    this.imageDictionary.Clear();
                                    this.imageUriHashSet.Clear();
                                    this.TranslateTransform.X = this.TranslateTransform.Y = 0;
                                    this.ScrollCanvas.Width = this.ScrollCanvas.Height = Double.NaN;
                                    this.MessageCanvas.Width = this.MessageCanvas.Height = Double.NaN;
                                    this.waitTimer.Stop();
                                    this.messageTypeTimer.Stop();
                                }

                                if (waitRequired && !this.isPinned && this.targetOpacity > 0 && this.targetScaleX > 0 && this.targetScaleY > 0 && !this.waitTimer.IsEnabled)
                                {
                                    this.waitTimer.Interval = this.messageCollection[this.historyPoint.Value].Duration;
                                    this.waitTimer.Start();
                                }
                            }
                            else if (!this.isPinned && this.targetOpacity > 0 && this.targetScaleX > 0 && this.targetScaleY > 0 && !this.waitTimer.IsEnabled)
                            {
                                this.waitTimer.Interval = TimeSpan.Zero;
                                this.waitTimer.Start();
                            }
                        }

                        if (this.targetSize.Width != this.Canvas.Width || this.targetSize.Height != this.Canvas.Height)
                        {
                            Size newClientSize = new Size(this.Canvas.Width, this.Canvas.Height);

                            this.resizeStep += 1 / (averageFrameRate / 4);

                            if (this.resizeStep >= 1)
                            {
                                this.resizeStep = 1;

                                if (newClientSize.Width != this.targetSize.Width || newClientSize.Height != this.targetSize.Height)
                                {
                                    newClientSize = this.targetSize;
                                }
                            }
                            else
                            {
                                double step = Math.Sin(this.resizeStep / 2 * Math.PI);

                                newClientSize.Width = this.sourceSize.Width + Math.Ceiling((this.targetSize.Width - this.sourceSize.Width) * step);
                                newClientSize.Height = this.sourceSize.Height + Math.Ceiling((this.targetSize.Height - this.sourceSize.Height) * step);
                            }

                            Geometry roundedRectangleGeometry = CreateRoundedRectangleGeometry(new Rect(0, 0, newClientSize.Width, newClientSize.Height - 11 + 4), 8, 8);
                            Geometry balloonGeometry = CreateBalloonGeometry(new Rect(4, 4, newClientSize.Width - 4 * 2, newClientSize.Height - 4), 8 * 3 / 4, 8 * 3 / 4);
                            Geometry highlightGeometry = CreateHighlightGeometry(new Rect(0, 0, newClientSize.Width, this.baseHeaderHeight + 8), 8, 8);
                            Geometry highlightLineGeometry = CreateHighlightLineGeometry(new Rect(0, 0, newClientSize.Width, 8), 8, 8);

                            if (roundedRectangleGeometry.CanFreeze)
                            {
                                roundedRectangleGeometry.Freeze();
                            }

                            if (balloonGeometry.CanFreeze)
                            {
                                balloonGeometry.Freeze();
                            }

                            if (highlightGeometry.CanFreeze)
                            {
                                highlightGeometry.Freeze();
                            }

                            if (highlightLineGeometry.CanFreeze)
                            {
                                highlightLineGeometry.Freeze();
                            }

                            GeometryGroup geometryGroup = new GeometryGroup();

                            geometryGroup.FillRule = FillRule.Nonzero;
                            geometryGroup.Children.Add(roundedRectangleGeometry);
                            geometryGroup.Children.Add(balloonGeometry);

                            if (geometryGroup.CanFreeze)
                            {
                                geometryGroup.Freeze();
                            }

                            RadialGradientBrush radialGradientBrush = new RadialGradientBrush();
                            GradientStop gradientStop1 = new GradientStop(Color.FromArgb(Byte.MaxValue, Byte.MaxValue, Byte.MaxValue, Byte.MaxValue), 0);
                            GradientStop gradientStop2 = new GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1);

                            radialGradientBrush.GradientOrigin = new Point(0.5, 0);
                            radialGradientBrush.Center = new Point(0.5, 0);
                            radialGradientBrush.RadiusX = 0.5;
                            radialGradientBrush.RadiusY = this.sourceSize.Width / 2 < this.sourceSize.Height - this.baseFooterHeight ? this.sourceSize.Width / 2 / this.sourceSize.Height : (this.sourceSize.Height - this.baseFooterHeight) / this.sourceSize.Height;
                            radialGradientBrush.Opacity = 0.1;
                            radialGradientBrush.GradientStops.Add(gradientStop1);
                            radialGradientBrush.GradientStops.Add(gradientStop2);

                            if (radialGradientBrush.CanFreeze)
                            {
                                radialGradientBrush.Freeze();
                            }

                            this.OuterPath.Width = roundedRectangleGeometry.Bounds.Right;
                            this.OuterPath.Height = roundedRectangleGeometry.Bounds.Bottom;
                            this.OuterPath.Data = roundedRectangleGeometry;
                            this.InnerPath.Width = balloonGeometry.Bounds.Right;
                            this.InnerPath.Height = balloonGeometry.Bounds.Bottom;
                            this.InnerPath.Data = balloonGeometry;
                            this.OverlayPath.Width = geometryGroup.Bounds.Right;
                            this.OverlayPath.Height = geometryGroup.Bounds.Bottom;
                            this.OverlayPath.Data = geometryGroup;
                            this.OverlayPath.Fill = radialGradientBrush;
                            this.HighlightPath.Width = highlightGeometry.Bounds.Right;
                            this.HighlightPath.Height = highlightGeometry.Bounds.Bottom;
                            this.HighlightPath.Data = highlightGeometry;
                            this.HighlightLinePath.Width = highlightLineGeometry.Bounds.Right;
                            this.HighlightLinePath.Height = highlightLineGeometry.Bounds.Bottom;
                            this.HighlightLinePath.Data = highlightLineGeometry;
                            this.Canvas.Width = newClientSize.Width;
                            this.Canvas.Height = newClientSize.Height;
                            this.ScaleTransform.CenterX = newClientSize.Width / 2;
                            this.ScaleTransform.CenterY = newClientSize.Height;

                            Canvas.SetLeft(this.Canvas, (this.LayoutRoot.Width - newClientSize.Width) / 2);
                            Canvas.SetTop(this.Canvas, this.LayoutRoot.Height - newClientSize.Height);
                            Canvas.SetLeft(this.FilterImage, newClientSize.Width - 14);
                            Canvas.SetLeft(this.ScrollImage, newClientSize.Width - 14);
                            Canvas.SetLeft(this.CloseImage, newClientSize.Width - 17);
                            Canvas.SetLeft(this.BackImage, newClientSize.Width - 30);
                            Canvas.SetLeft(this.UpImage, newClientSize.Width - 17);
                            Canvas.SetTop(this.UpImage, newClientSize.Height - 32);
                            Canvas.SetLeft(this.DownImage, newClientSize.Width - 17);
                            Canvas.SetTop(this.DownImage, newClientSize.Height - 21);
                        }

                        if (this.backIsHover || this.backBlinkStep > 0 || this.BackImage.Source == null)
                        {
                            Pen pen = new Pen();

                            if (this.backIsHover || this.backBlinkStep > 0)
                            {
                                this.backBlinkStep += 1 / (averageFrameRate / 2);

                                if (this.backBlinkStep >= 1)
                                {
                                    this.backBlinkStep = 0;
                                    pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                }
                                else
                                {
                                    Color color = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;

                                    pen.Brush = new SolidColorBrush(Color.FromArgb((byte)(color.A + (this.linkColor.A - color.A) * Math.Sin(this.backBlinkStep * Math.PI)), (byte)(color.R + (this.linkColor.R - color.R) * Math.Sin(this.backBlinkStep * Math.PI)), (byte)(color.G + (this.linkColor.G - color.G) * Math.Sin(this.backBlinkStep * Math.PI)), (byte)(color.B + (this.linkColor.B - color.B) * Math.Sin(this.backBlinkStep * Math.PI))));
                                }
                            }
                            else
                            {
                                pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                            }

                            pen.Thickness = 2;
                            pen.StartLineCap = PenLineCap.Square;
                            pen.EndLineCap = PenLineCap.Square;

                            DrawingGroup backDrawingGroup = new DrawingGroup();
                            GuidelineSet backGuidelineSet = new GuidelineSet();

                            backDrawingGroup.Children.Add(new GeometryDrawing(null, pen, CreateBackGeometry(new Rect(0, 0, 6, 7))));

                            backGuidelineSet.GuidelinesX.Add(backDrawingGroup.Bounds.X);
                            backGuidelineSet.GuidelinesX.Add(backDrawingGroup.Bounds.X + Math.Ceiling(backDrawingGroup.Bounds.Width));
                            backGuidelineSet.GuidelinesY.Add(backDrawingGroup.Bounds.Y);
                            backGuidelineSet.GuidelinesY.Add(backDrawingGroup.Bounds.Y + Math.Ceiling(backDrawingGroup.Bounds.Height));

                            backDrawingGroup.GuidelineSet = backGuidelineSet;
                            backDrawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(backDrawingGroup.Bounds.X, backDrawingGroup.Bounds.Y, Math.Ceiling(backDrawingGroup.Bounds.Width), Math.Ceiling(backDrawingGroup.Bounds.Height)))));

                            DrawingImage backDrawingImage = new DrawingImage(backDrawingGroup);

                            if (backDrawingImage.CanFreeze)
                            {
                                backDrawingImage.Freeze();
                            }

                            this.BackImage.Source = backDrawingImage;
                            this.BackImage.Width = Math.Ceiling(backDrawingGroup.Bounds.Width);
                            this.BackImage.Height = Math.Ceiling(backDrawingGroup.Bounds.Height);
                        }

                        if (this.closeIsHover || this.closeBlinkStep > 0 || this.CloseImage.Source == null)
                        {
                            Pen pen = new Pen();

                            if (this.closeIsHover || this.closeBlinkStep > 0)
                            {
                                this.closeBlinkStep += 1 / (averageFrameRate / 2);

                                if (this.closeBlinkStep >= 1)
                                {
                                    this.closeBlinkStep = 0;
                                    pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                                }
                                else
                                {
                                    Color color1 = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Colors.Black : Colors.White;
                                    Color color2 = Color.FromArgb(Byte.MaxValue, 204, 0, 0);

                                    pen.Brush = new SolidColorBrush(Color.FromArgb(Byte.MaxValue, (byte)(color1.R + (color2.R - color1.R) * Math.Sin(this.closeBlinkStep * Math.PI)), (byte)(color1.G + (color2.G - color1.G) * Math.Sin(this.closeBlinkStep * Math.PI)), (byte)(color1.B + (color2.B - color1.B) * Math.Sin(this.closeBlinkStep * Math.PI))));
                                }
                            }
                            else
                            {
                                pen.Brush = Math.Max(Math.Max(this.backgroundColor.R, this.backgroundColor.G), this.backgroundColor.B) > Byte.MaxValue / 2 ? Brushes.Black : Brushes.White;
                            }

                            pen.Thickness = 2;
                            pen.StartLineCap = PenLineCap.Square;
                            pen.EndLineCap = PenLineCap.Square;

                            DrawingGroup closeDrawingGroup = new DrawingGroup();
                            GuidelineSet closeGuidelineSet = new GuidelineSet();

                            closeDrawingGroup.Children.Add(new GeometryDrawing(null, pen, CreateCloseGeometry(new Rect(0, 0, 6, 6))));

                            closeGuidelineSet.GuidelinesX.Add(closeDrawingGroup.Bounds.X);
                            closeGuidelineSet.GuidelinesX.Add(closeDrawingGroup.Bounds.X + Math.Ceiling(closeDrawingGroup.Bounds.Width));
                            closeGuidelineSet.GuidelinesY.Add(closeDrawingGroup.Bounds.Y);
                            closeGuidelineSet.GuidelinesY.Add(closeDrawingGroup.Bounds.Y + Math.Ceiling(closeDrawingGroup.Bounds.Height));

                            closeDrawingGroup.GuidelineSet = closeGuidelineSet;
                            closeDrawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(closeDrawingGroup.Bounds.X, closeDrawingGroup.Bounds.Y, Math.Ceiling(closeDrawingGroup.Bounds.Width), Math.Ceiling(closeDrawingGroup.Bounds.Height)))));

                            DrawingImage closeDrawingImage = new DrawingImage(closeDrawingGroup);

                            if (closeDrawingImage.CanFreeze)
                            {
                                closeDrawingImage.Freeze();
                            }

                            this.CloseImage.Source = closeDrawingImage;
                            this.CloseImage.Width = Math.Ceiling(closeDrawingGroup.Bounds.Width);
                            this.CloseImage.Height = Math.Ceiling(closeDrawingGroup.Bounds.Height);
                        }

                        if (this.Canvas.Opacity == 0 && this.Canvas.Opacity == this.targetOpacity)
                        {
                            Hide();
                        }
                    }
                }
            }
        }

        private Geometry CreateBalloonGeometry(Rect rect, int radiusX, int radiusY)
        {
            StreamGeometry streamGeometry = new StreamGeometry();
            double length = 11;

            streamGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(new Point(rect.X, rect.Y + radiusY), true, true);
                streamGeometryContext.QuadraticBezierTo(rect.Location, new Point(rect.X + radiusX, rect.Y), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width - radiusX, rect.Y), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + radiusY), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height - radiusY - length), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y + rect.Height - length), new Point(rect.X + rect.Width - radiusX, rect.Y + rect.Height - length), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width / 2 + length / 2, rect.Y + rect.Height - length), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width / 2 - length / 2, rect.Y + rect.Height - length), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + radiusX, rect.Y + rect.Height - length), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X, rect.Y + rect.Height - length), new Point(rect.X, rect.Y + rect.Height - radiusY - length), true, false);
            }

            return streamGeometry;
        }

        private Geometry CreateRoundedRectangleGeometry(Rect rect, int radiusX, int radiusY)
        {
            StreamGeometry streamGeometry = new StreamGeometry();

            streamGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(new Point(rect.X, rect.Y + radiusY), true, true);
                streamGeometryContext.QuadraticBezierTo(rect.Location, new Point(rect.X + radiusX, rect.Y), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width - radiusX, rect.Y), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + radiusY), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height - radiusY), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y + rect.Height), new Point(rect.X + rect.Width - radiusX, rect.Y + rect.Height), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + radiusX, rect.Y + rect.Height), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X, rect.Y + rect.Height), new Point(rect.X, rect.Y + rect.Height - radiusY), true, false);
            }

            return streamGeometry;
        }

        private Geometry CreateHighlightGeometry(Rect rect, int radiusX, int radiusY)
        {
            StreamGeometry streamGeometry = new StreamGeometry();

            streamGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(new Point(rect.X, rect.Y + radiusY), true, true);
                streamGeometryContext.QuadraticBezierTo(rect.Location, new Point(rect.X + radiusX, rect.Y), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width - radiusX, rect.Y), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + radiusY), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height - radiusY), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width * 75 / 100, rect.Y + rect.Height), new Point(rect.X + rect.Width / 2, rect.Y + rect.Height), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width * 25 / 100, rect.Y + rect.Height), new Point(rect.X, rect.Y + rect.Height - radiusY), true, false);
            }

            return streamGeometry;
        }

        private Geometry CreateHighlightLineGeometry(Rect rect, int radiusX, int radiusY)
        {
            StreamGeometry streamGeometry = new StreamGeometry();

            streamGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(new Point(rect.X, rect.Y + rect.Height), true, true);
                streamGeometryContext.LineTo(new Point(rect.X, rect.Y + radiusY), true, false);
                streamGeometryContext.QuadraticBezierTo(rect.Location, new Point(rect.X + radiusX, rect.Y), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width - radiusX, rect.Y), true, false);
                streamGeometryContext.QuadraticBezierTo(new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + radiusY), true, false);
                streamGeometryContext.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height), true, false);
            }

            return streamGeometry;
        }

        private Geometry CreateCloseGeometry(Rect rect)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            LineGeometry lineGeometry1 = new LineGeometry();
            LineGeometry lineGeometry2 = new LineGeometry();

            lineGeometry1.StartPoint = rect.Location;
            lineGeometry1.EndPoint = new Point(rect.X + rect.Width, rect.Y + rect.Height);

            lineGeometry2.StartPoint = new Point(rect.X, rect.Y + rect.Height);
            lineGeometry2.EndPoint = new Point(rect.X + rect.Width, rect.Y);

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(lineGeometry1);
            geometryGroup.Children.Add(lineGeometry2);

            return geometryGroup;
        }

        private Geometry CreateBackGeometry(Rect rect)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            LineGeometry lineSegment1 = new LineGeometry();
            LineGeometry lineSegment2 = new LineGeometry();
            LineGeometry lineGeometry1 = new LineGeometry();

            lineSegment1.StartPoint = new Point(rect.X + rect.Width / 2, rect.Y);
            lineSegment1.EndPoint = new Point(rect.X, rect.Y + rect.Height / 2);

            lineSegment2.StartPoint = new Point(rect.X, rect.Y + rect.Height / 2);
            lineSegment2.EndPoint = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height);

            lineGeometry1.StartPoint = new Point(rect.X, rect.Y + rect.Height / 2);
            lineGeometry1.EndPoint = new Point(rect.X + rect.Width, rect.Y + rect.Height / 2);

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(lineSegment1);
            geometryGroup.Children.Add(lineSegment2);
            geometryGroup.Children.Add(lineGeometry1);

            return geometryGroup;
        }

        private Geometry CreateUpGeometry(Rect rect)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            LineGeometry lineGeometry1 = new LineGeometry();
            LineGeometry lineGeometry2 = new LineGeometry();

            lineGeometry1.StartPoint = new Point(rect.X, rect.Y + rect.Height);
            lineGeometry1.EndPoint = new Point(rect.X + rect.Width / 2, rect.Y);

            lineGeometry2.StartPoint = new Point(rect.X + rect.Width / 2, rect.Y);
            lineGeometry2.EndPoint = new Point(rect.X + rect.Width, rect.Y + rect.Height);

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(lineGeometry1);
            geometryGroup.Children.Add(lineGeometry2);

            return geometryGroup;
        }

        private Geometry CreateDownGeometry(Rect rect)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            LineGeometry lineGeometry1 = new LineGeometry();
            LineGeometry lineGeometry2 = new LineGeometry();

            lineGeometry1.StartPoint = rect.Location;
            lineGeometry1.EndPoint = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height);

            lineGeometry2.StartPoint = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height);
            lineGeometry2.EndPoint = new Point(rect.X + rect.Width, rect.Y);

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(lineGeometry1);
            geometryGroup.Children.Add(lineGeometry2);

            return geometryGroup;
        }

        private Geometry CreatePlusGeometry(Rect rect)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            LineGeometry lineGeometry1 = new LineGeometry();
            LineGeometry lineGeometry2 = new LineGeometry();

            lineGeometry1.StartPoint = new Point(rect.X + rect.Width / 2, rect.Y);
            lineGeometry1.EndPoint = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height);

            lineGeometry2.StartPoint = new Point(rect.X, rect.Y + rect.Height / 2);
            lineGeometry2.EndPoint = new Point(rect.X + rect.Width, rect.Y + rect.Height / 2);

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(lineGeometry1);
            geometryGroup.Children.Add(lineGeometry2);

            return geometryGroup;
        }

        private DrawingBrush CreateStripeBrush(Brush brush, Size size)
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            GuidelineSet guidelineSet = new GuidelineSet();
            GeometryGroup geometryGroup = new GeometryGroup();
            StreamGeometry streamGeometry1 = new StreamGeometry();
            StreamGeometry streamGeometry2 = new StreamGeometry();

            guidelineSet.GuidelinesX.Add(0);
            guidelineSet.GuidelinesX.Add(size.Width);
            guidelineSet.GuidelinesY.Add(0);
            guidelineSet.GuidelinesY.Add(size.Height);

            drawingGroup.GuidelineSet = guidelineSet;

            streamGeometry1.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry1.Open())
            {
                streamGeometryContext.BeginFigure(new Point(0, 0), true, true);
                streamGeometryContext.LineTo(new Point(size.Width / 2, 0), true, false);
                streamGeometryContext.LineTo(new Point(0, size.Height / 2), true, false);
            }

            streamGeometry2.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext streamGeometryContext = streamGeometry2.Open())
            {
                streamGeometryContext.BeginFigure(new Point(size.Width, 0), true, true);
                streamGeometryContext.LineTo(new Point(size.Width, size.Height / 2), true, false);
                streamGeometryContext.LineTo(new Point(size.Width / 2, size.Height), true, false);
                streamGeometryContext.LineTo(new Point(0, size.Height), true, false);
            }

            geometryGroup.FillRule = FillRule.Nonzero;
            geometryGroup.Children.Add(streamGeometry1);
            geometryGroup.Children.Add(streamGeometry2);

            drawingGroup.Children.Add(new GeometryDrawing(brush, null, geometryGroup));

            PresentationSource presentationSource = PresentationSource.FromVisual(this);

            if (presentationSource != null && presentationSource.CompositionTarget != null && presentationSource.CompositionTarget.TransformToDevice.M11 == 1.0 && presentationSource.CompositionTarget.TransformToDevice.M22 == 1.0)
            {
                RenderOptions.SetEdgeMode(drawingGroup, EdgeMode.Aliased);
            }

            DrawingBrush drawingBrush = new DrawingBrush(drawingGroup);

            drawingBrush.TileMode = TileMode.Tile;
            drawingBrush.ViewportUnits = BrushMappingMode.Absolute;
            drawingBrush.Viewport = new Rect(0, 0, size.Width, size.Height);
            drawingBrush.Stretch = Stretch.None;

            return drawingBrush;
        }

        private Size GetBalloonSize(Message message)
        {
            Size messageSize = GetMessageSize(message);

            if (messageSize.Height > this.maxMessageSize.Height)
            {
                messageSize.Height = this.maxMessageSize.Height;
            }

            return new Size(this.baseWidth, this.baseHeaderHeight + messageSize.Height + this.baseFooterHeight);
        }

        private Size GetBalloonSize(Message message, ref bool isScrollable)
        {
            Size messageSize = GetMessageSize(message);

            if (messageSize.Height > this.maxMessageSize.Height)
            {
                isScrollable = true;
                messageSize.Height = this.maxMessageSize.Height;
            }
            else
            {
                isScrollable = false;
            }

            return new Size(this.baseWidth, this.baseHeaderHeight + messageSize.Height + this.baseFooterHeight);
        }

        private Size GetMessageSize(Message message)
        {
            Size messageSize = new Size(0, 0);
            double x = 0;
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            bool isBreaked = true;
            int lineLength = 0;
            bool isReseted = true;

            foreach (object o in message)
            {
                string inline = o as string;
                Brush brush = this.textBrush;
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                StringBuilder lineStringBuilder = new StringBuilder();

                if (inline == null)
                {
                    Entry entry = o as Entry;

                    if (entry == null)
                    {
                        inline = o.ToString();
                    }
                    else
                    {
                        inline = entry.Title;
                        brush = this.linkBrush;
                    }
                }

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(inline, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                {
                    dictionary.Add(match.Index, match.Length);
                }

                for (int i = 0; i < inline.Length; i++)
                {
                    int length;

                    if (dictionary.TryGetValue(i, out length) && x + Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), inline.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width && !isReseted)
                    {
                        if (lineStringBuilder.Length > 0)
                        {
                            FormattedText formattedText = new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                            if (x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace - formattedText.OverhangLeading - formattedText.OverhangTrailing) > messageSize.Width)
                            {
                                messageSize.Width = x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace - formattedText.OverhangLeading - formattedText.OverhangTrailing);
                            }
                        }

                        lineStringBuilder.Clear();
                        messageSize.Height += this.lineHeight;
                        x = 0;
                        isBreaked = true;
                    }

                    lineStringBuilder.Append(inline[i]);

                    if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                        if (lineStringBuilder.Length > 0)
                        {
                            FormattedText formattedText = new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                            if (x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > messageSize.Width)
                            {
                                messageSize.Width = x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                            }

                            lineStringBuilder.Remove(0, lineStringBuilder.Length);
                        }

                        messageSize.Height += this.lineHeight;
                        x = 0;
                        isBreaked = true;
                        isReseted = true;
                    }
                    else if (x + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace) > this.maxMessageSize.Width)
                    {
                        if (lineStringBuilder.Length - 1 > 0)
                        {
                            FormattedText formattedText = new FormattedText(lineStringBuilder.ToString().Substring(0, lineStringBuilder.Length - 1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip);

                            if (x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace) > messageSize.Width)
                            {
                                messageSize.Width = x + Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
                            }

                            lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                        }

                        messageSize.Height += this.lineHeight;
                        x = 0;
                        isBreaked = true;
                        isReseted = true;
                    }
                    else
                    {
                        isReseted = false;
                    }
                }

                if (lineStringBuilder.Length > 0)
                {
                    x += Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, brush, pixelsPerDip).WidthIncludingTrailingWhitespace);

                    if (x > messageSize.Width)
                    {
                        messageSize.Width = x;
                    }

                    isBreaked = false;
                    isReseted = false;
                }

                lineLength = lineStringBuilder.Length;
            }

            if (isBreaked || lineLength > 0)
            {
                messageSize.Height += this.lineHeight;
            }

            if (message.HasAttachments)
            {
                messageSize.Height += this.lineHeight * message.Attachments.Count;
            }

            return messageSize;
        }

        private Size GetInspectorSize(Entry entry)
        {
            const double space = 10;
            Size imageSize = new Size(32, 32);
            Size iconSize = new Size(6, 6);
            string title = String.IsNullOrEmpty(entry.Title) && entry.Resource != null ? entry.Resource.ToString() : entry.Title;
            Size inspectorSize = new Size(this.baseWidth - 12, imageSize.Height / 2);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            if (!String.IsNullOrEmpty(title))
            {
                double iconWidth = entry.HasSimilarEntries ? (space - iconSize.Width) / 2 + iconSize.Width : 0;
                double titleWidth = inspectorSize.Width - 30 - imageSize.Width - iconWidth;
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                StringBuilder lineStringBuilder = new StringBuilder();
                int lines = 0;
                bool isBreaked = false;
                bool isReseted = true;

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(title, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                {
                    dictionary.Add(match.Index, match.Length);
                }

                for (int i = 0; i < title.Length; i++)
                {
                    int length;

                    if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), title.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > titleWidth && !isReseted)
                    {
                        lineStringBuilder.Clear();
                        lines++;
                        isBreaked = true;
                    }

                    lineStringBuilder.Append(title[i]);

                    if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                        if (lineStringBuilder.Length > 0)
                        {
                            lineStringBuilder.Remove(0, lineStringBuilder.Length);
                        }

                        lines++;
                        isBreaked = true;
                        isReseted = true;
                    }
                    else if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > titleWidth)
                    {
                        if (lineStringBuilder.Length - 1 > 0)
                        {
                            lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                        }

                        lines++;
                        isBreaked = true;
                        isReseted = true;
                    }
                    else
                    {
                        isReseted = false;
                    }
                }

                if (lineStringBuilder.Length > 0 || isBreaked)
                {
                    lines++;
                }

                inspectorSize.Height += this.lineHeight * lines;
            }

            if (entry.Resource != null && entry.Resource.IsAbsoluteUri)
            {
                string authority = entry.Resource.Authority;
                StringBuilder lineStringBuilder = new StringBuilder();
                int lines = 0;

                for (int i = 0; i < authority.Length; i++)
                {
                    int safeLineLength = lineStringBuilder.Length;

                    lineStringBuilder.Append(authority[i]);

                    if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        if (safeLineLength > 0)
                        {
                            lineStringBuilder.Remove(0, safeLineLength);
                        }

                        lines++;
                    }
                }

                if (lineStringBuilder.Length > 0)
                {
                    lines++;
                }

                inspectorSize.Height += this.lineHeight * lines;
            }

            if (!String.IsNullOrEmpty(entry.Author))
            {
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                StringBuilder lineStringBuilder = new StringBuilder();
                int lines = 0;
                bool isBreaked = false;

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(entry.Author, @"[\p{IsBasicLatin}-[\s]]+\s?"))
                {
                    dictionary.Add(match.Index, match.Length);
                }

                for (int i = 0; i < entry.Author.Length; i++)
                {
                    int length;

                    if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), entry.Author.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        lineStringBuilder.Clear();
                        lines++;
                        isBreaked = true;
                    }

                    lineStringBuilder.Append(entry.Author[i]);

                    if (lineStringBuilder.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        lineStringBuilder.Remove(lineStringBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine.Length);

                        if (lineStringBuilder.Length > 0)
                        {
                            lineStringBuilder.Remove(0, lineStringBuilder.Length);
                        }

                        lines++;
                        isBreaked = true;
                    }
                    else if (lineStringBuilder.Length > 0 && Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.linkBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        if (lineStringBuilder.Length - 1 > 0)
                        {
                            lineStringBuilder.Remove(0, lineStringBuilder.Length - 1);
                        }

                        lines++;
                        isBreaked = true;
                    }
                }

                if (lineStringBuilder.Length > 0 || isBreaked)
                {
                    lines++;
                }

                inspectorSize.Height += this.lineHeight * lines;
            }

            if (entry.Modified.Ticks > 0)
            {
                string modified = entry.Modified.ToString("G", CultureInfo.CurrentCulture);
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                StringBuilder lineStringBuilder = new StringBuilder();
                int lines = 0;

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(modified, @"\S+\s?"))
                {
                    dictionary.Add(match.Index, match.Length);
                }

                for (int i = 0; i < modified.Length; i++)
                {
                    int length;

                    if (dictionary.TryGetValue(i, out length) && Math.Ceiling(new FormattedText(String.Concat(lineStringBuilder.ToString(), modified.Substring(i, length)), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        lineStringBuilder.Clear();
                        lines++;
                    }

                    int safeLineLength = lineStringBuilder.Length;

                    lineStringBuilder.Append(modified[i]);

                    if (Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        if (safeLineLength > 0)
                        {
                            lineStringBuilder.Remove(0, safeLineLength);
                        }

                        lines++;
                    }
                }

                if (lineStringBuilder.Length > 0)
                {
                    lines++;
                }

                inspectorSize.Height += this.lineHeight * lines;
            }

            if (entry.Score.HasValue)
            {
                string score = entry.Score.Value.ToString("F3", CultureInfo.CurrentCulture);
                StringBuilder lineStringBuilder = new StringBuilder();
                int lines = 0;

                for (int i = 0; i < score.Length; i++)
                {
                    int safeLineLength = lineStringBuilder.Length;

                    lineStringBuilder.Append(score[i]);

                    if (space + Math.Ceiling(new FormattedText(lineStringBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.textBrush, pixelsPerDip).WidthIncludingTrailingWhitespace) > inspectorSize.Width - 30 - imageSize.Width)
                    {
                        if (safeLineLength > 0)
                        {
                            lineStringBuilder.Remove(0, safeLineLength);
                        }

                        lines++;
                    }
                }

                if (lineStringBuilder.Length > 0)
                {
                    lines++;
                }

                inspectorSize.Height += this.lineHeight * lines;
            }

            if (entry.HasTags)
            {
                inspectorSize.Height += this.lineHeight;
            }

            if (inspectorSize.Height < imageSize.Height)
            {
                inspectorSize.Height = imageSize.Height;
            }

            return inspectorSize;
        }

        private double GetNextInterval(IEnumerable<Entry> entries, double threshold, int count, bool reverse)
        {
            List<double> scoreList = new List<double>();
            double previous = threshold;
            double interval = 0;

            foreach (Entry entry in from entry in entries where entry.Score.HasValue select entry)
            {
                if (!scoreList.Contains(entry.Score.Value))
                {
                    if (reverse && entry.Score < threshold)
                    {
                        scoreList.Add(entry.Score.Value);
                    }
                    else if (!reverse && entry.Score > threshold)
                    {
                        scoreList.Add(entry.Score.Value);
                    }
                }
            }

            scoreList.Sort(delegate (double d1, double d2)
            {
                if (d1 > d2)
                {
                    return 1;
                }
                else if (d1 < d2)
                {
                    return -1;
                }

                return 0;
            });

            if (Math.Abs(count) > scoreList.Count)
            {
                count = scoreList.Count;
            }

            if (reverse)
            {
                scoreList.Reverse();
            }

            scoreList.GetRange(0, Math.Abs(count)).ForEach(delegate (double d)
            {
                interval += Math.Abs(d - previous);
                previous = d;
            });

            return interval;
        }

        private void UpdateImage(Uri uri, bool ignoreCache)
        {
            if (uri.IsAbsoluteUri)
            {
                System.Configuration.Configuration config1 = null;
                string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

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

                    if (config1.AppSettings.Settings["Cache"] == null)
                    {
                        if (uri.Scheme.Equals("data"))
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                            if (match.Success)
                            {
                                MemoryStream ms = new MemoryStream(Convert.FromBase64String(match.Groups[1].Value));
                                BitmapImage bi = null;

                                try
                                {
                                    bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.StreamSource = ms;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.CreateOptions = BitmapCreateOptions.None;
                                    bi.EndInit();
                                }
                                catch
                                {
                                    bi = null;
                                }
                                finally
                                {
                                    ms.Close();
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }
                        }
                        else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                        {
                            WebRequest webRequest = WebRequest.Create(uri);

                            if (config1.AppSettings.Settings["Timeout"] != null && config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                            {
                                webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                            }

                            if (config1.AppSettings.Settings["UserAgent"] != null)
                            {
                                HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                if (httpWebRequest != null)
                                {
                                    httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                }
                            }

                            Task.Factory.StartNew<MemoryStream>(delegate (object state)
                            {
                                MemoryStream ms = null;

                                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                {
                                    WebRequest request = (WebRequest)state;
                                    WebResponse response = null;
                                    Stream s = null;
                                    BufferedStream bs = null;

                                    try
                                    {
                                        response = request.GetResponse();

                                        if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                        {
                                            s = response.GetResponseStream();
                                            bs = new BufferedStream(s);
                                            s = null;
                                            ms = new MemoryStream();

                                            int i;

                                            while ((i = bs.ReadByte()) != -1)
                                            {
                                                ms.WriteByte((byte)i);
                                            }

                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                    }
                                    catch
                                    {
                                        if (ms != null)
                                        {
                                            ms.Close();
                                            ms = null;
                                        }
                                    }
                                    finally
                                    {
                                        if (bs != null)
                                        {
                                            bs.Close();
                                        }

                                        if (s != null)
                                        {
                                            s.Close();
                                        }

                                        if (response != null)
                                        {
                                            response.Close();
                                        }
                                    }
                                }

                                return ms;
                            }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                            {
                                BitmapImage bi = null;

                                if (task.Result != null)
                                {
                                    try
                                    {
                                        bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.StreamSource = task.Result;
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.CreateOptions = BitmapCreateOptions.None;
                                        bi.EndInit();
                                    }
                                    catch
                                    {
                                        bi = null;
                                    }
                                    finally
                                    {
                                        task.Result.Close();
                                    }
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                    else
                    {
                        System.Security.Cryptography.SHA512CryptoServiceProvider sha512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (byte b in sha512.ComputeHash(Encoding.UTF8.GetBytes(uri.AbsoluteUri)))
                        {
                            stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                        }

                        stringBuilder.Append(System.IO.Path.GetExtension(uri.AbsolutePath));

                        if (stringBuilder.ToString().IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                        {
                            string path1 = System.IO.Path.IsPathRooted(config1.AppSettings.Settings["Cache"].Value) ? config1.AppSettings.Settings["Cache"].Value : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config1.AppSettings.Settings["Cache"].Value);
                            string path2 = System.IO.Path.Combine(path1, stringBuilder.ToString());

                            if (!ignoreCache && File.Exists(path2))
                            {
                                Task.Factory.StartNew<MemoryStream>(delegate
                                {
                                    MemoryStream ms = null;
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
                                        ms = new MemoryStream();

                                        byte[] buffer = new byte[fs.Length];
                                        int bytesRead;

                                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            ms.Write(buffer, 0, bytesRead);
                                        }

                                        ms.Seek(0, SeekOrigin.Begin);
                                    }
                                    catch
                                    {
                                        if (ms != null)
                                        {
                                            ms.Close();
                                            ms = null;
                                        }
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }

                                    return ms;
                                }).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                            else if (uri.Scheme.Equals("data"))
                            {
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                if (match.Success)
                                {
                                    byte[] bytes = Convert.FromBase64String(match.Groups[1].Value);

                                    Task.Factory.StartNew<MemoryStream>(delegate
                                    {
                                        FileStream fs = null;
                                        MemoryStream ms = null;

                                        try
                                        {
                                            if (Directory.Exists(path1))
                                            {
                                                foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                {
                                                    File.Delete(filename);
                                                }
                                            }
                                            else
                                            {
                                                Directory.CreateDirectory(path1);
                                            }

                                            fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                            ms = new MemoryStream();

                                            foreach (byte b in bytes)
                                            {
                                                fs.WriteByte(b);
                                                ms.WriteByte(b);
                                            }

                                            fs.Flush();
                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }

                                        return ms;
                                    }, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                    {
                                        BitmapImage bi = null;

                                        if (task.Result != null)
                                        {
                                            try
                                            {
                                                bi = new BitmapImage();
                                                bi.BeginInit();
                                                bi.StreamSource = task.Result;
                                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                                bi.CreateOptions = BitmapCreateOptions.None;
                                                bi.EndInit();
                                            }
                                            catch
                                            {
                                                bi = null;
                                            }
                                            finally
                                            {
                                                task.Result.Close();
                                            }
                                        }

                                        if (this.imageUriHashSet.Contains(uri))
                                        {
                                            if (this.imageDictionary.ContainsKey(uri))
                                            {
                                                this.imageDictionary[uri] = bi;
                                            }
                                            else
                                            {
                                                this.imageDictionary.Add(uri, bi);
                                            }
                                        }
                                    }, TaskScheduler.FromCurrentSynchronizationContext());
                                }
                            }
                            else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                            {
                                WebRequest webRequest = WebRequest.Create(uri);

                                if (config1.AppSettings.Settings["Timeout"] != null && config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                                {
                                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                }

                                if (config1.AppSettings.Settings["UserAgent"] != null)
                                {
                                    HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                    if (httpWebRequest != null)
                                    {
                                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                    }
                                }

                                Task.Factory.StartNew<MemoryStream>(delegate (object state)
                                {
                                    MemoryStream ms = null;

                                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                    {
                                        WebRequest request = (WebRequest)state;
                                        WebResponse response = null;
                                        Stream s = null;
                                        BufferedStream bs = null;
                                        FileStream fs = null;

                                        try
                                        {
                                            response = request.GetResponse();

                                            if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                            {
                                                s = response.GetResponseStream();
                                                bs = new BufferedStream(s);
                                                s = null;

                                                if (Directory.Exists(path1))
                                                {
                                                    foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                    {
                                                        File.Delete(filename);
                                                    }
                                                }
                                                else
                                                {
                                                    Directory.CreateDirectory(path1);
                                                }

                                                fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                                ms = new MemoryStream();

                                                int i;

                                                while ((i = bs.ReadByte()) != -1)
                                                {
                                                    byte b = (byte)i;

                                                    fs.WriteByte(b);
                                                    ms.WriteByte(b);
                                                }

                                                fs.Flush();
                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }

                                            if (bs != null)
                                            {
                                                bs.Close();
                                            }

                                            if (s != null)
                                            {
                                                s.Close();
                                            }

                                            if (response != null)
                                            {
                                                response.Close();
                                            }
                                        }
                                    }

                                    return ms;
                                }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        }
                        else if (uri.Scheme.Equals("data"))
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                            if (match.Success)
                            {
                                MemoryStream ms = new MemoryStream(Convert.FromBase64String(match.Groups[1].Value));
                                BitmapImage bi = null;

                                try
                                {
                                    bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.StreamSource = ms;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.CreateOptions = BitmapCreateOptions.None;
                                    bi.EndInit();
                                }
                                catch
                                {
                                    bi = null;
                                }
                                finally
                                {
                                    ms.Close();
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }
                        }
                        else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                        {
                            WebRequest webRequest = WebRequest.Create(uri);

                            if (config1.AppSettings.Settings["Timeout"] != null && config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                            {
                                webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                            }

                            if (config1.AppSettings.Settings["UserAgent"] != null)
                            {
                                HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                if (httpWebRequest != null)
                                {
                                    httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                }
                            }

                            Task.Factory.StartNew<MemoryStream>(delegate (object state)
                            {
                                MemoryStream ms = null;

                                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                {
                                    WebRequest request = (WebRequest)state;
                                    WebResponse response = null;
                                    Stream s = null;
                                    BufferedStream bs = null;

                                    try
                                    {
                                        response = request.GetResponse();

                                        if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                        {
                                            s = response.GetResponseStream();
                                            bs = new BufferedStream(s);
                                            s = null;
                                            ms = new MemoryStream();

                                            int i;

                                            while ((i = bs.ReadByte()) != -1)
                                            {
                                                ms.WriteByte((byte)i);
                                            }

                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                    }
                                    catch
                                    {
                                        if (ms != null)
                                        {
                                            ms.Close();
                                            ms = null;
                                        }
                                    }
                                    finally
                                    {
                                        if (bs != null)
                                        {
                                            bs.Close();
                                        }

                                        if (s != null)
                                        {
                                            s.Close();
                                        }

                                        if (response != null)
                                        {
                                            response.Close();
                                        }
                                    }
                                }

                                return ms;
                            }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                            {
                                BitmapImage bi = null;

                                if (task.Result != null)
                                {
                                    try
                                    {
                                        bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.StreamSource = task.Result;
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.CreateOptions = BitmapCreateOptions.None;
                                        bi.EndInit();
                                    }
                                    catch
                                    {
                                        bi = null;
                                    }
                                    finally
                                    {
                                        task.Result.Close();
                                    }
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                }
                else
                {
                    System.Configuration.Configuration config2 = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

                    if (config1.AppSettings.Settings["Cache"] == null)
                    {
                        if (config2.AppSettings.Settings["Cache"] == null)
                        {
                            if (uri.Scheme.Equals("data"))
                            {
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                if (match.Success)
                                {
                                    MemoryStream ms = new MemoryStream(Convert.FromBase64String(match.Groups[1].Value));
                                    BitmapImage bi = null;

                                    try
                                    {
                                        bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.StreamSource = ms;
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.CreateOptions = BitmapCreateOptions.None;
                                        bi.EndInit();
                                    }
                                    catch
                                    {
                                        bi = null;
                                    }
                                    finally
                                    {
                                        ms.Close();
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }
                            }
                            else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                            {
                                WebRequest webRequest = WebRequest.Create(uri);

                                if (config1.AppSettings.Settings["Timeout"] != null && config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                                {
                                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                }

                                if (config1.AppSettings.Settings["UserAgent"] != null)
                                {
                                    HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                    if (httpWebRequest != null)
                                    {
                                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                    }
                                }

                                Task.Factory.StartNew<MemoryStream>(delegate (object state)
                                {
                                    MemoryStream ms = null;

                                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                    {
                                        WebRequest request = (WebRequest)state;
                                        WebResponse response = null;
                                        Stream s = null;
                                        BufferedStream bs = null;

                                        try
                                        {
                                            response = request.GetResponse();

                                            if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                            {
                                                s = response.GetResponseStream();
                                                bs = new BufferedStream(s);
                                                s = null;
                                                ms = new MemoryStream();

                                                int i;

                                                while ((i = bs.ReadByte()) != -1)
                                                {
                                                    ms.WriteByte((byte)i);
                                                }

                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (bs != null)
                                            {
                                                bs.Close();
                                            }

                                            if (s != null)
                                            {
                                                s.Close();
                                            }

                                            if (response != null)
                                            {
                                                response.Close();
                                            }
                                        }
                                    }

                                    return ms;
                                }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        }
                        else
                        {
                            System.Security.Cryptography.SHA512CryptoServiceProvider sha512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
                            StringBuilder stringBuilder = new StringBuilder();

                            foreach (byte b in sha512.ComputeHash(Encoding.UTF8.GetBytes(uri.AbsoluteUri)))
                            {
                                stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                            }

                            stringBuilder.Append(System.IO.Path.GetExtension(uri.AbsolutePath));

                            if (stringBuilder.ToString().IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                            {
                                string path1 = System.IO.Path.IsPathRooted(config2.AppSettings.Settings["Cache"].Value) ? config2.AppSettings.Settings["Cache"].Value : System.IO.Path.Combine(directory, config2.AppSettings.Settings["Cache"].Value);

                                if (!Directory.Exists(path1))
                                {
                                    path1 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config2.AppSettings.Settings["Cache"].Value);
                                }

                                string path2 = System.IO.Path.Combine(path1, stringBuilder.ToString());

                                if (!ignoreCache && File.Exists(path2))
                                {
                                    Task.Factory.StartNew<MemoryStream>(delegate
                                    {
                                        MemoryStream ms = null;
                                        FileStream fs = null;

                                        try
                                        {
                                            fs = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
                                            ms = new MemoryStream();

                                            byte[] buffer = new byte[fs.Length];
                                            int bytesRead;

                                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                            {
                                                ms.Write(buffer, 0, bytesRead);
                                            }

                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }

                                        return ms;
                                    }).ContinueWith(delegate (Task<MemoryStream> task)
                                    {
                                        BitmapImage bi = null;

                                        if (task.Result != null)
                                        {
                                            try
                                            {
                                                bi = new BitmapImage();
                                                bi.BeginInit();
                                                bi.StreamSource = task.Result;
                                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                                bi.CreateOptions = BitmapCreateOptions.None;
                                                bi.EndInit();
                                            }
                                            catch
                                            {
                                                bi = null;
                                            }
                                            finally
                                            {
                                                task.Result.Close();
                                            }
                                        }

                                        if (this.imageUriHashSet.Contains(uri))
                                        {
                                            if (this.imageDictionary.ContainsKey(uri))
                                            {
                                                this.imageDictionary[uri] = bi;
                                            }
                                            else
                                            {
                                                this.imageDictionary.Add(uri, bi);
                                            }
                                        }
                                    }, TaskScheduler.FromCurrentSynchronizationContext());
                                }
                                else if (uri.Scheme.Equals("data"))
                                {
                                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                    if (match.Success)
                                    {
                                        byte[] bytes = Convert.FromBase64String(match.Groups[1].Value);

                                        Task.Factory.StartNew<MemoryStream>(delegate
                                        {
                                            FileStream fs = null;
                                            MemoryStream ms = null;

                                            try
                                            {
                                                if (Directory.Exists(path1))
                                                {
                                                    foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                    {
                                                        File.Delete(filename);
                                                    }
                                                }
                                                else
                                                {
                                                    Directory.CreateDirectory(path1);
                                                }

                                                fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                                ms = new MemoryStream();

                                                foreach (byte b in bytes)
                                                {
                                                    fs.WriteByte(b);
                                                    ms.WriteByte(b);
                                                }

                                                fs.Flush();
                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                            catch
                                            {
                                                if (ms != null)
                                                {
                                                    ms.Close();
                                                    ms = null;
                                                }
                                            }
                                            finally
                                            {
                                                if (fs != null)
                                                {
                                                    fs.Close();
                                                }
                                            }

                                            return ms;
                                        }, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                        {
                                            BitmapImage bi = null;

                                            if (task.Result != null)
                                            {
                                                try
                                                {
                                                    bi = new BitmapImage();
                                                    bi.BeginInit();
                                                    bi.StreamSource = task.Result;
                                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                                    bi.CreateOptions = BitmapCreateOptions.None;
                                                    bi.EndInit();
                                                }
                                                catch
                                                {
                                                    bi = null;
                                                }
                                                finally
                                                {
                                                    task.Result.Close();
                                                }
                                            }

                                            if (this.imageUriHashSet.Contains(uri))
                                            {
                                                if (this.imageDictionary.ContainsKey(uri))
                                                {
                                                    this.imageDictionary[uri] = bi;
                                                }
                                                else
                                                {
                                                    this.imageDictionary.Add(uri, bi);
                                                }
                                            }
                                        }, TaskScheduler.FromCurrentSynchronizationContext());
                                    }
                                }
                                else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                                {
                                    WebRequest webRequest = WebRequest.Create(uri);

                                    if (config1.AppSettings.Settings["Timeout"] == null)
                                    {
                                        if (config2.AppSettings.Settings["Timeout"] != null && config2.AppSettings.Settings["Timeout"].Value.Length > 0)
                                        {
                                            webRequest.Timeout = Int32.Parse(config2.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                        }
                                    }
                                    else if (config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                                    {
                                        webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                    }

                                    if (config1.AppSettings.Settings["UserAgent"] == null)
                                    {
                                        if (config2.AppSettings.Settings["UserAgent"] != null)
                                        {
                                            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                            if (httpWebRequest != null)
                                            {
                                                httpWebRequest.UserAgent = config2.AppSettings.Settings["UserAgent"].Value;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                        if (httpWebRequest != null)
                                        {
                                            httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                        }
                                    }

                                    Task.Factory.StartNew<MemoryStream>(delegate (object state)
                                    {
                                        MemoryStream ms = null;

                                        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                        {
                                            WebRequest request = (WebRequest)state;
                                            WebResponse response = null;
                                            Stream s = null;
                                            BufferedStream bs = null;
                                            FileStream fs = null;

                                            try
                                            {
                                                response = request.GetResponse();

                                                if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                                {
                                                    s = response.GetResponseStream();
                                                    bs = new BufferedStream(s);
                                                    s = null;

                                                    if (Directory.Exists(path1))
                                                    {
                                                        foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                        {
                                                            File.Delete(filename);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Directory.CreateDirectory(path1);
                                                    }

                                                    fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                                    ms = new MemoryStream();

                                                    int i;

                                                    while ((i = bs.ReadByte()) != -1)
                                                    {
                                                        byte b = (byte)i;

                                                        fs.WriteByte(b);
                                                        ms.WriteByte(b);
                                                    }

                                                    fs.Flush();
                                                    ms.Seek(0, SeekOrigin.Begin);
                                                }
                                            }
                                            catch
                                            {
                                                if (ms != null)
                                                {
                                                    ms.Close();
                                                    ms = null;
                                                }
                                            }
                                            finally
                                            {
                                                if (fs != null)
                                                {
                                                    fs.Close();
                                                }

                                                if (bs != null)
                                                {
                                                    bs.Close();
                                                }

                                                if (s != null)
                                                {
                                                    s.Close();
                                                }

                                                if (response != null)
                                                {
                                                    response.Close();
                                                }
                                            }
                                        }

                                        return ms;
                                    }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                    {
                                        BitmapImage bi = null;

                                        if (task.Result != null)
                                        {
                                            try
                                            {
                                                bi = new BitmapImage();
                                                bi.BeginInit();
                                                bi.StreamSource = task.Result;
                                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                                bi.CreateOptions = BitmapCreateOptions.None;
                                                bi.EndInit();
                                            }
                                            catch
                                            {
                                                bi = null;
                                            }
                                            finally
                                            {
                                                task.Result.Close();
                                            }
                                        }

                                        if (this.imageUriHashSet.Contains(uri))
                                        {
                                            if (this.imageDictionary.ContainsKey(uri))
                                            {
                                                this.imageDictionary[uri] = bi;
                                            }
                                            else
                                            {
                                                this.imageDictionary.Add(uri, bi);
                                            }
                                        }
                                    }, TaskScheduler.FromCurrentSynchronizationContext());
                                }
                            }
                            else if (uri.Scheme.Equals("data"))
                            {
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                if (match.Success)
                                {
                                    MemoryStream ms = new MemoryStream(Convert.FromBase64String(match.Groups[1].Value));
                                    BitmapImage bi = null;

                                    try
                                    {
                                        bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.StreamSource = ms;
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.CreateOptions = BitmapCreateOptions.None;
                                        bi.EndInit();
                                    }
                                    catch
                                    {
                                        bi = null;
                                    }
                                    finally
                                    {
                                        ms.Close();
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }
                            }
                            else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                            {
                                WebRequest webRequest = WebRequest.Create(uri);

                                if (config1.AppSettings.Settings["Timeout"] == null)
                                {
                                    if (config2.AppSettings.Settings["Timeout"] != null && config2.AppSettings.Settings["Timeout"].Value.Length > 0)
                                    {
                                        webRequest.Timeout = Int32.Parse(config2.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                    }
                                }
                                else if (config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                                {
                                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                }

                                if (config1.AppSettings.Settings["UserAgent"] != null)
                                {
                                    if (config2.AppSettings.Settings["UserAgent"] != null)
                                    {
                                        HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                        if (httpWebRequest != null)
                                        {
                                            httpWebRequest.UserAgent = config2.AppSettings.Settings["UserAgent"].Value;
                                        }
                                    }
                                }
                                else
                                {
                                    HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                    if (httpWebRequest != null)
                                    {
                                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                    }
                                }

                                Task.Factory.StartNew<MemoryStream>(delegate (object state)
                                {
                                    MemoryStream ms = null;

                                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                    {
                                        WebRequest request = (WebRequest)state;
                                        WebResponse response = null;
                                        Stream s = null;
                                        BufferedStream bs = null;

                                        try
                                        {
                                            response = request.GetResponse();

                                            if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                            {
                                                s = response.GetResponseStream();
                                                bs = new BufferedStream(s);
                                                s = null;
                                                ms = new MemoryStream();

                                                int i;

                                                while ((i = bs.ReadByte()) != -1)
                                                {
                                                    ms.WriteByte((byte)i);
                                                }

                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (bs != null)
                                            {
                                                bs.Close();
                                            }

                                            if (s != null)
                                            {
                                                s.Close();
                                            }

                                            if (response != null)
                                            {
                                                response.Close();
                                            }
                                        }
                                    }

                                    return ms;
                                }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        }
                    }
                    else
                    {
                        System.Security.Cryptography.SHA512CryptoServiceProvider sha512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (byte b in sha512.ComputeHash(Encoding.UTF8.GetBytes(uri.AbsoluteUri)))
                        {
                            stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                        }

                        stringBuilder.Append(System.IO.Path.GetExtension(uri.AbsolutePath));

                        if (stringBuilder.ToString().IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                        {
                            string path1 = System.IO.Path.IsPathRooted(config1.AppSettings.Settings["Cache"].Value) ? config1.AppSettings.Settings["Cache"].Value : System.IO.Path.Combine(directory, config1.AppSettings.Settings["Cache"].Value);

                            if (!Directory.Exists(path1))
                            {
                                path1 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), config2.AppSettings.Settings["Cache"].Value);
                            }

                            string path2 = System.IO.Path.Combine(path1, stringBuilder.ToString());

                            if (!ignoreCache && File.Exists(path2))
                            {
                                Task.Factory.StartNew<MemoryStream>(delegate
                                {
                                    MemoryStream ms = null;
                                    FileStream fs = null;

                                    try
                                    {
                                        fs = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
                                        ms = new MemoryStream();

                                        byte[] buffer = new byte[fs.Length];
                                        int bytesRead;

                                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            ms.Write(buffer, 0, bytesRead);
                                        }

                                        ms.Seek(0, SeekOrigin.Begin);
                                    }
                                    catch
                                    {
                                        if (ms != null)
                                        {
                                            ms.Close();
                                            ms = null;
                                        }
                                    }
                                    finally
                                    {
                                        if (fs != null)
                                        {
                                            fs.Close();
                                        }
                                    }

                                    return ms;
                                }).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                            else if (uri.Scheme.Equals("data"))
                            {
                                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                                if (match.Success)
                                {
                                    byte[] bytes = Convert.FromBase64String(match.Groups[1].Value);

                                    Task.Factory.StartNew<MemoryStream>(delegate
                                    {
                                        FileStream fs = null;
                                        MemoryStream ms = null;

                                        try
                                        {
                                            if (Directory.Exists(path1))
                                            {
                                                foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                {
                                                    File.Delete(filename);
                                                }
                                            }
                                            else
                                            {
                                                Directory.CreateDirectory(path1);
                                            }

                                            fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                            ms = new MemoryStream();

                                            foreach (byte b in bytes)
                                            {
                                                fs.WriteByte(b);
                                                ms.WriteByte(b);
                                            }

                                            fs.Flush();
                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }
                                        }

                                        return ms;
                                    }, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                    {
                                        BitmapImage bi = null;

                                        if (task.Result != null)
                                        {
                                            try
                                            {
                                                bi = new BitmapImage();
                                                bi.BeginInit();
                                                bi.StreamSource = task.Result;
                                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                                bi.CreateOptions = BitmapCreateOptions.None;
                                                bi.EndInit();
                                            }
                                            catch
                                            {
                                                bi = null;
                                            }
                                            finally
                                            {
                                                task.Result.Close();
                                            }
                                        }

                                        if (this.imageUriHashSet.Contains(uri))
                                        {
                                            if (this.imageDictionary.ContainsKey(uri))
                                            {
                                                this.imageDictionary[uri] = bi;
                                            }
                                            else
                                            {
                                                this.imageDictionary.Add(uri, bi);
                                            }
                                        }
                                    }, TaskScheduler.FromCurrentSynchronizationContext());
                                }
                            }
                            else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                            {
                                WebRequest webRequest = WebRequest.Create(uri);

                                if (config1.AppSettings.Settings["Timeout"] == null)
                                {
                                    if (config2.AppSettings.Settings["Timeout"] != null && config2.AppSettings.Settings["Timeout"].Value.Length > 0)
                                    {
                                        webRequest.Timeout = Int32.Parse(config2.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                    }
                                }
                                else if (config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                                {
                                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                }

                                if (config1.AppSettings.Settings["UserAgent"] == null)
                                {
                                    if (config2.AppSettings.Settings["UserAgent"] != null)
                                    {
                                        HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                        if (httpWebRequest != null)
                                        {
                                            httpWebRequest.UserAgent = config2.AppSettings.Settings["UserAgent"].Value;
                                        }
                                    }
                                }
                                else
                                {
                                    HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                    if (httpWebRequest != null)
                                    {
                                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                    }
                                }

                                Task.Factory.StartNew<MemoryStream>(delegate (object state)
                                {
                                    MemoryStream ms = null;

                                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                    {
                                        WebRequest request = (WebRequest)state;
                                        WebResponse response = null;
                                        Stream s = null;
                                        BufferedStream bs = null;
                                        FileStream fs = null;

                                        try
                                        {
                                            response = request.GetResponse();

                                            if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                            {
                                                s = response.GetResponseStream();
                                                bs = new BufferedStream(s);
                                                s = null;

                                                if (Directory.Exists(path1))
                                                {
                                                    foreach (string filename in from filename in Directory.EnumerateFiles(path1, "*", SearchOption.TopDirectoryOnly) let expiredDateTime = DateTime.Now - new TimeSpan(7, 0, 0, 0) let creationTime = File.GetCreationTime(filename) where creationTime < expiredDateTime select filename)
                                                    {
                                                        File.Delete(filename);
                                                    }
                                                }
                                                else
                                                {
                                                    Directory.CreateDirectory(path1);
                                                }

                                                fs = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None);
                                                ms = new MemoryStream();

                                                int i;

                                                while ((i = bs.ReadByte()) != -1)
                                                {
                                                    byte b = (byte)i;

                                                    fs.WriteByte(b);
                                                    ms.WriteByte(b);
                                                }

                                                fs.Flush();
                                                ms.Seek(0, SeekOrigin.Begin);
                                            }
                                        }
                                        catch
                                        {
                                            if (ms != null)
                                            {
                                                ms.Close();
                                                ms = null;
                                            }
                                        }
                                        finally
                                        {
                                            if (fs != null)
                                            {
                                                fs.Close();
                                            }

                                            if (bs != null)
                                            {
                                                bs.Close();
                                            }

                                            if (s != null)
                                            {
                                                s.Close();
                                            }

                                            if (response != null)
                                            {
                                                response.Close();
                                            }
                                        }
                                    }

                                    return ms;
                                }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                                {
                                    BitmapImage bi = null;

                                    if (task.Result != null)
                                    {
                                        try
                                        {
                                            bi = new BitmapImage();
                                            bi.BeginInit();
                                            bi.StreamSource = task.Result;
                                            bi.CacheOption = BitmapCacheOption.OnLoad;
                                            bi.CreateOptions = BitmapCreateOptions.None;
                                            bi.EndInit();
                                        }
                                        catch
                                        {
                                            bi = null;
                                        }
                                        finally
                                        {
                                            task.Result.Close();
                                        }
                                    }

                                    if (this.imageUriHashSet.Contains(uri))
                                    {
                                        if (this.imageDictionary.ContainsKey(uri))
                                        {
                                            this.imageDictionary[uri] = bi;
                                        }
                                        else
                                        {
                                            this.imageDictionary.Add(uri, bi);
                                        }
                                    }
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        }
                        else if (uri.Scheme.Equals("data"))
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(uri.LocalPath, "image/(?:(?:x-)?bmp|gif|jpeg|png|tiff(?:-fx)?);base64,(?<1>.+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                            if (match.Success)
                            {
                                MemoryStream ms = new MemoryStream(Convert.FromBase64String(match.Groups[1].Value));
                                BitmapImage bi = null;

                                try
                                {
                                    bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.StreamSource = ms;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.CreateOptions = BitmapCreateOptions.None;
                                    bi.EndInit();
                                }
                                catch
                                {
                                    bi = null;
                                }
                                finally
                                {
                                    ms.Close();
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }
                        }
                        else if (uri.Scheme.Equals(Uri.UriSchemeFile) || uri.Scheme.Equals(Uri.UriSchemeFtp) || uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                        {
                            WebRequest webRequest = WebRequest.Create(uri);

                            if (config1.AppSettings.Settings["Timeout"] == null)
                            {
                                if (config2.AppSettings.Settings["Timeout"] != null && config2.AppSettings.Settings["Timeout"].Value.Length > 0)
                                {
                                    webRequest.Timeout = Int32.Parse(config2.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                                }
                            }
                            else if (config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                            {
                                webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                            }

                            if (config1.AppSettings.Settings["UserAgent"] == null)
                            {
                                if (config2.AppSettings.Settings["UserAgent"] != null)
                                {
                                    HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                    if (httpWebRequest != null)
                                    {
                                        httpWebRequest.UserAgent = config2.AppSettings.Settings["UserAgent"].Value;
                                    }
                                }
                            }
                            else
                            {
                                HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                                if (httpWebRequest != null)
                                {
                                    httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                                }
                            }

                            Task.Factory.StartNew<MemoryStream>(delegate (object state)
                            {
                                MemoryStream ms = null;

                                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                                {
                                    WebRequest request = (WebRequest)state;
                                    WebResponse response = null;
                                    Stream s = null;
                                    BufferedStream bs = null;

                                    try
                                    {
                                        response = request.GetResponse();

                                        if (System.Text.RegularExpressions.Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                        {
                                            s = response.GetResponseStream();
                                            bs = new BufferedStream(s);
                                            s = null;
                                            ms = new MemoryStream();

                                            int i;

                                            while ((i = bs.ReadByte()) != -1)
                                            {
                                                ms.WriteByte((byte)i);
                                            }

                                            ms.Seek(0, SeekOrigin.Begin);
                                        }
                                    }
                                    catch
                                    {
                                        if (ms != null)
                                        {
                                            ms.Close();
                                            ms = null;
                                        }
                                    }
                                    finally
                                    {
                                        if (bs != null)
                                        {
                                            bs.Close();
                                        }

                                        if (s != null)
                                        {
                                            s.Close();
                                        }

                                        if (response != null)
                                        {
                                            response.Close();
                                        }
                                    }
                                }

                                return ms;
                            }, webRequest, TaskCreationOptions.LongRunning).ContinueWith(delegate (Task<MemoryStream> task)
                            {
                                BitmapImage bi = null;

                                if (task.Result != null)
                                {
                                    try
                                    {
                                        bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.StreamSource = task.Result;
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.CreateOptions = BitmapCreateOptions.None;
                                        bi.EndInit();
                                    }
                                    catch
                                    {
                                        bi = null;
                                    }
                                    finally
                                    {
                                        task.Result.Close();
                                    }
                                }

                                if (this.imageUriHashSet.Contains(uri))
                                {
                                    if (this.imageDictionary.ContainsKey(uri))
                                    {
                                        this.imageDictionary[uri] = bi;
                                    }
                                    else
                                    {
                                        this.imageDictionary.Add(uri, bi);
                                    }
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                }
            }
            else
            {
                Task.Factory.StartNew<MemoryStream>(delegate (object state)
                {
                    MemoryStream ms = null;
                    FileStream fs = null;

                    try
                    {
                        fs = new FileStream((string)state, FileMode.Open, FileAccess.Read, FileShare.Read);
                        ms = new MemoryStream();

                        byte[] buffer = new byte[fs.Length];
                        int bytesRead;

                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                    }
                    catch
                    {
                        if (ms != null)
                        {
                            ms.Close();
                            ms = null;
                        }
                    }
                    finally
                    {
                        if (fs != null)
                        {
                            fs.Close();
                        }
                    }

                    return ms;
                }, uri.ToString()).ContinueWith(delegate (Task<MemoryStream> task)
                {
                    BitmapImage bi = null;

                    if (task.Result != null)
                    {
                        try
                        {
                            bi = new BitmapImage();
                            bi.BeginInit();
                            bi.StreamSource = task.Result;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.CreateOptions = BitmapCreateOptions.None;
                            bi.EndInit();
                        }
                        catch
                        {
                            bi = null;
                        }
                        finally
                        {
                            task.Result.Close();
                        }
                    }

                    if (this.imageUriHashSet.Contains(uri))
                    {
                        if (this.imageDictionary.ContainsKey(uri))
                        {
                            this.imageDictionary[uri] = bi;
                        }
                        else
                        {
                            this.imageDictionary.Add(uri, bi);
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }
}
