using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Apricot
{
	/// <summary>
	/// Summary description for Script.
	/// </summary>
    public sealed class Script
	{
        public event EventHandler Start = null;
        public event EventHandler Stop = null;
        private static readonly Lazy<Script> instance = new Lazy<Script>(delegate { return new Script(); });
        private readonly long activateThreshold = Int64.MaxValue;
        private bool isEnabled = false;
        private System.Windows.Threading.DispatcherTimer pollingTimer = null;
        private System.Windows.Threading.DispatcherTimer updateTimer = null;
        private Collection<Character> characterCollection = null;
        private Collection<Word> wordCollection = null;
        private Collection<Sequence> sequenceCollection = null;
        private Dictionary<string, string> sequenceStateDictionary = null;
        private Queue<Sequence> sequenceQueue = null;
        private Dictionary<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> cacheDictionary = null;
        private List<string> recentTermList = null;
        private Queue<Entry> activateEntryQueue = null;
        private DateTime lastPolledDateTime;
        private DateTime lastUpdatedDateTime;
        private TimeSpan idleTimeSpan = TimeSpan.Zero;
        
        public static Script Instance
        {
            get
            {
                return instance.Value;
            }
        }

        public Collection<Character> Characters
        {
            get
            {
                return this.characterCollection;
            }
        }

        public Collection<Word> Words
        {
            get
            {
                return this.wordCollection;
            }
        }

        public Collection<Sequence> Sequences
        {
            get
            {
                return this.sequenceCollection;
            }
        }

        public bool Enabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                if (value)
                {
                    List<Sequence> preparedSequenceList = new List<Sequence>();

                    this.isEnabled = true;
                    this.sequenceQueue.Clear();
                    this.sequenceStateDictionary.Clear();
                    this.cacheDictionary.Clear();
                    this.recentTermList.Clear();
                    this.activateEntryQueue.Clear();
                    this.idleTimeSpan = TimeSpan.Zero;
                    this.lastPolledDateTime = this.lastUpdatedDateTime = DateTime.Now;

                    if (this.pollingTimer != null)
                    {
                        this.pollingTimer.Start();
                    }

                    if (this.updateTimer != null)
                    {
                        this.updateTimer.Start();
                    }

                    foreach (Character character in this.characterCollection)
                    {
                        Sequence sequence = new Sequence();

                        sequence.Owner = character.Name;

                        preparedSequenceList.Add(sequence);
                    }

                    preparedSequenceList.AddRange(Prepare(from sequence in this.sequenceCollection where sequence.Name.Equals("Start") select sequence, null));

                    TryEnqueue(preparedSequenceList);

                    // Rise the Start event.
                    if (this.Start != null)
                    {
                        this.Start(this, new EventArgs());
                    }
                }
                else
                {
                    List<Sequence> preparedSequenceList = new List<Sequence>(Prepare(from sequence in this.sequenceCollection where sequence.Name.Equals("Stop") select sequence, null));

                    this.sequenceQueue.Clear();

                    foreach (Character character in this.characterCollection)
                    {
                        Sequence sequence = new Sequence();

                        sequence.Owner = character.Name;

                        preparedSequenceList.Add(sequence);
                    }

                    TryEnqueue(preparedSequenceList);

                    // Rise the Stop event.
                    if (this.Stop != null)
                    {
                        this.Stop(this, new EventArgs());
                    }

                    if (this.updateTimer != null)
                    {
                        this.updateTimer.Stop();
                    }

                    if (this.pollingTimer != null)
                    {
                        this.pollingTimer.Stop();
                    }

                    this.isEnabled = false;
                }
            }
        }

        private Script()
        {
            //
            // TODO: Add constructor logic here
            //
            this.characterCollection = new Collection<Character>();
            this.wordCollection = new Collection<Word>();
            this.sequenceCollection = new Collection<Sequence>();
            this.sequenceStateDictionary = new Dictionary<string, string>();
            this.sequenceQueue = new Queue<Sequence>();
            this.cacheDictionary = new Dictionary<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>>();
            this.recentTermList = new List<string>();
            this.activateEntryQueue = new Queue<Entry>();
            this.lastPolledDateTime = DateTime.Now;
            this.lastUpdatedDateTime = DateTime.Now;

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

            if (config.AppSettings.Settings["PollingInterval"] != null)
            {
                if (config.AppSettings.Settings["PollingInterval"].Value.Length > 0)
                {
                    this.pollingTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
                    this.pollingTimer.Tick += new EventHandler(delegate
                    {
                        DateTime nowDateTime = DateTime.Now;

                        for (DateTime dateTime = this.lastPolledDateTime.AddSeconds(1); dateTime <= nowDateTime; dateTime = dateTime.AddSeconds(1))
                        {
                            Tick(dateTime);
                        }
                        
                        if (this.sequenceQueue.Count > 0)
                        {
                            this.idleTimeSpan = TimeSpan.Zero;
                        }
                        else
                        {
                            this.idleTimeSpan += nowDateTime - this.lastPolledDateTime;
                        }

                        if (this.idleTimeSpan.Ticks >= this.activateThreshold)
                        {
                            Activate();
                        }

                        if (this.idleTimeSpan.Ticks > 0)
                        {
                            Idle();
                        }

                        this.lastPolledDateTime = nowDateTime;
                    });
                    this.pollingTimer.Interval = TimeSpan.Parse(config.AppSettings.Settings["PollingInterval"].Value, CultureInfo.InvariantCulture);
                }
            }

            if (config.AppSettings.Settings["UpdateInterval"] != null)
            {
                if (config.AppSettings.Settings["UpdateInterval"].Value.Length > 0)
                {
                    this.updateTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
                    this.updateTimer.Tick += new EventHandler(delegate
                    {
                        Update();
                    });
                    this.updateTimer.Interval = TimeSpan.Parse(config.AppSettings.Settings["UpdateInterval"].Value, CultureInfo.InvariantCulture);
                }
            }

            if (config.AppSettings.Settings["ActivateThreshold"] != null)
            {
                if (config.AppSettings.Settings["ActivateThreshold"].Value.Length > 0)
                {
                    this.activateThreshold = Int64.Parse(config.AppSettings.Settings["ActivateThreshold"].Value, CultureInfo.InvariantCulture);
                }
            }
        }

        public void Load()
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
                directory = null;
            }

            if (config.AppSettings.Settings["Words"] != null)
            {
                using (FileStream fs = new FileStream(directory == null ? config.AppSettings.Settings["Words"].Value : Path.Combine(directory, config.AppSettings.Settings["Words"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Word[]));

                    foreach (Word word in (Word[])serializer.Deserialize(fs))
                    {
                        this.wordCollection.Add(word);
                    }
                }
            }

            if (config.AppSettings.Settings["Characters"] != null)
            {
                List<string> pathList = new List<string>();

                using (FileStream fs = new FileStream(directory == null ? config.AppSettings.Settings["Characters"].Value : Path.Combine(directory, config.AppSettings.Settings["Characters"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Character[]));

                    foreach (Character character in (Character[])serializer.Deserialize(fs))
                    {
                        if (!pathList.Exists(delegate(string path)
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

                        this.characterCollection.Add(character);
                    }
                }

                pathList.ForEach(delegate(string path)
                {
                    Parse(path);
                });
            }
        }

        public void Save()
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
                directory = null;
            }

            if (config.AppSettings.Settings["Characters"] != null)
            {
                foreach (Character character in from character in this.characterCollection where character.HasTypes select character)
                {
                    List<string> typeList = character.Types.ToList();

                    typeList.Sort(delegate(string s1, string s2)
                    {
                        return String.Compare(s1, s2, StringComparison.InvariantCulture);
                    });
                    character.Types.Clear();
                    typeList.ForEach(delegate(string type)
                    {
                        character.Types.Add(type);
                    });
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    Character[] characters = this.characterCollection.ToArray();
                    XmlSerializer serializer = new XmlSerializer(characters.GetType());

                    serializer.Serialize(ms, characters);
                    ms.Seek(0, SeekOrigin.Begin);

                    using (FileStream fs = new FileStream(directory == null ? config.AppSettings.Settings["Characters"].Value : Path.Combine(directory, config.AppSettings.Settings["Characters"].Value), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        byte[] buffer = ms.ToArray();

                        fs.Write(buffer, 0, buffer.Length);
                        fs.Flush();
                    }
                }
            }

            if (config.AppSettings.Settings["Words"] != null)
            {
                List<Word> wordList = this.wordCollection.ToList();

                wordList.ForEach(delegate(Word word)
                {
                    if (word.HasAttributes)
                    {
                        List<string> attributeList = word.Attributes.ToList();

                        attributeList.Sort(delegate(string s1, string s2)
                        {
                            return String.Compare(s1, s2, StringComparison.InvariantCulture);
                        });
                        word.Attributes.Clear();
                        attributeList.ForEach(delegate(string attribute)
                        {
                            word.Attributes.Add(attribute);
                        });
                    }
                });
                wordList.Sort(delegate(Word w1, Word w2)
                {
                    return String.Compare(w1.Name, w2.Name, StringComparison.InvariantCulture);
                });

                using (MemoryStream ms = new MemoryStream())
                {
                    Word[] words = wordList.ToArray();
                    XmlSerializer serializer = new XmlSerializer(words.GetType());

                    serializer.Serialize(ms, words);
                    ms.Seek(0, SeekOrigin.Begin);

                    using (FileStream fs = new FileStream(directory == null ? config.AppSettings.Settings["Words"].Value : Path.Combine(directory, config.AppSettings.Settings["Words"].Value), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        byte[] buffer = ms.ToArray();

                        fs.Write(buffer, 0, buffer.Length);
                        fs.Flush();
                    }
                }
            }
        }

        public void Parse(string path)
        {
            if (this.sequenceCollection.Count > 0)
            {
                List<Sequence> tempSequenceList = this.sequenceCollection.ToList();

                this.sequenceCollection.Clear();
                tempSequenceList.ForEach(delegate(Sequence sequence)
                {
                    if (this.characterCollection.Any(delegate(Character character)
                    {
                        return character.Name.Equals(sequence.Owner);
                    }))
                    {
                        this.sequenceCollection.Add(sequence);
                    }
                });
            }

            if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                FileStream fs = null;

                try
                {
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                    using (ZipArchive zipArchive = new ZipArchive(fs))
                    {
                        fs = null;

                        HashSet<string> characterNameHashSet = new HashSet<string>();
                        LinkedList<Character> characterLinkedList = new LinkedList<Character>(this.characterCollection);
                        List<Sprite> cachedSpriteList = new List<Sprite>();
                        List<Sound> cachedSoundList = new List<Sound>();
                        Dictionary<string, HashSet<string>> motionTypeDictionary = new Dictionary<string, HashSet<string>>();

                        foreach (ZipArchiveEntry zipArchiveEntry in from zipArchiveEntry in zipArchive.Entries where zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) select zipArchiveEntry)
                        {
                            using (Stream stream = zipArchiveEntry.Open())
                            {
                                XmlDocument xmlDocument = new XmlDocument();

                                xmlDocument.Load(stream);
                                xmlDocument.Normalize();

                                if (xmlDocument.DocumentElement.Name.Equals("script"))
                                {
                                    foreach (XmlNode xmlNode in xmlDocument.DocumentElement.ChildNodes)
                                    {
                                        if (xmlNode.Name.Equals("character"))
                                        {
                                            Character character = ParseCharacter(xmlNode);
                                            List<Sequence> sequenceList = new List<Sequence>();

                                            if (!characterNameHashSet.Contains(character.Name))
                                            {
                                                List<Sequence> tempSequenceList = this.sequenceCollection.ToList();

                                                tempSequenceList.ForEach(delegate(Sequence sequence)
                                                {
                                                    if (sequence.Owner.Equals(character.Name))
                                                    {
                                                        this.sequenceCollection.Remove(sequence);
                                                    }
                                                });

                                                characterNameHashSet.Add(character.Name);
                                            }

                                            foreach (XmlNode sequenceNode in xmlNode.ChildNodes)
                                            {
                                                if (sequenceNode.Name.Equals("sequence"))
                                                {
                                                    Sequence sequence = ParseSequence(sequenceNode, character, cachedSpriteList, cachedSoundList);

                                                    sequenceList.Add(sequence);
                                                    this.sequenceCollection.Add(sequence);
                                                }
                                            }

                                            for (LinkedListNode<Character> nextLinkedListNode = characterLinkedList.First; nextLinkedListNode != null; nextLinkedListNode = nextLinkedListNode.Next)
                                            {
                                                if (nextLinkedListNode.Value.Name.Equals(character.Name))
                                                {
                                                    nextLinkedListNode.Value.BaseLocation = character.BaseLocation;
                                                    nextLinkedListNode.Value.Size = character.Size;
                                                    nextLinkedListNode.Value.Origin = character.Origin;

                                                    if (nextLinkedListNode.Value.HasTypes)
                                                    {
                                                        Queue<Sequence> sequenceQueue = new Queue<Sequence>(this.sequenceCollection);
                                                        HashSet<string> motionTypeHashSet;

                                                        if (!motionTypeDictionary.TryGetValue(nextLinkedListNode.Value.Name, out motionTypeHashSet))
                                                        {
                                                            motionTypeHashSet = new HashSet<string>();
                                                            motionTypeDictionary.Add(nextLinkedListNode.Value.Name, motionTypeHashSet);
                                                        }

                                                        while (sequenceQueue.Count > 0)
                                                        {
                                                            Sequence sequence = sequenceQueue.Dequeue();

                                                            foreach (object o in sequence)
                                                            {
                                                                Sequence s = o as Sequence;

                                                                if (s == null)
                                                                {
                                                                    if (nextLinkedListNode.Value.Name.Equals(sequence.Owner))
                                                                    {
                                                                        Collection<Motion> collection = o as Collection<Motion>;

                                                                        if (collection != null)
                                                                        {
                                                                            foreach (Motion motion in from motion in collection where motion.Type != null select motion)
                                                                            {
                                                                                if (!motionTypeHashSet.Contains(motion.Type))
                                                                                {
                                                                                    motionTypeHashSet.Add(motion.Type);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                else if (s.Any())
                                                                {
                                                                    sequenceQueue.Enqueue(s);
                                                                }
                                                            }
                                                        }

                                                        string[] types = nextLinkedListNode.Value.Types.ToArray();

                                                        nextLinkedListNode.Value.Types.Clear();

                                                        foreach (string type in types)
                                                        {
                                                            if (motionTypeHashSet.Contains(type))
                                                            {
                                                                nextLinkedListNode.Value.Types.Add(type);
                                                            }
                                                        }
                                                    }

                                                    characterLinkedList.Remove(nextLinkedListNode);

                                                    break;
                                                }
                                            }
                                        }
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
            else
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    XmlDocument xmlDocument = new XmlDocument();

                    xmlDocument.Load(fs);
                    xmlDocument.Normalize();

                    if (xmlDocument.DocumentElement.Name.Equals("script"))
                    {
                        HashSet<string> characterNameHashSet = new HashSet<string>();
                        LinkedList<Character> characterLinkedList = new LinkedList<Character>(this.characterCollection);
                        List<Sprite> cachedSpriteList = new List<Sprite>();
                        List<Sound> cachedSoundList = new List<Sound>();
                        Dictionary<string, HashSet<string>> motionTypeDictionary = new Dictionary<string, HashSet<string>>();

                        foreach (XmlNode xmlNode in xmlDocument.DocumentElement.ChildNodes)
                        {
                            if (xmlNode.Name.Equals("character"))
                            {
                                Character character = ParseCharacter(xmlNode);
                                List<Sequence> sequenceList = new List<Sequence>();

                                if (!characterNameHashSet.Contains(character.Name))
                                {
                                    List<Sequence> tempSequenceList = this.sequenceCollection.ToList();

                                    tempSequenceList.ForEach(delegate(Sequence sequence)
                                    {
                                        if (sequence.Owner.Equals(character.Name))
                                        {
                                            this.sequenceCollection.Remove(sequence);
                                        }
                                    });

                                    characterNameHashSet.Add(character.Name);
                                }

                                foreach (XmlNode sequenceNode in xmlNode.ChildNodes)
                                {
                                    if (sequenceNode.Name.Equals("sequence"))
                                    {
                                        Sequence sequence = ParseSequence(sequenceNode, character, cachedSpriteList, cachedSoundList);

                                        sequenceList.Add(sequence);
                                        this.sequenceCollection.Add(sequence);
                                    }
                                }

                                for (LinkedListNode<Character> nextLinkedListNode = characterLinkedList.First; nextLinkedListNode != null; nextLinkedListNode = nextLinkedListNode.Next)
                                {
                                    if (nextLinkedListNode.Value.Name.Equals(character.Name))
                                    {
                                        nextLinkedListNode.Value.BaseLocation = character.BaseLocation;
                                        nextLinkedListNode.Value.Size = character.Size;
                                        nextLinkedListNode.Value.Origin = character.Origin;

                                        if (nextLinkedListNode.Value.HasTypes)
                                        {
                                            Queue<Sequence> sequenceQueue = new Queue<Sequence>(sequenceList);
                                            HashSet<string> motionTypeHashSet;

                                            if (!motionTypeDictionary.TryGetValue(nextLinkedListNode.Value.Name, out motionTypeHashSet))
                                            {
                                                motionTypeHashSet = new HashSet<string>();
                                                motionTypeDictionary.Add(nextLinkedListNode.Value.Name, motionTypeHashSet);
                                            }

                                            while (sequenceQueue.Count > 0)
                                            {
                                                Sequence sequence = sequenceQueue.Dequeue();

                                                foreach (object o in sequence)
                                                {
                                                    Sequence s = o as Sequence;

                                                    if (s == null)
                                                    {
                                                        if (nextLinkedListNode.Value.Name.Equals(sequence.Owner))
                                                        {
                                                            Collection<Motion> collection = o as Collection<Motion>;

                                                            if (collection != null)
                                                            {
                                                                foreach (Motion motion in from motion in collection where motion.Type != null select motion)
                                                                {
                                                                    if (!motionTypeHashSet.Contains(motion.Type))
                                                                    {
                                                                        motionTypeHashSet.Add(motion.Type);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (s.Any())
                                                    {
                                                        sequenceQueue.Enqueue(s);
                                                    }
                                                }
                                            }

                                            string[] types = nextLinkedListNode.Value.Types.ToArray();

                                            nextLinkedListNode.Value.Types.Clear();

                                            foreach (string type in types)
                                            {
                                                if (motionTypeHashSet.Contains(type))
                                                {
                                                    nextLinkedListNode.Value.Types.Add(type);
                                                }
                                            }
                                        }

                                        characterLinkedList.Remove(nextLinkedListNode);

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Queue<Sequence> q = new Queue<Sequence>(this.sequenceCollection);
            HashSet<string> hs = new HashSet<string>();

            while (q.Count > 0)
            {
                Sequence sequence = q.Dequeue();

                foreach (object o in sequence)
                {
                    Sequence s = o as Sequence;

                    if (s != null)
                    {
                        q.Enqueue(s);
                    }
                }

                if (!hs.Contains(sequence.Name))
                {
                    hs.Add(sequence.Name);
                }
            }

            foreach (string s in (from s in this.sequenceStateDictionary.Keys where !hs.Contains(s) select s).ToArray())
            {
                this.sequenceStateDictionary.Remove(s);
            }
        }

        private Character ParseCharacter(XmlNode characterNode)
        {
            Character character = new Character();
            double originX = 0;
            double originY = 0;
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            
            foreach (XmlAttribute characterAttribute in characterNode.Attributes)
            {
                if (characterAttribute.Name.Equals("name"))
                {
                    character.Name = characterAttribute.Value;
                }
                else if (characterAttribute.Name.Equals("origin-x"))
                {
                    originX = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (characterAttribute.Name.Equals("origin-y"))
                {
                    originY = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (characterAttribute.Name.Equals("x") || characterAttribute.Name.Equals("left"))
                {
                    x = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (characterAttribute.Name.Equals("y") || characterAttribute.Name.Equals("top"))
                {
                    y = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (characterAttribute.Name.Equals("width"))
                {
                    width = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (characterAttribute.Name.Equals("height"))
                {
                    height = Double.Parse(characterAttribute.Value, CultureInfo.InvariantCulture);
                }
            }

            character.Origin = new System.Windows.Point(originX, originY);
            character.BaseLocation = new System.Windows.Point(x, y);
            character.Size = new System.Windows.Size(width, height);

            return character;
        }

        private Sequence ParseSequence(XmlNode sequenceNode, Character character, List<Sprite> cachedSpriteList, List<Sound> cachedSoundList)
        {
            Sequence sequence = new Sequence();
            System.Collections.Queue queue = new System.Collections.Queue();
            System.Collections.ArrayList arrayList = new System.Collections.ArrayList();
            
            foreach (XmlAttribute sequenceAttribute in sequenceNode.Attributes)
            {
                if (sequenceAttribute.Name.Equals("name"))
                {
                    sequence.Name = sequenceAttribute.Value;
                }
                else if (sequenceAttribute.Name.Equals("state"))
                {
                    sequence.State = sequenceAttribute.Value;
                }
            }

            foreach (XmlNode xmlNode in sequenceNode.ChildNodes)
            {
                if (xmlNode.Name.Equals("sequence"))
                {
                    queue.Enqueue(ParseSequence(xmlNode, character, cachedSpriteList, cachedSoundList));
                }
                else if (xmlNode.Name.Equals("message"))
                {
                    queue.Enqueue(ParseMessage(xmlNode));
                }
                else if (xmlNode.Name.Equals("motion"))
                {
                    queue.Enqueue(ParseMotion(xmlNode, cachedSpriteList));
                }
                else if (xmlNode.Name.Equals("sound"))
                {
                    queue.Enqueue(ParseSound(xmlNode, cachedSoundList));
                }
            }

            while (queue.Count > 0)
            {
                object o = queue.Dequeue();
                Motion motion = o as Motion;

                if (motion == null)
                {
                    arrayList.Add(o);
                }
                else
                {
                    List<Motion> motionList = new List<Motion>();
                    Dictionary<int, LinkedList<Motion>> motionDictionary = new Dictionary<int, LinkedList<Motion>>();
                    LinkedList<Motion> motionLinkedList = new LinkedList<Motion>();

                    motionDictionary.Add(motion.ZIndex, motionLinkedList);
                    motionLinkedList.AddFirst(motion);

                    while (queue.Count > 0)
                    {
                        if (!(queue.Peek() is Motion))
                        {
                            break;
                        }

                        Motion m = (Motion)queue.Dequeue();
                        LinkedList<Motion> linkedList;

                        if (!motionDictionary.TryGetValue(m.ZIndex, out linkedList))
                        {
                            linkedList = new LinkedList<Motion>();
                            motionDictionary.Add(m.ZIndex, linkedList);
                        }

                        linkedList.AddLast(m);
                    }
                    
                    foreach (KeyValuePair<int, LinkedList<Motion>> kvp in motionDictionary)
                    {
                        do
                        {
                            Queue<Motion> motionQueue = new Queue<Motion>();
                            string type = kvp.Value.First.Value.Type;
                            LinkedListNode<Motion> nextLinkedListNode = kvp.Value.First.Next;
                                
                            motionQueue.Enqueue(kvp.Value.First.Value);
                            kvp.Value.RemoveFirst();

                            while (nextLinkedListNode != null)
                            {
                                LinkedListNode<Motion> linkedListNode = nextLinkedListNode;

                                nextLinkedListNode = linkedListNode.Next;

                                if (String.Equals(type, linkedListNode.Value.Type))
                                {
                                    motionQueue.Enqueue(linkedListNode.Value);
                                    kvp.Value.Remove(linkedListNode);
                                }
                            }

                            do
                            {
                                Motion dequeuedMotion = motionQueue.Dequeue();
                                List<Motion> tempMotionList = new List<Motion>();
                                double maxFrameRate = dequeuedMotion.FrameRate;

                                while (motionQueue.Count > 0)
                                {
                                    if (dequeuedMotion.Loop == motionQueue.Peek().Loop)
                                    {
                                        tempMotionList.Add(motionQueue.Dequeue());
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                tempMotionList.ForEach(delegate(Motion m)
                                {
                                    if (m.FrameRate > maxFrameRate)
                                    {
                                        maxFrameRate = m.FrameRate;
                                    }
                                });

                                if (dequeuedMotion.HasFrames)
                                {
                                    int iterations = (int)Math.Round(maxFrameRate / dequeuedMotion.FrameRate);
                                    Sprite[] sprites = dequeuedMotion.Sprites.ToArray();

                                    dequeuedMotion.Sprites.Clear();

                                    foreach (Sprite sprite in sprites)
                                    {
                                        for (int i = 0; i < iterations; i++)
                                        {
                                            dequeuedMotion.Sprites.Add(sprite);
                                        }
                                    }
                                }

                                tempMotionList.ForEach(delegate(Motion m)
                                {
                                    if (m.HasFrames)
                                    {
                                        int iterations = (int)Math.Round(maxFrameRate / m.FrameRate);

                                        foreach (Sprite sprite in m.Sprites)
                                        {
                                            for (int i = 0; i < iterations; i++)
                                            {
                                                dequeuedMotion.Sprites.Add(sprite);
                                            }
                                        }
                                    }
                                });

                                dequeuedMotion.FrameRate = maxFrameRate;
                                motionList.Add(dequeuedMotion);
                            } while (motionQueue.Count > 0);
                        } while (kvp.Value.Count > 0);
                    }

                    arrayList.Add(new Collection<Motion>(motionList));
                }
            }

            sequence.Owner = character.Name;

            foreach (object o in arrayList)
            {
                sequence.Add(o);
            }

            return sequence;
        }

        private Message ParseMessage(XmlNode messageNode)
        {
            Message message = new Message();

            if (messageNode.HasChildNodes)
            {
                StringBuilder stringBuilder = new StringBuilder();
            
                foreach (XmlNode xmlNode in messageNode.ChildNodes)
                {
                    if (xmlNode.NodeType == XmlNodeType.Text || xmlNode.NodeType == XmlNodeType.CDATA)
                    {
                        stringBuilder.Append(Regex.Replace(xmlNode.InnerText, String.Concat("\\s*", Environment.NewLine, "\\s*"), String.Empty, RegexOptions.CultureInvariant));
                    }
                    else if (xmlNode.Name.Equals("break") || xmlNode.Name.Equals("br"))
                    {
                        stringBuilder.Append(Environment.NewLine);
                    }
                }

                message.Add(stringBuilder.ToString());
            }

            foreach (XmlAttribute attribute in messageNode.Attributes)
            {
                if (attribute.Name.Equals("speed"))
                {
                    message.Speed = Double.Parse(attribute.Value, CultureInfo.InvariantCulture);
                }
                else if (attribute.Name.Equals("duration"))
                {
                    message.Duration = TimeSpan.Parse(attribute.Value, CultureInfo.InvariantCulture);
                }
            }

            return message;
        }

        private Motion ParseMotion(XmlNode motionNode, List<Sprite> cachedSpriteList)
        {
            Motion motion = new Motion();
            int iterations = 1;
            List<Sprite> spriteList = new List<Sprite>();

            foreach (XmlAttribute motionAttribute in motionNode.Attributes)
            {
                if (motionAttribute.Name.Equals("loop"))
                {
                    motion.Loop = Boolean.Parse(motionAttribute.Value);
                }
                else if (motionAttribute.Name.Equals("fps"))
                {
                    motion.FrameRate = Double.Parse(motionAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (motionAttribute.Name.Equals("z-index"))
                {
                    motion.ZIndex = Int32.Parse(motionAttribute.Value, CultureInfo.InvariantCulture);
                }
                else if (motionAttribute.Name.Equals("type"))
                {
                    motion.Type = motionAttribute.Value;
                }
                else if (motionAttribute.Name.Equals("iterations"))
                {
                    iterations = Int32.Parse(motionAttribute.Value, CultureInfo.InvariantCulture);
                }
            }

            foreach (XmlNode xmlNode in motionNode.ChildNodes)
            {
                if (xmlNode.Name.Equals("image"))
                {
                    Nullable<double> x = null;
                    Nullable<double> y = null;
                    Nullable<double> width = null;
                    Nullable<double> height = null;
                    Nullable<double> opacity = null;

                    foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                    {
                        if (xmlAttribute.Name.Equals("x"))
                        {
                            x = new Nullable<double>(Double.Parse(xmlAttribute.Value, CultureInfo.InvariantCulture));
                        }
                        else if (xmlAttribute.Name.Equals("y"))
                        {
                            y = new Nullable<double>(Double.Parse(xmlAttribute.Value, CultureInfo.InvariantCulture));
                        }
                        else if (xmlAttribute.Name.Equals("width"))
                        {
                            width = new Nullable<double>(Double.Parse(xmlAttribute.Value, CultureInfo.InvariantCulture));
                        }
                        else if (xmlAttribute.Name.Equals("height"))
                        {
                            height = new Nullable<double>(Double.Parse(xmlAttribute.Value, CultureInfo.InvariantCulture));
                        }
                        else if (xmlAttribute.Name.Equals("opacity"))
                        {
                            opacity = new Nullable<double>(Double.Parse(xmlAttribute.Value, CultureInfo.InvariantCulture));
                        }
                    }

                    Sprite sprite = cachedSpriteList.Find(delegate(Sprite s)
                    {
                        if (s.Location.X == (x.HasValue ? x.Value : 0) && s.Location.Y == (y.HasValue ? y.Value : 0) && s.Opacity == (opacity.HasValue ? opacity.Value : 1) && s.Path.Equals(xmlNode.InnerText))
                        {
                            if (s.Size.IsEmpty)
                            {
                                if (!width.HasValue && !height.HasValue)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (width.HasValue && height.HasValue)
                                {
                                    if (s.Size.Width == width.Value && s.Size.Height == height.Value)
                                    {
                                        return true;
                                    }
                                }
                                else if (width.HasValue && s.Size.Width == width.Value && Double.IsNaN(s.Size.Height))
                                {
                                    return true;
                                }
                                else if (height.HasValue && Double.IsNaN(s.Size.Width) && s.Size.Height == height.Value)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    });

                    if (sprite == null)
                    {
                        sprite = new Sprite(xmlNode.InnerText);

                        if (x.HasValue || y.HasValue)
                        {
                            sprite.Location = new System.Windows.Point(x.HasValue ? x.Value : 0, y.HasValue ? y.Value : 0);
                        }

                        if (width.HasValue && height.HasValue)
                        {
                            sprite.Size = new System.Windows.Size(width.Value, height.Value);
                        }
                        else if (width.HasValue)
                        {
                            sprite.Size = new System.Windows.Size(width.Value, Double.NaN);
                        }
                        else if (height.HasValue)
                        {
                            sprite.Size = new System.Windows.Size(Double.NaN, height.Value);
                        }

                        if (opacity.HasValue)
                        {
                            sprite.Opacity = opacity.Value;
                        }

                        cachedSpriteList.Add(sprite);
                    }
                    
                    spriteList.Add(sprite);
                }
            }

            for (int i = 0; i < iterations; i++)
            {
                spriteList.ForEach(delegate(Sprite sprite)
                {
                    motion.Sprites.Add(sprite);
                });
            }

            return motion;
        }

        private Sound ParseSound(XmlNode soundNode, List<Sound> cachedSoundList)
        {
            Sound sound = cachedSoundList.Find(delegate(Sound s)
            {
                return s.Path.Equals(soundNode.InnerText);
            });

            if (sound == null)
            {
                sound = new Sound(soundNode.InnerText);

                cachedSoundList.Add(sound);
            }

            return sound;
        }

        public void Update()
        {
            Update(false);
        }

        public void Update(bool reset)
        {
            DateTime nowDateTime = DateTime.Now;
            DateTime updatedDateTime = reset ? nowDateTime - new TimeSpan(12, 0, 0) : this.lastUpdatedDateTime;
            string[] terms = (from word in this.wordCollection select word.Name).Distinct().ToArray();
            Dictionary<Uri, Entry> entryDictionary = new Dictionary<Uri, Entry>();
            Dictionary<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> cacheDictionary = new Dictionary<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>>();
            
            this.lastUpdatedDateTime = nowDateTime;

            Task.Factory.StartNew(delegate
            {
                DateTime pastDateTime = nowDateTime - new TimeSpan(24, 0, 0);

                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    System.Configuration.Configuration config = null;
                    string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                    Fetcher fetcher = new Fetcher();

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
                        directory = null;
                    }

                    if (config.AppSettings.Settings["Timeout"] != null)
                    {
                        if (config.AppSettings.Settings["Timeout"].Value.Length > 0)
                        {
                            fetcher.Timeout = new Nullable<int>(Int32.Parse(config.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture));
                        }
                    }

                    if (config.AppSettings.Settings["UserAgent"] != null)
                    {
                        fetcher.UserAgent = config.AppSettings.Settings["UserAgent"].Value;
                    }

                    if (config.AppSettings.Settings["Subscriptions"] != null)
                    {
                        using (FileStream fs = new FileStream(directory == null ? config.AppSettings.Settings["Subscriptions"].Value : Path.Combine(directory, config.AppSettings.Settings["Subscriptions"].Value), FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            XmlDocument xmlDocument = new XmlDocument();

                            xmlDocument.Load(fs);
                            xmlDocument.Normalize();

                            foreach (XmlNode xmlOutlineNode in xmlDocument.DocumentElement.SelectNodes("/opml/body//outline"))
                            {
                                foreach (XmlAttribute xmlChildAttribute in xmlOutlineNode.Attributes)
                                {
                                    if (xmlChildAttribute.Name.Equals("xmlUrl"))
                                    {
                                        fetcher.Subscriptions.Add(new Uri(xmlChildAttribute.Value, UriKind.Absolute));

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    fetcher.Collect();

                    foreach (Entry entry in from entry in fetcher.Entries where entry.Resource != null && entry.Modified > pastDateTime && entry.Modified <= nowDateTime select entry)
                    {
                        if (entryDictionary.ContainsKey(entry.Resource))
                        {
                            if (entry.Modified > entryDictionary[entry.Resource].Modified)
                            {
                                entryDictionary[entry.Resource] = entry;
                            }
                        }
                        else
                        {
                            entryDictionary.Add(entry.Resource, entry);
                        }
                    }
                }

                foreach (System.Configuration.ConnectionStringSettings settings in System.Configuration.ConfigurationManager.ConnectionStrings)
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(settings.ProviderName);

                    using (IDbConnection connection = factory.CreateConnection())
                    {
                        connection.ConnectionString = settings.ConnectionString;
                        connection.Open();

                        using (IDbCommand command = factory.CreateCommand())
                        {
                            IDataReader reader = null;

                            command.Connection = connection;
                            command.CommandText = BuildSelectStatement(pastDateTime, nowDateTime);

                            try
                            {
                                reader = command.ExecuteReader();

                                while (reader.Read())
                                {
                                    Entry entry = new Entry();

                                    if (!Convert.IsDBNull(reader["Resource"]))
                                    {
                                        Uri uri;

                                        if (Uri.TryCreate((string)reader["Resource"], UriKind.RelativeOrAbsolute, out uri))
                                        {
                                            entry.Resource = uri;
                                        }
                                    }

                                    if (!Convert.IsDBNull(reader["Title"]))
                                    {
                                        entry.Title = reader["Title"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Description"]))
                                    {
                                        entry.Description = reader["Description"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Author"]))
                                    {
                                        entry.Author = reader["Author"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Created"]))
                                    {
                                        entry.Created = (DateTime)reader["Created"];
                                    }

                                    if (!Convert.IsDBNull(reader["Modified"]))
                                    {
                                        entry.Modified = (DateTime)reader["Modified"];
                                    }

                                    if (entryDictionary.ContainsKey(entry.Resource))
                                    {
                                        if (entry.Modified > entryDictionary[entry.Resource].Modified)
                                        {
                                            entryDictionary[entry.Resource] = entry;
                                        }
                                    }
                                    else
                                    {
                                        entryDictionary.Add(entry.Resource, entry);
                                    }
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                {
                                    reader.Close();
                                }
                            }
                        }
                    }
                }

                if (entryDictionary.Count == 0)
                {
                    foreach (System.Configuration.ConnectionStringSettings settings in System.Configuration.ConfigurationManager.ConnectionStrings)
                    {
                        DbProviderFactory factory = DbProviderFactories.GetFactory(settings.ProviderName);

                        using (IDbConnection connection = factory.CreateConnection())
                        {
                            connection.ConnectionString = settings.ConnectionString;
                            connection.Open();

                            using (IDbCommand command = factory.CreateCommand())
                            {
                                IDataReader reader = null;

                                command.Connection = connection;
                                command.CommandText = BuildSelectStatement(100);

                                try
                                {
                                    reader = command.ExecuteReader();

                                    while (reader.Read())
                                    {
                                        Entry entry = new Entry();

                                        if (!Convert.IsDBNull(reader["Resource"]))
                                        {
                                            Uri uri;

                                            if (Uri.TryCreate((string)reader["Resource"], UriKind.RelativeOrAbsolute, out uri))
                                            {
                                                entry.Resource = uri;
                                            }
                                        }

                                        if (!Convert.IsDBNull(reader["Title"]))
                                        {
                                            entry.Title = reader["Title"] as string;
                                        }

                                        if (!Convert.IsDBNull(reader["Description"]))
                                        {
                                            entry.Description = reader["Description"] as string;
                                        }

                                        if (!Convert.IsDBNull(reader["Author"]))
                                        {
                                            entry.Author = reader["Author"] as string;
                                        }

                                        if (!Convert.IsDBNull(reader["Created"]))
                                        {
                                            entry.Created = (DateTime)reader["Created"];
                                        }

                                        if (!Convert.IsDBNull(reader["Modified"]))
                                        {
                                            entry.Modified = (DateTime)reader["Modified"];
                                        }

                                        if (entryDictionary.ContainsKey(entry.Resource))
                                        {
                                            if (entry.Modified > entryDictionary[entry.Resource].Modified)
                                            {
                                                entryDictionary[entry.Resource] = entry;
                                            }
                                        }
                                        else
                                        {
                                            entryDictionary.Add(entry.Resource, entry);
                                        }
                                    }
                                }
                                finally
                                {
                                    if (reader != null)
                                    {
                                        reader.Close();
                                    }
                                }
                            }
                        }
                    }
                }

                Dictionary<char, List<string>> termDictionary = new Dictionary<char, List<string>>();
                
                foreach (string term in terms)
                {
                    if (term.Length > 0)
                    {
                        List<string> termList;

                        if (!termDictionary.TryGetValue(term[0], out termList))
                        {
                            termList = new List<string>();
                            termDictionary.Add(term[0], termList);
                        }

                        termList.Add(term);
                    }
                }

                List<Entry> cachedEntryList = (from entry in entryDictionary.Values orderby entry.Modified descending select entry).ToList();
                List<Entry> recentEntryList = cachedEntryList.FindAll(delegate(Entry entry)
                {
                    if (entry.Modified > nowDateTime - new TimeSpan(12, 0, 0) && entry.Modified <= nowDateTime)
                    {
                        return true;
                    }

                    return false;
                });
                Dictionary<string, double> idfDictionary = GetInverseDocumentFrequency(termDictionary, cachedEntryList);
                Dictionary<Uri, double[]> vectorDictionary = new Dictionary<Uri, double[]>();
                HashSet<int> indexHashSet = new HashSet<int>();

                if (recentEntryList.Count < 25)
                {
                    DateTime dt = nowDateTime;

                    if (recentEntryList.Count > 0)
                    {
                        dt -= new TimeSpan(12, 0, 0);
                    }

                    recentEntryList.AddRange(cachedEntryList.FindAll(delegate(Entry entry)
                    {
                        if (entry.Modified <= dt)
                        {
                            return true;
                        }

                        return false;
                    }));

                    if (recentEntryList.Count > 25)
                    {
                        recentEntryList.RemoveRange(25, recentEntryList.Count - 25);
                    }
                }

                recentEntryList.ForEach(delegate(Entry entry)
                {
                    Dictionary<string, double> d = GetTermFrequency(termDictionary, entry);
                    KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair;
                    double[] vector = new double[terms.Length];
                    bool isZeroVector = true;
                    
                    foreach (KeyValuePair<string, double> kvp in d)
                    {
                        entry.Tags.Add(kvp.Key);
                    }

                    for (int i = 0; i < terms.Length; i++)
                    {
                        double tf;
                        double idf;

                        if (d.TryGetValue(terms[i], out tf) && idfDictionary.TryGetValue(terms[i], out idf))
                        {
                            if (cacheDictionary.TryGetValue(terms[i], out keyValuePair))
                            {
                                keyValuePair.Key.Add(new KeyValuePair<Entry, double>(entry, tf));
                            }
                            else
                            {
                                List<KeyValuePair<Entry, double>> keyValuePairList = new List<KeyValuePair<Entry, double>>();

                                keyValuePairList.Add(new KeyValuePair<Entry, double>(entry, tf));
                                cacheDictionary.Add(terms[i], new KeyValuePair<List<KeyValuePair<Entry, double>>, double>(keyValuePairList, idf));
                            }

                            vector[i] = tf * idf;
                            isZeroVector = false;

                            if (!indexHashSet.Contains(i))
                            {
                                indexHashSet.Add(i);
                            }
                        }
                        else
                        {
                            vector[i] = 0;
                        }
                    }

                    if (!isZeroVector)
                    {
                        vectorDictionary.Add(entry.Resource, vector);
                    }
                });

                foreach (Uri uri in vectorDictionary.Keys.ToArray())
                {
                    List<double> vectorList = new List<double>();

                    for (int i = 0; i < vectorDictionary[uri].Length; i++)
                    {
                        if (indexHashSet.Contains(i))
                        {
                            vectorList.Add(vectorDictionary[uri][i]);
                        }
                    }

                    vectorDictionary[uri] = vectorList.ToArray();
                }

                Filter filter = new Filter(Environment.TickCount);

                if (vectorDictionary.Count > 0)
                {
                    foreach (KeyValuePair<Uri, double[]> kvp in vectorDictionary)
                    {
                        filter.Add(kvp.Key.ToString(), kvp.Value);
                    }

                    filter.Reset();
                    filter.Train(filter.MaxIterations);
                }

                if (filter.IsTrained)
                {
                    filter.Build();
                    recentEntryList.ForEach(delegate(Entry entry)
                    {
                        List<Entry> similarEntryList = new List<Entry>();

                        foreach (string similarLabel in filter.Query(entry.Resource.ToString(), filter.Width * 25 / 100, filter.Height * 25 / 100))
                        {
                            Entry newEntry = (Entry)entryDictionary[new Uri(similarLabel, UriKind.RelativeOrAbsolute)].Clone();

                            newEntry.Score = new Nullable<double>(filter.GetDistance(entry.Resource.ToString(), similarLabel));
                            similarEntryList.Add(newEntry);
                        }

                        similarEntryList.Sort(delegate(Entry e1, Entry e2)
                        {
                            if (e1.Score > e2.Score)
                            {
                                return 1;
                            }
                            else if (e1.Score < e2.Score)
                            {
                                return -1;
                            }

                            return 0;
                        });
                        similarEntryList.ForEach(delegate(Entry e)
                        {
                            if (!e.Resource.Equals(entry.Resource))
                            {
                                entry.SimilarEntries.Add(e);
                            }
                        });
                    });
                }
            }, TaskCreationOptions.LongRunning).ContinueWith(delegate
            {
                List<Entry> newEntryList = (from entry in entryDictionary.Values where entry.Modified > updatedDateTime && entry.Modified <= nowDateTime orderby entry.Modified descending select entry).ToList();
                bool tfidfIsUpdated = true;
                
                if (reset && newEntryList.Count == 0)
                {
                    newEntryList.AddRange((from entry in entryDictionary.Values where entry.Modified <= nowDateTime orderby entry.Modified descending select entry).Take(25));
                }

                if (this.cacheDictionary.Count == cacheDictionary.Count)
                {
                    tfidfIsUpdated = !cacheDictionary.All(delegate(KeyValuePair<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> keyValuePair1)
                    {
                        KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair2;

                        if (this.cacheDictionary.TryGetValue(keyValuePair1.Key, out keyValuePair2))
                        {
                            double sum1 = 0;
                            double sum2 = 0;

                            keyValuePair1.Value.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair3)
                            {
                                sum1 += keyValuePair3.Value;
                            });

                            keyValuePair2.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair3)
                            {
                                sum2 += keyValuePair3.Value;
                            });

                            if (sum1 / (from kvp1 in cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value.Value == sum2 / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair2.Value)
                            {
                                return true;
                            }
                        }

                        return false;
                    });
                }

                if (tfidfIsUpdated && this.cacheDictionary.Count == 0 && cacheDictionary.Count == 0)
                {
                    tfidfIsUpdated = false;
                }

                this.cacheDictionary.Clear();

                foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> kvp in cacheDictionary)
                {
                    this.cacheDictionary.Add(kvp.Key, kvp.Value);
                }

                if (newEntryList.Count > 0)
                {
                    Alert(newEntryList);
                }

                if (cacheDictionary.Count > 0 && (reset || tfidfIsUpdated))
                {
                    Trend(cacheDictionary.Keys);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void Suspend()
        {
            if (this.updateTimer != null)
            {
                this.updateTimer.Stop();
            }

            if (this.pollingTimer != null)
            {
                this.pollingTimer.Stop();
            }
        }

        public void Resume()
        {
            this.idleTimeSpan = TimeSpan.Zero;
            this.lastPolledDateTime = this.lastUpdatedDateTime = DateTime.Now;

            if (this.pollingTimer != null)
            {
                this.pollingTimer.Start();
            }

            if (this.updateTimer != null)
            {
                this.updateTimer.Start();
            }
        }

        public IEnumerable<Sequence> Prepare(IEnumerable<Sequence> sequences, string state)
        {
            return Prepare(sequences, state, delegate(IEnumerable<Sequence> collection)
            {
                Sequence[] tempSequences = collection.ToArray();

                return new Sequence[] { tempSequences[new Random(Environment.TickCount).Next(tempSequences.Length)] };
            });
        }

        private IEnumerable<Sequence> Prepare(IEnumerable<Sequence> sequences, string state, Func<IEnumerable<Sequence>, IEnumerable<Sequence>> func)
        {
            List<Sequence> executableSequenceList = new List<Sequence>();
            List<Sequence> preparedSequenceList = new List<Sequence>();
            List<string> ownerList = new List<string>();

            foreach (Sequence sequence in from sequence in sequences where sequence.State != null select sequence)
            {
                string newState = state;

                if (newState == null)
                {
                    string oldState;

                    if (this.sequenceStateDictionary.TryGetValue(sequence.Name, out oldState))
                    {
                        newState = oldState;
                    }
                }

                if (newState != null)
                {
                    if (Regex.IsMatch(newState, sequence.State, RegexOptions.CultureInvariant | RegexOptions.Singleline))
                    {
                        executableSequenceList.Add(sequence);
                    }
                }
            }

            if (executableSequenceList.Count == 0)
            {
                executableSequenceList.AddRange(from sequence in sequences where sequence.State == null select sequence);
                state = null;
            }

            executableSequenceList.ForEach(delegate(Sequence sequence)
            {
                if (!ownerList.Contains(sequence.Owner))
                {
                    ownerList.Add(sequence.Owner);
                }
            });

            ownerList.ForEach(delegate(string owner)
            {
                foreach (Sequence sequence in func(executableSequenceList.FindAll(delegate(Sequence sequence)
                {
                    return sequence.Owner.Equals(owner);
                })))
                {
                    Sequence flattenedSequence = new Sequence();

                    if (state != null)
                    {
                        if (this.sequenceStateDictionary.ContainsKey(sequence.Name))
                        {
                            this.sequenceStateDictionary[sequence.Name] = state;
                        }
                        else
                        {
                            this.sequenceStateDictionary.Add(sequence.Name, state);
                        }
                    }

                    flattenedSequence.Name = sequence.Name;
                    flattenedSequence.Owner = sequence.Owner;
                    flattenedSequence.State = sequence.State;

                    foreach (object obj in sequence)
                    {
                        Sequence nestedSequence = obj as Sequence;

                        if (nestedSequence == null)
                        {
                            flattenedSequence.Add(obj);
                        }
                        else if (!nestedSequence.Any())
                        {
                            List<Sequence> sequenceList = new List<Sequence>();

                            foreach (Sequence s in this.sequenceCollection)
                            {
                                Stack<Sequence> sequenceStack = GetSequenceStack(s, nestedSequence);

                                if (sequenceStack.Count > 0)
                                {
                                    Stack<Sequence> stack = new Stack<Sequence>();

                                    do
                                    {
                                        Sequence poppedSequence = sequenceStack.Pop();

                                        if (sequenceStack.Count > 0)
                                        {
                                            Stack<Sequence> tempStack = new Stack<Sequence>();

                                            foreach (object o in sequenceStack.Peek())
                                            {
                                                Sequence tempSequence = o as Sequence;

                                                if (tempSequence != null)
                                                {
                                                    if (tempSequence.Any() && !stack.Contains(tempSequence))
                                                    {
                                                        tempStack.Push(tempSequence);
                                                    }

                                                    if (tempSequence == poppedSequence)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }

                                            while (tempStack.Count > 0)
                                            {
                                                stack.Push(tempStack.Pop());
                                            }
                                        }

                                        if (poppedSequence.Any() && !stack.Contains(poppedSequence))
                                        {
                                            stack.Push(poppedSequence);
                                        }
                                    } while (sequenceStack.Count > 0);

                                    while (stack.Count > 0)
                                    {
                                        sequenceList.Add(stack.Pop());
                                    }
                                }
                                else
                                {
                                    sequenceList.Add(s);
                                }
                            }

                            foreach (Sequence s in Prepare(sequenceList.FindAll(delegate(Sequence s)
                            {
                                return s.Name.Equals(nestedSequence.Name);
                            }), nestedSequence.State, func))
                            {
                                if (s.Owner.Equals(sequence.Owner))
                                {
                                    foreach (object o in s)
                                    {
                                        flattenedSequence.Add(o);
                                    }
                                }
                                else
                                {
                                    if (flattenedSequence.Any())
                                    {
                                        preparedSequenceList.Add(flattenedSequence);

                                        flattenedSequence = new Sequence();
                                        flattenedSequence.Name = sequence.Name;
                                        flattenedSequence.Owner = sequence.Owner;
                                        flattenedSequence.State = sequence.State;
                                    }

                                    preparedSequenceList.Add(s);
                                }
                            }
                        }
                    }

                    if (flattenedSequence.Any())
                    {
                        preparedSequenceList.Add(flattenedSequence);
                    }
                }
            });

            return preparedSequenceList;
        }

        public IEnumerable<Sequence> Prepare(IEnumerable<Sequence> sequences, string state, IEnumerable<string> terms)
        {
            Dictionary<string, string> stateDictionary = new Dictionary<string, string>(this.sequenceStateDictionary);
            List<Sequence> preparedSequenceList = new List<Sequence>();
            Dictionary<int, LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>> cachedDictionary = new Dictionary<int, LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>>();
            Dictionary<string, List<string>> wordDictionary = terms.Aggregate<string, Dictionary<string, List<string>>>(new Dictionary<string, List<string>>(), delegate(Dictionary<string, List<string>> d, string s)
            {
                foreach (Word word in from word in this.wordCollection where word.Name.Equals(s) select word)
                {
                    List<string> nameList;

                    if (d.TryGetValue(word.Name, out nameList))
                    {
                        nameList.AddRange(word.Attributes);
                    }
                    else
                    {
                        d.Add(word.Name, new List<string>(word.Attributes));
                    }
                }

                return d;
            });
            
            do
            {
                int i = 0;
                LinkedList<string> linkedList = null;
                HashSet<string> hashSet = new HashSet<string>();
                bool isAvailable = true;

                preparedSequenceList.AddRange(Prepare(sequences, state, delegate(IEnumerable<Sequence> collection)
                {
                    List<Sequence> sequenceList = new List<Sequence>();
                    LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>> cachedLinkedList;

                    if (cachedDictionary.TryGetValue(i, out cachedLinkedList))
                    {
                        sequenceList.Add(cachedLinkedList.First().Key);
                        linkedList = cachedLinkedList.First().Value.Key;

                        foreach (string s in cachedLinkedList.First().Value.Value)
                        {
                            if (!hashSet.Contains(s))
                            {
                                hashSet.Add(s);
                            }
                        }
                    }
                    else
                    {
                        List<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>> tempList = collection.Aggregate<Sequence, List<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>>(new List<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>(), delegate(List<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>> list, Sequence s)
                        {
                            LinkedList<string> ll = new LinkedList<string>(linkedList == null ? terms : linkedList);
                            HashSet<string> hs = new HashSet<string>(hashSet);

                            foreach (object o in s)
                            {
                                Message message = o as Message;

                                if (message != null)
                                {
                                    for (Match match = Regex.Match(message.Text, @"(?<Open>\{{2})*\{(?<1>(?:[^{}]|(?<2>(?:(?:\{|}){2})+))+)}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                                    {
                                        if (!hs.Contains(match.Groups[1].Value))
                                        {
                                            LinkedListNode<string> linkedListNode = ll.First;

                                            if (linkedListNode != null)
                                            {
                                                do
                                                {
                                                    List<string> attributeList;
                                                    string pattern;

                                                    if (match.Groups[2].Success)
                                                    {
                                                        int j = match.Groups[1].Index;
                                                        StringBuilder sb = new StringBuilder();

                                                        foreach (Capture capture in match.Groups[2].Captures)
                                                        {
                                                            if (capture.Index > j)
                                                            {
                                                                sb.Append(message.Text.Substring(j, capture.Index - j));
                                                            }

                                                            sb.Append(capture.Value.Substring(capture.Length / 2));
                                                            j = capture.Index + capture.Length;
                                                        }

                                                        if (match.Groups[1].Index + match.Groups[1].Length > j)
                                                        {
                                                            sb.Append(message.Text.Substring(j, match.Groups[1].Index + match.Groups[1].Length - j));
                                                        }

                                                        pattern = sb.ToString();
                                                    }
                                                    else
                                                    {
                                                        pattern = match.Groups[1].Value;
                                                    }

                                                    if (wordDictionary.TryGetValue(linkedListNode.Value, out attributeList) && attributeList.Exists(delegate(string attribute)
                                                    {
                                                        return Regex.IsMatch(attribute, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
                                                    }))
                                                    {
                                                        hs.Add(match.Groups[1].Value);
                                                        ll.Remove(linkedListNode);

                                                        break;
                                                    }

                                                    linkedListNode = linkedListNode.Next;
                                                } while (linkedListNode != null);

                                                if (hs.Contains(match.Groups[1].Value))
                                                {
                                                    continue;
                                                }
                                            }

                                            return list;
                                        }
                                    }
                                }
                            }

                            list.Add(new KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>(s, new KeyValuePair<LinkedList<string>, HashSet<string>>(ll, hs)));

                            return list;
                        });

                        if (tempList.Count > 0)
                        {
                            LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>> tempLinkedList = new LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>(Shuffle<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>>(tempList));

                            sequenceList.Add(tempLinkedList.First().Key);
                            linkedList = tempLinkedList.First().Value.Key;

                            foreach (string s in tempLinkedList.First().Value.Value)
                            {
                                if (!hashSet.Contains(s))
                                {
                                    hashSet.Add(s);
                                }
                            }

                            cachedDictionary.Add(i, tempLinkedList);
                        }
                        else
                        {
                            isAvailable = false;
                        }
                    }

                    i++;

                    return sequenceList;
                }));

                if (isAvailable && terms.Any() == preparedSequenceList.Exists(delegate(Sequence sequence)
                {
                    foreach (object o in sequence)
                    {
                        Message message = o as Message;

                        if (message != null)
                        {
                            return Regex.IsMatch(message.Text, @"(?<Open>\{{2})*\{([^{}]|((\{|}){2})+)+}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant);
                        }
                    }

                    return false;
                }))
                {
                    Dictionary<string, Entry> d = new Dictionary<string, Entry>();

                    preparedSequenceList.ForEach(delegate(Sequence sequence)
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

                                for (Match match = Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                                {
                                    Entry entry;

                                    if (match.Index > index)
                                    {
                                        stringBuilder.Append(Regex.Replace(message.Text.Substring(index, match.Index - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                        {
                                            return m.Value.Substring(m.Length / 2);
                                        }), RegexOptions.CultureInvariant));
                                    }

                                    if (match.Groups[1].Success)
                                    {
                                        stringBuilder.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2));
                                    }

                                    if (d.TryGetValue(match.Groups[2].Value, out entry))
                                    {
                                        if (stringBuilder.Length > 0)
                                        {
                                            newMessage.Add(stringBuilder.ToString());
                                            stringBuilder.Clear();
                                        }

                                        newMessage.Add(entry);
                                    }
                                    else
                                    {
                                        string pattern;

                                        if (match.Groups[3].Success)
                                        {
                                            int j = match.Groups[2].Index;
                                            StringBuilder sb = new StringBuilder();

                                            foreach (Capture capture in match.Groups[3].Captures)
                                            {
                                                if (capture.Index > j)
                                                {
                                                    sb.Append(message.Text.Substring(j, capture.Index - j));
                                                }

                                                sb.Append(capture.Value.Substring(capture.Length / 2));
                                                j = capture.Index + capture.Length;
                                            }

                                            if (match.Groups[2].Index + match.Groups[2].Length > j)
                                            {
                                                sb.Append(message.Text.Substring(j, match.Groups[2].Index + match.Groups[2].Length - j));
                                            }

                                            pattern = sb.ToString();
                                        }
                                        else
                                        {
                                            pattern = match.Groups[2].Value;
                                        }

                                        foreach (string term in terms)
                                        {
                                            List<string> attributeList;

                                            if (wordDictionary.TryGetValue(term, out attributeList) && attributeList.Exists(delegate(string attribute)
                                            {
                                                return Regex.IsMatch(attribute, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
                                            }))
                                            {
                                                Entry newEntry = new Entry();
                                                KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair1;

                                                newEntry.Title = term;

                                                if (this.cacheDictionary.TryGetValue(term, out keyValuePair1))
                                                {
                                                    Nullable<KeyValuePair<Entry, double>> maxKeyValuePair = null;
                                                    double sum = 0;

                                                    keyValuePair1.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                                                    {
                                                        if (maxKeyValuePair.HasValue)
                                                        {
                                                            if (keyValuePair2.Value * keyValuePair1.Value > maxKeyValuePair.Value.Value && keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                                            {
                                                                maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                                            }
                                                        }
                                                        else if (keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                                        {
                                                            maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                                        }

                                                        sum += keyValuePair2.Value;
                                                    });

                                                    if (maxKeyValuePair.HasValue)
                                                    {
                                                        StringBuilder sb = new StringBuilder();

                                                        foreach (Match m in Regex.Matches(maxKeyValuePair.Value.Key.Description, "<.+?>", RegexOptions.CultureInvariant | RegexOptions.Singleline))
                                                        {
                                                            sb.Append(m.Value);
                                                        }

                                                        newEntry.Description = sb.ToString();
                                                    }

                                                    newEntry.Score = new Nullable<double>(sum / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value);
                                                }

                                                foreach (string s in from s in terms where !s.Equals(term) select s)
                                                {
                                                    newEntry.Tags.Add(s);
                                                }

                                                if (stringBuilder.Length > 0)
                                                {
                                                    newMessage.Add(stringBuilder.ToString());
                                                    stringBuilder.Clear();
                                                }

                                                newMessage.Add(newEntry);
                                                d.Add(match.Groups[2].Value, newEntry);
                                                wordDictionary.Remove(term);

                                                break;
                                            }
                                        }
                                    }

                                    if (match.Groups[4].Success)
                                    {
                                        stringBuilder.Append(match.Groups[4].Value.Substring(match.Groups[4].Length / 2));
                                    }

                                    index = match.Index + match.Length;
                                }

                                if (message.Text.Length > index)
                                {
                                    stringBuilder.Append(Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                    {
                                        return m.Value.Substring(m.Length / 2);
                                    }), RegexOptions.CultureInvariant));
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
                    });

                    break;
                }

                preparedSequenceList.Clear();
                this.sequenceStateDictionary.Clear();

                foreach (KeyValuePair<string, string> kvp in stateDictionary)
                {
                    this.sequenceStateDictionary.Add(kvp.Key, kvp.Value);
                }

                for (int j = i - 1; j >= 0; j--)
                {
                    LinkedList<KeyValuePair<Sequence, KeyValuePair<LinkedList<string>, HashSet<string>>>> ll;

                    if (cachedDictionary.TryGetValue(j, out ll))
                    {
                        ll.RemoveFirst();

                        if (ll.Count > 0)
                        {
                            break;
                        }
                        else
                        {
                            cachedDictionary.Remove(j);
                        }
                    }
                }
            } while (cachedDictionary.Count > 0);

            return preparedSequenceList;
        }

        private Stack<Sequence> GetSequenceStack(Sequence sourceSequence, Sequence targetSequence)
        {
            Stack<Sequence> sequenceStack = new Stack<Sequence>();

            sequenceStack.Push(sourceSequence);

            if (sequenceStack.Peek() != targetSequence)
            {
                foreach (object o in sourceSequence)
                {
                    Sequence sequence = o as Sequence;

                    if (sequence != null)
                    {
                        Stack<Sequence> stack = GetSequenceStack(sequence, targetSequence);

                        if (stack.Count > 0 && stack.Peek() == targetSequence)
                        {
                            Stack<Sequence> tempStack = new Stack<Sequence>();

                            do
                            {
                                tempStack.Push(stack.Pop());
                            } while (stack.Count > 0);

                            do
                            {
                                sequenceStack.Push(tempStack.Pop());
                            } while (tempStack.Count > 0);

                            return sequenceStack;
                        }
                    }
                }

                sequenceStack.Pop();
            }

            return sequenceStack;
        }

        public bool TryEnqueue(IEnumerable<Sequence> sequences)
        {
            var query = from sequence in sequences where this.characterCollection.Any(x => x.Name.Equals(sequence.Owner)) select sequence;

            if (!query.Any())
            {
                return false;
            }

            foreach (var sequence in query)
            {
                this.sequenceQueue.Enqueue(sequence);
            }

            this.idleTimeSpan = TimeSpan.Zero;

            return true;
        }

        public bool TryDequeue(string name, out Sequence sequence)
        {
            sequence = null;

            if (this.sequenceQueue.Count > 0)
            {
                if (this.sequenceQueue.Peek().Owner.Equals(name))
                {
                    sequence = this.sequenceQueue.Dequeue();

                    return true;
                }
            }

            return false;
        }

        public void Idle()
        {
            foreach (Sequence sequence in from sequence in Prepare(from sequence in this.sequenceCollection where sequence.Name.Equals("Idle") select sequence, null) where this.characterCollection.Any(x => x.Name.Equals(sequence.Owner)) select sequence)
            {
                this.sequenceQueue.Enqueue(sequence);
            }
        }

        public void Tick(DateTime dateTime)
        {
            IEnumerable<Sequence> preparedSequences = Prepare(from sequence in this.sequenceCollection where sequence.Name.Equals("Tick") select sequence, dateTime.ToString("s", CultureInfo.InvariantCulture));

            foreach (Sequence sequence in preparedSequences)
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

                        for (Match match = Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                        {
                            string pattern;

                            if (match.Index > index)
                            {
                                stringBuilder.Append(Regex.Replace(message.Text.Substring(index, match.Index - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                {
                                    return m.Value.Substring(m.Length / 2);
                                }), RegexOptions.CultureInvariant));
                            }

                            if (match.Groups[1].Success)
                            {
                                stringBuilder.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2));
                            }

                            if (match.Groups[3].Success)
                            {
                                int i = match.Groups[2].Index;
                                StringBuilder sb = new StringBuilder();

                                foreach (Capture capture in match.Groups[3].Captures)
                                {
                                    if (capture.Index > i)
                                    {
                                        sb.Append(message.Text.Substring(i, capture.Index - i));
                                    }

                                    sb.Append(capture.Value.Substring(capture.Length / 2));
                                    i = capture.Index + capture.Length;
                                }

                                if (match.Groups[2].Index + match.Groups[2].Length > i)
                                {
                                    sb.Append(message.Text.Substring(i, match.Groups[2].Index + match.Groups[2].Length - i));
                                }

                                pattern = sb.ToString();
                            }
                            else
                            {
                                pattern = match.Groups[2].Value;
                            }

                            stringBuilder.Append(dateTime.ToString(pattern));

                            if (match.Groups[4].Success)
                            {
                                stringBuilder.Append(match.Groups[4].Value.Substring(match.Groups[4].Length / 2));
                            }

                            index = match.Index + match.Length;
                        }

                        if (message.Text.Length > index)
                        {
                            stringBuilder.Append(Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                            {
                                return m.Value.Substring(m.Length / 2);
                            }), RegexOptions.CultureInvariant));
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
            }

            TryEnqueue(preparedSequences);
        }

        public void Learn(string term)
        {
            Queue<Sequence> sequenceQueue = new Queue<Sequence>(this.sequenceCollection);
            Dictionary<string, HashSet<string>> patternDicitonary = new Dictionary<string, HashSet<string>>();
            Dictionary<string, bool> attributeDictionary = this.wordCollection.Aggregate<Word, Dictionary<string, bool>>(new Dictionary<string, bool>(), delegate(Dictionary<string, bool> dicitonary, Word word)
            {
                if (word.Name.Equals(term))
                {
                    foreach (string attribute in word.Attributes)
                    {
                        if (dicitonary.ContainsKey(attribute))
                        {
                            dicitonary[attribute] = false;
                        }
                        else
                        {
                            dicitonary.Add(attribute, false);
                        }
                    }
                }
                else
                {
                    foreach (string attribute in word.Attributes)
                    {
                        if (!dicitonary.ContainsKey(attribute))
                        {
                            dicitonary.Add(attribute, true);
                        }
                    }
                }

                return dicitonary;
            });
            Dictionary<string, List<Entry>> attachmentDictionary = new Dictionary<string, List<Entry>>();

            while (sequenceQueue.Count > 0)
            {
                Sequence sequence1 = sequenceQueue.Dequeue();

                foreach (object o in sequence1)
                {
                    Sequence sequence2 = o as Sequence;

                    if (sequence2 == null)
                    {
                        Message message = o as Message;

                        if (message != null)
                        {
                            for (Match match = Regex.Match(message.Text, @"(?<Open>\{{2})*\{(?<1>(?:[^{}]|(?<2>(?:(?:\{|}){2})+))+)}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                            {
                                string pattern;

                                if (match.Groups[2].Success)
                                {
                                    int i = match.Groups[1].Index;
                                    StringBuilder sb = new StringBuilder();

                                    foreach (Capture capture in match.Groups[2].Captures)
                                    {
                                        if (capture.Index > i)
                                        {
                                            sb.Append(message.Text.Substring(i, capture.Index - i));
                                        }

                                        sb.Append(capture.Value.Substring(capture.Length / 2));
                                        i = capture.Index + capture.Length;
                                    }

                                    if (match.Groups[1].Index + match.Groups[1].Length > i)
                                    {
                                        sb.Append(message.Text.Substring(i, match.Groups[1].Index + match.Groups[1].Length - i));
                                    }

                                    pattern = sb.ToString();
                                }
                                else
                                {
                                    pattern = match.Groups[1].Value;
                                }

                                for (int i = 0; i < pattern.Length; i++)
                                {
                                    string s = pattern.Substring(i, 1);

                                    if (s.Equals(Regex.Escape(s)))
                                    {
                                        HashSet<string> hashSet;

                                        if (patternDicitonary.TryGetValue(sequence1.Owner, out hashSet))
                                        {
                                            if (!hashSet.Contains(pattern))
                                            {
                                                hashSet.Add(pattern);
                                            }
                                        }
                                        else
                                        {
                                            hashSet = new HashSet<string>();
                                            hashSet.Add(pattern);
                                            patternDicitonary.Add(sequence1.Owner, hashSet);
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (sequence2.Any())
                    {
                        sequenceQueue.Enqueue(sequence2);
                    }
                }
            }

            foreach (KeyValuePair<string, HashSet<string>> kvp1 in patternDicitonary)
            {
                List<Entry> entryList = new List<Entry>(); 
                
                foreach (KeyValuePair<string, bool> kvp2 in attributeDictionary)
                {
                    if (kvp1.Value.Any(delegate(string s)
                    {
                        return Regex.IsMatch(kvp2.Key, s, RegexOptions.CultureInvariant | RegexOptions.Singleline);
                    }))
                    {
                        Entry entry = new Entry();

                        entry.Title = kvp2.Key;
                        entry.Enabled = kvp2.Value;
                        entry.ReadOnly = false;

                        entryList.Add(entry);
                    }
                }

                if (entryList.Count > 0)
                {
                    entryList.Sort(delegate(Entry e1, Entry e2)
                    {
                        return String.Compare(e1.Title, e2.Title, StringComparison.CurrentCulture);
                    });

                    attachmentDictionary.Add(kvp1.Key, entryList);
                }
            }

            var query = from sequence in this.sequenceCollection where sequence.Name.Equals("Learn") select sequence;
            HashSet<Sequence> sequenceHashSet = new HashSet<Sequence>();
            HashSet<Message> messageHashSet = new HashSet<Message>();
            
            TryEnqueue(Prepare(query, term, delegate(IEnumerable<Sequence> collection)
            {
                Sequence[] sequences = collection.ToArray();
                Sequence sequence = sequences[new Random(Environment.TickCount).Next(sequences.Length)];

                if (query.Any(x => x == sequence) || sequenceHashSet.Contains(sequence))
                {
                    foreach (object o in sequence)
                    {
                        Sequence s = o as Sequence;

                        if (s == null)
                        {
                            Message message = o as Message;

                            if (message != null && !messageHashSet.Contains(message))
                            {
                                messageHashSet.Add(message);
                            }
                        }
                        else if (s.Any() && !sequenceHashSet.Contains(s))
                        {
                            sequenceHashSet.Add(s);
                        }
                    }
                }

                return new Sequence[] { sequence };
            }).Aggregate<Sequence, List<Sequence>>(new List<Sequence>(), delegate(List<Sequence> preparedSequenceList, Sequence sequence)
            {
                System.Collections.ArrayList arrayList = new System.Collections.ArrayList();

                foreach (object obj in sequence)
                {
                    Message message = obj as Message;

                    if (message == null)
                    {
                        arrayList.Add(obj);
                    }
                    else
                    {
                        int index = 0;
                        StringBuilder stringBuilder = new StringBuilder();
                        Message newMessage = new Message();
                        List<Entry> entryList;

                        for (Match match = Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                        {
                            string pattern;

                            if (match.Index > index)
                            {
                                stringBuilder.Append(Regex.Replace(message.Text.Substring(index, match.Index - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                {
                                    return m.Value.Substring(m.Length / 2);
                                }), RegexOptions.CultureInvariant));
                            }

                            if (match.Groups[1].Success)
                            {
                                stringBuilder.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2));
                            }

                            if (match.Groups[3].Success)
                            {
                                int i = match.Groups[2].Index;
                                StringBuilder sb = new StringBuilder();

                                foreach (Capture capture in match.Groups[3].Captures)
                                {
                                    if (capture.Index > i)
                                    {
                                        sb.Append(message.Text.Substring(i, capture.Index - i));
                                    }

                                    sb.Append(capture.Value.Substring(capture.Length / 2));
                                    i = capture.Index + capture.Length;
                                }

                                if (match.Groups[2].Index + match.Groups[2].Length > i)
                                {
                                    sb.Append(message.Text.Substring(i, match.Groups[2].Index + match.Groups[2].Length - i));
                                }

                                pattern = sb.ToString();
                            }
                            else
                            {
                                pattern = match.Groups[2].Value;
                            }

                            if (Regex.IsMatch(term, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline))
                            {
                                Entry newEntry = new Entry();
                                KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair1;

                                newEntry.Title = term;

                                if (this.cacheDictionary.TryGetValue(term, out keyValuePair1))
                                {
                                    Nullable<KeyValuePair<Entry, double>> maxKeyValuePair = null;
                                    double sum = 0;

                                    keyValuePair1.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                                    {
                                        if (maxKeyValuePair.HasValue)
                                        {
                                            if (keyValuePair2.Value * keyValuePair1.Value > maxKeyValuePair.Value.Value && keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                            {
                                                maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                            }
                                        }
                                        else if (keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                        {
                                            maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                        }

                                        sum += keyValuePair2.Value;
                                    });

                                    if (maxKeyValuePair.HasValue)
                                    {
                                        StringBuilder sb = new StringBuilder();

                                        foreach (Match m in Regex.Matches(maxKeyValuePair.Value.Key.Description, "<.+?>", RegexOptions.CultureInvariant | RegexOptions.Singleline))
                                        {
                                            sb.Append(m.Value);
                                        }

                                        newEntry.Description = sb.ToString();
                                    }

                                    newEntry.Score = new Nullable<double>(sum / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value);
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

                            if (match.Groups[4].Success)
                            {
                                stringBuilder.Append(match.Groups[4].Value.Substring(match.Groups[4].Length / 2));
                            }

                            index = match.Index + match.Length;
                        }

                        if (message.Text.Length > index)
                        {
                            stringBuilder.Append(Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                            {
                                return m.Value.Substring(m.Length / 2);
                            }), RegexOptions.CultureInvariant));
                            newMessage.Add(stringBuilder.ToString());
                        }
                        else if (stringBuilder.Length > 0)
                        {
                            newMessage.Add(stringBuilder.ToString());
                        }

                        newMessage.Speed = message.Speed;
                        newMessage.Duration = message.Duration;

                        if (messageHashSet.Contains(message) && attachmentDictionary.TryGetValue(sequence.Owner, out entryList))
                        {
                            entryList.ForEach(delegate(Entry entry)
                            {
                                newMessage.Attachments.Add(entry);
                            });
                        }

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

        public void Alert(IEnumerable<Entry> entries)
        {
            var query = from sequence in this.sequenceCollection where sequence.Name.Equals("Alert") select sequence;
            HashSet<Sequence> sequenceHashSet = new HashSet<Sequence>();
            HashSet<Message> messageHashSet = new HashSet<Message>();
            IEnumerable<Sequence> preparedSequences = Prepare(query, null, delegate(IEnumerable<Sequence> collection)
            {
                Sequence[] sequences = collection.ToArray();
                Sequence sequence = sequences[new Random(Environment.TickCount).Next(sequences.Length)];

                if (query.Any(x => x == sequence) || sequenceHashSet.Contains(sequence))
                {
                    foreach (object o in sequence)
                    {
                        Sequence s = o as Sequence;

                        if (s == null)
                        {
                            Message message = o as Message;

                            if (message != null && !messageHashSet.Contains(message))
                            {
                                messageHashSet.Add(message);
                            }
                        }
                        else if (s.Any() && !sequenceHashSet.Contains(s))
                        {
                            sequenceHashSet.Add(s);
                        }
                    }
                }

                return new Sequence[] { sequence };
            });

            foreach (Sequence sequence in preparedSequences)
            {
                System.Collections.ArrayList arrayList = new System.Collections.ArrayList();

                foreach (object obj in sequence)
                {
                    Message message = obj as Message;

                    if (message == null)
                    {
                        arrayList.Add(obj);
                    }
                    else
                    {
                        Message newMessage = new Message();

                        foreach (object o in message)
                        {
                            newMessage.Add(o);
                        }

                        newMessage.Speed = message.Speed;
                        newMessage.Duration = message.Duration;

                        if (messageHashSet.Contains(message))
                        {
                            foreach (Entry entry in entries)
                            {
                                newMessage.Attachments.Add(entry);
                            }
                        }

                        arrayList.Add(newMessage);
                    }
                }

                sequence.Clear();

                foreach (object o in arrayList)
                {
                    sequence.Add(o);
                }
            }

            TryEnqueue(preparedSequences);
        }

        public void Trend(IEnumerable<string> terms)
        {
            List<Entry> entryList = new List<Entry>();
            
            foreach (string term in terms)
            {
                Entry newEntry = new Entry();
                KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair1;

                newEntry.Title = term;

                if (this.cacheDictionary.TryGetValue(term, out keyValuePair1))
                {
                    Nullable<KeyValuePair<Entry, double>> maxKeyValuePair = null;
                    double sum = 0;

                    keyValuePair1.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                    {
                        if (maxKeyValuePair.HasValue)
                        {
                            if (keyValuePair2.Value * keyValuePair1.Value > maxKeyValuePair.Value.Value && keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                            {
                                maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                            }
                        }
                        else if (keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                        {
                            maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                        }

                        sum += keyValuePair2.Value;
                    });

                    if (maxKeyValuePair.HasValue)
                    {
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (Match m in Regex.Matches(maxKeyValuePair.Value.Key.Description, "<.+?>", RegexOptions.CultureInvariant | RegexOptions.Singleline))
                        {
                            stringBuilder.Append(m.Value);
                        }

                        newEntry.Description = stringBuilder.ToString();
                    }

                    newEntry.Score = new Nullable<double>(sum / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value);
                }

                entryList.Add(newEntry);
            }

            entryList.Sort(delegate(Entry e1, Entry e2)
            {
                if (e1.Score > e2.Score)
                {
                    return 1;
                }
                else if (e1.Score < e2.Score)
                {
                    return -1;
                }

                return 0;
            });
            entryList.Reverse();

            var query = from sequence in this.sequenceCollection where sequence.Name.Equals("Trend") select sequence;
            HashSet<Sequence> sequenceHashSet = new HashSet<Sequence>();
            HashSet<Message> messageHashSet = new HashSet<Message>();
            IEnumerable<Sequence> preparedSequences = Prepare(query, null, delegate(IEnumerable<Sequence> collection)
            {
                Sequence[] sequences = collection.ToArray();
                Sequence sequence = sequences[new Random(Environment.TickCount).Next(sequences.Length)];

                if (query.Any(x => x == sequence) || sequenceHashSet.Contains(sequence))
                {
                    foreach (object o in sequence)
                    {
                        Sequence s = o as Sequence;

                        if (s == null)
                        {
                            Message message = o as Message;

                            if (message != null && !messageHashSet.Contains(message))
                            {
                                messageHashSet.Add(message);
                            }
                        }
                        else if (s.Any() && !sequenceHashSet.Contains(s))
                        {
                            sequenceHashSet.Add(s);
                        }
                    }
                }

                return new Sequence[] { sequence };
            });

            foreach (Sequence sequence in preparedSequences)
            {
                System.Collections.ArrayList arrayList = new System.Collections.ArrayList();

                foreach (object obj in sequence)
                {
                    Message message = obj as Message;

                    if (message == null)
                    {
                        arrayList.Add(obj);
                    }
                    else
                    {
                        Message newMessage = new Message();

                        foreach (object o in message)
                        {
                            newMessage.Add(o);
                        }

                        newMessage.Speed = message.Speed;
                        newMessage.Duration = message.Duration;

                        if (messageHashSet.Contains(message))
                        {
                            entryList.ForEach(delegate(Entry entry)
                            {
                                newMessage.Attachments.Add(entry);
                            });
                        }

                        arrayList.Add(newMessage);
                    }
                }

                sequence.Clear();

                foreach (object o in arrayList)
                {
                    sequence.Add(o);
                }
            }

            TryEnqueue(preparedSequences);
        }

        public void Search(string query)
        {
            Dictionary<Uri, KeyValuePair<Entry, double[]>> vectorDictionary = new Dictionary<Uri, KeyValuePair<Entry, double[]>>();
            Dictionary<string, double> idfDictionary = new Dictionary<string, double>();
            int i = 0;
            Dictionary<char, List<string>> termDictionary = this.wordCollection.Aggregate<Word, Dictionary<char, List<string>>>(new Dictionary<char, List<string>>(), delegate(Dictionary<char, List<string>> d, Word word)
            {
                if (word.Name.Length > 0)
                {
                    List<string> nameList;

                    if (!d.TryGetValue(word.Name[0], out nameList))
                    {
                        nameList = new List<string>();
                        d.Add(word.Name[0], nameList);
                    }

                    nameList.Add(word.Name);
                }

                return d;
            });
            List<Entry> entryList = new List<Entry>();

            foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> keyValuePair1 in this.cacheDictionary)
            {
                keyValuePair1.Value.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                {
                    KeyValuePair<Entry, double[]> kvp;

                    if (vectorDictionary.TryGetValue(keyValuePair2.Key.Resource, out kvp))
                    {
                        kvp.Value[i] = keyValuePair2.Value * keyValuePair1.Value.Value;
                    }
                    else
                    {
                        double[] vector2 = new double[this.cacheDictionary.Count];

                        vector2[i] = keyValuePair2.Value * keyValuePair1.Value.Value;
                        vectorDictionary.Add(keyValuePair2.Key.Resource, new KeyValuePair<Entry, double[]>(keyValuePair2.Key, vector2));
                    }
                });
                idfDictionary.Add(keyValuePair1.Key, keyValuePair1.Value.Value);

                i++;
            }

            Task.Factory.StartNew(delegate
            {
                Dictionary<char, List<string>> dictionary = idfDictionary.Keys.Aggregate<string, Dictionary<char, List<string>>>(new Dictionary<char, List<string>>(), delegate(Dictionary<char, List<string>> d, string term)
                {
                    if (term.Length > 0)
                    {
                        List<string> termList;

                        if (!d.TryGetValue(term[0], out termList))
                        {
                            termList = new List<string>();
                            d.Add(term[0], termList);
                        }

                        termList.Add(term);
                    }

                    return d;
                });

                foreach (System.Configuration.ConnectionStringSettings settings in System.Configuration.ConfigurationManager.ConnectionStrings)
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(settings.ProviderName);

                    using (IDbConnection connection = factory.CreateConnection())
                    {
                        connection.ConnectionString = settings.ConnectionString;
                        connection.Open();

                        using (IDbCommand command = factory.CreateCommand())
                        {
                            IDataReader reader = null;

                            command.Connection = connection;
                            command.CommandText = BuildSelectStatement(query);

                            try
                            {
                                reader = command.ExecuteReader();

                                while (reader.Read())
                                {
                                    Entry entry = new Entry();

                                    if (!Convert.IsDBNull(reader["Resource"]))
                                    {
                                        Uri uri;

                                        if (Uri.TryCreate((string)reader["Resource"], UriKind.RelativeOrAbsolute, out uri))
                                        {
                                            entry.Resource = uri;
                                        }
                                    }

                                    if (!Convert.IsDBNull(reader["Title"]))
                                    {
                                        entry.Title = reader["Title"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Description"]))
                                    {
                                        entry.Description = reader["Description"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Author"]))
                                    {
                                        entry.Author = reader["Author"] as string;
                                    }

                                    if (!Convert.IsDBNull(reader["Created"]))
                                    {
                                        entry.Created = (DateTime)reader["Created"];
                                    }

                                    if (!Convert.IsDBNull(reader["Modified"]))
                                    {
                                        entry.Modified = (DateTime)reader["Modified"];
                                    }

                                    Dictionary<string, double> tfDictionary = GetTermFrequency(dictionary, entry);
                                    List<double> vectorList = new List<double>();
                                    bool isZeroVector = true;
                                    List<string> termList = new List<string>();

                                    foreach (KeyValuePair<string, double> kvp in idfDictionary)
                                    {
                                        double tf;

                                        if (tfDictionary.TryGetValue(kvp.Key, out tf))
                                        {
                                            vectorList.Add(tf * kvp.Value);
                                            isZeroVector = false;
                                        }
                                        else
                                        {
                                            vectorList.Add(0);
                                        }
                                    }

                                    if (!isZeroVector)
                                    {
                                        Entry e = null;
                                        double lowestDistance = Double.MaxValue;

                                        foreach (KeyValuePair<Entry, double[]> kvp in vectorDictionary.Values)
                                        {
                                            double distance = 0;

                                            for (int j = 0; j < idfDictionary.Count; j++)
                                            {
                                                distance += (vectorList[j] - kvp.Value[j]) * (vectorList[j] - kvp.Value[j]);
                                            }

                                            distance = Math.Sqrt(distance);

                                            if (lowestDistance > distance)
                                            {
                                                e = kvp.Key;
                                                lowestDistance = distance;
                                            }
                                        }

                                        if (e != null)
                                        {
                                            foreach (Entry similarEntry in e.SimilarEntries)
                                            {
                                                entry.SimilarEntries.Add(similarEntry);
                                            }
                                        }
                                    }

                                    if (entry.Title != null)
                                    {
                                        termList.AddRange(GetTermList(termDictionary, entry.Title));
                                    }

                                    if (entry.Description != null)
                                    {
                                        GetTermList(termDictionary, Regex.Replace(entry.Description, "<.+?>", String.Empty, RegexOptions.CultureInvariant | RegexOptions.Singleline)).ForEach(delegate(string term)
                                        {
                                            if (!termList.Contains(term))
                                            {
                                                termList.Add(term);
                                            }
                                        });
                                    }

                                    termList.ForEach(delegate(string s)
                                    {
                                        entry.Tags.Add(s);
                                    });

                                    entryList.Add(entry);
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                {
                                    reader.Close();
                                }
                            }
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning).ContinueWith(delegate
            {
                var q = from sequence in this.sequenceCollection where sequence.Name.Equals("Search") select sequence;
                HashSet<Sequence> sequenceHashSet = new HashSet<Sequence>();
                HashSet<Message> messageHashSet = new HashSet<Message>();

                TryEnqueue(Prepare(q, query, delegate(IEnumerable<Sequence> collection)
                {
                    Sequence[] sequences = collection.ToArray();
                    Sequence sequence = sequences[new Random(Environment.TickCount).Next(sequences.Length)];

                    if (q.Any(x => x == sequence) || sequenceHashSet.Contains(sequence))
                    {
                        foreach (object o in sequence)
                        {
                            Sequence s = o as Sequence;

                            if (s == null)
                            {
                                Message message = o as Message;

                                if (message != null && !messageHashSet.Contains(message))
                                {
                                    messageHashSet.Add(message);
                                }
                            }
                            else if (s.Any() && !sequenceHashSet.Contains(s))
                            {
                                sequenceHashSet.Add(s);
                            }
                        }
                    }

                    return new Sequence[] { sequence };
                }).Aggregate<Sequence, List<Sequence>>(new List<Sequence>(), delegate(List<Sequence> preparedSequenceList, Sequence sequence)
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
                            StringBuilder stringBuilder1 = new StringBuilder();
                            Message newMessage = new Message();

                            for (Match match = Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                            {
                                string pattern;

                                if (match.Index > index)
                                {
                                    stringBuilder1.Append(Regex.Replace(message.Text.Substring(index, match.Index - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                    {
                                        return m.Value.Substring(m.Length / 2);
                                    }), RegexOptions.CultureInvariant));
                                }

                                if (match.Groups[1].Success)
                                {
                                    stringBuilder1.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2));
                                }

                                if (match.Groups[3].Success)
                                {
                                    int j = match.Groups[2].Index;
                                    StringBuilder sb = new StringBuilder();

                                    foreach (Capture capture in match.Groups[3].Captures)
                                    {
                                        if (capture.Index > j)
                                        {
                                            sb.Append(message.Text.Substring(j, capture.Index - j));
                                        }

                                        sb.Append(capture.Value.Substring(capture.Length / 2));
                                        j = capture.Index + capture.Length;
                                    }

                                    if (match.Groups[2].Index + match.Groups[2].Length > j)
                                    {
                                        sb.Append(message.Text.Substring(j, match.Groups[2].Index + match.Groups[2].Length - j));
                                    }

                                    pattern = sb.ToString();
                                }
                                else
                                {
                                    pattern = match.Groups[2].Value;
                                }

                                if (Regex.IsMatch(query, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline))
                                {
                                    Entry newEntry = new Entry();
                                    KeyValuePair<List<KeyValuePair<Entry, double>>, double> keyValuePair1;

                                    newEntry.Title = query;

                                    if (this.cacheDictionary.TryGetValue(query, out keyValuePair1))
                                    {
                                        Nullable<KeyValuePair<Entry, double>> maxKeyValuePair = null;
                                        double sum = 0;

                                        keyValuePair1.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                                        {
                                            if (maxKeyValuePair.HasValue)
                                            {
                                                if (keyValuePair2.Value * keyValuePair1.Value > maxKeyValuePair.Value.Value && keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                                {
                                                    maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                                }
                                            }
                                            else if (keyValuePair2.Key.Description != null && Regex.IsMatch(keyValuePair2.Key.Description, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
                                            {
                                                maxKeyValuePair = new Nullable<KeyValuePair<Entry, double>>(new KeyValuePair<Entry, double>(keyValuePair2.Key, keyValuePair2.Value * keyValuePair1.Value));
                                            }

                                            sum += keyValuePair2.Value;
                                        });

                                        if (maxKeyValuePair.HasValue)
                                        {
                                            StringBuilder sb = new StringBuilder();

                                            foreach (Match m in Regex.Matches(maxKeyValuePair.Value.Key.Description, "<.+?>", RegexOptions.CultureInvariant | RegexOptions.Singleline))
                                            {
                                                sb.Append(m.Value);
                                            }

                                            newEntry.Description = sb.ToString();
                                        }

                                        newEntry.Score = new Nullable<double>(sum / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value);
                                    }

                                    if (stringBuilder1.Length > 0)
                                    {
                                        newMessage.Add(stringBuilder1.ToString());
                                        stringBuilder1.Clear();
                                    }

                                    newMessage.Add(newEntry);
                                }
                                else
                                {
                                    return preparedSequenceList;
                                }

                                if (match.Groups[4].Success)
                                {
                                    stringBuilder1.Append(match.Groups[4].Value.Substring(match.Groups[4].Length / 2));
                                }

                                index = match.Index + match.Length;
                            }

                            if (message.Text.Length > index)
                            {
                                stringBuilder1.Append(Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                {
                                    return m.Value.Substring(m.Length / 2);
                                }), RegexOptions.CultureInvariant));
                                newMessage.Add(stringBuilder1.ToString());
                            }
                            else if (stringBuilder1.Length > 0)
                            {
                                newMessage.Add(stringBuilder1.ToString());
                            }

                            newMessage.Speed = message.Speed;
                            newMessage.Duration = message.Duration;

                            if (messageHashSet.Contains(message))
                            {
                                entryList.ForEach(delegate(Entry entry)
                                {
                                    newMessage.Attachments.Add(entry);
                                });
                            }

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
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void Suggest(string caption, IEnumerable<Entry> entries)
        {
            var query = from sequence in this.sequenceCollection where sequence.Name.Equals("Suggest") select sequence;
            HashSet<Sequence> sequenceHashSet = new HashSet<Sequence>();
            HashSet<Message> messageHashSet = new HashSet<Message>();

            TryEnqueue(Prepare(query, caption, delegate(IEnumerable<Sequence> collection)
            {
                Sequence[] sequences = collection.ToArray();
                Sequence sequence = sequences[new Random(Environment.TickCount).Next(sequences.Length)];

                if (query.Any(x => x == sequence) || sequenceHashSet.Contains(sequence))
                {
                    foreach (object o in sequence)
                    {
                        Sequence s = o as Sequence;

                        if (s == null)
                        {
                            Message message = o as Message;

                            if (message != null && !messageHashSet.Contains(message))
                            {
                                messageHashSet.Add(message);
                            }
                        }
                        else if (s.Any() && !sequenceHashSet.Contains(s))
                        {
                            sequenceHashSet.Add(s);
                        }
                    }
                }

                return new Sequence[] { sequence };
            }).Aggregate<Sequence, List<Sequence>>(new List<Sequence>(), delegate(List<Sequence> preparedSequenceList, Sequence sequence)
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

                        for (Match match = Regex.Match(message.Text, @"(?<1>(?<Open>\{{2})*)\{(?<2>(?:[^{}]|(?<3>(?:(?:\{|}){2})+))+)}(?<4>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant); match.Success; match = match.NextMatch())
                        {
                            string pattern;

                            if (match.Index > index)
                            {
                                stringBuilder.Append(Regex.Replace(message.Text.Substring(index, match.Index - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                                {
                                    return m.Value.Substring(m.Length / 2);
                                }), RegexOptions.CultureInvariant));
                            }

                            if (match.Groups[1].Success)
                            {
                                stringBuilder.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2));
                            }

                            if (match.Groups[3].Success)
                            {
                                int i = match.Groups[2].Index;
                                StringBuilder sb = new StringBuilder();

                                foreach (Capture capture in match.Groups[3].Captures)
                                {
                                    if (capture.Index > i)
                                    {
                                        sb.Append(message.Text.Substring(i, capture.Index - i));
                                    }

                                    sb.Append(capture.Value.Substring(capture.Length / 2));
                                    i = capture.Index + capture.Length;
                                }

                                if (match.Groups[2].Index + match.Groups[2].Length > i)
                                {
                                    sb.Append(message.Text.Substring(i, match.Groups[2].Index + match.Groups[2].Length - i));
                                }

                                pattern = sb.ToString();
                            }
                            else
                            {
                                pattern = match.Groups[2].Value;
                            }

                            if (Regex.IsMatch(caption, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline))
                            {
                                stringBuilder.Append(caption);
                            }
                            else
                            {
                                return preparedSequenceList;
                            }

                            if (match.Groups[4].Success)
                            {
                                stringBuilder.Append(match.Groups[4].Value.Substring(match.Groups[4].Length / 2));
                            }

                            index = match.Index + match.Length;
                        }

                        if (message.Text.Length > index)
                        {
                            stringBuilder.Append(Regex.Replace(message.Text.Substring(index, message.Text.Length - index), @"\{\{|}}", new MatchEvaluator(delegate(Match m)
                            {
                                return m.Value.Substring(m.Length / 2);
                            }), RegexOptions.CultureInvariant));
                            newMessage.Add(stringBuilder.ToString());
                        }
                        else if (stringBuilder.Length > 0)
                        {
                            newMessage.Add(stringBuilder.ToString());
                        }

                        newMessage.Speed = message.Speed;
                        newMessage.Duration = message.Duration;

                        if (messageHashSet.Contains(message))
                        {
                            foreach (Entry entry in entries)
                            {
                                newMessage.Attachments.Add(entry);
                            }
                        }

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

        public void Activate()
        {
            List<string> mergedTermList = new List<string>();
            
            if (this.activateEntryQueue.Count == 0)
            {
                double[] vector1 = new double[this.cacheDictionary.Count];
                Dictionary<Uri, KeyValuePair<Entry, double[]>> vectorDictionary = new Dictionary<Uri, KeyValuePair<Entry, double[]>>();
                int i = 0;

                foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<Entry, double>>, double>> keyValuePair1 in this.cacheDictionary)
                {
                    double sum = 0;

                    keyValuePair1.Value.Key.ForEach(delegate(KeyValuePair<Entry, double> keyValuePair2)
                    {
                        KeyValuePair<Entry, double[]> kvp;

                        sum += keyValuePair2.Value;

                        if (vectorDictionary.TryGetValue(keyValuePair2.Key.Resource, out kvp))
                        {
                            kvp.Value[i] = keyValuePair2.Value * keyValuePair1.Value.Value;
                        }
                        else
                        {
                            double[] vector2 = new double[this.cacheDictionary.Count];

                            vector2[i] = keyValuePair2.Value * keyValuePair1.Value.Value;
                            vectorDictionary.Add(keyValuePair2.Key.Resource, new KeyValuePair<Entry, double[]>(keyValuePair2.Key, vector2));
                        }
                    });

                    vector1[i] = sum / (from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key select kvp2.Key.Resource).Distinct().Count() * keyValuePair1.Value.Value;
                    i++;
                }

                List<KeyValuePair<Entry, double>> entryList = new List<KeyValuePair<Entry, double>>();

                foreach (KeyValuePair<Entry, double[]> kvp in vectorDictionary.Values)
                {
                    double distance = 0;

                    for (int j = 0; j < this.cacheDictionary.Count; j++)
                    {
                        distance += (vector1[j] - kvp.Value[j]) * (vector1[j] - kvp.Value[j]);
                    }

                    entryList.Add(new KeyValuePair<Entry, double>(kvp.Key, Math.Sqrt(distance)));
                }

                entryList.Sort(delegate(KeyValuePair<Entry, double> kvp1, KeyValuePair<Entry, double> kvp2)
                {
                    if (kvp1.Value > kvp2.Value)
                    {
                        return 1;
                    }
                    else if (kvp1.Value < kvp2.Value)
                    {
                        return -1;
                    }

                    return 0;
                });
                entryList.ForEach(delegate(KeyValuePair<Entry, double> kvp)
                {
                    this.activateEntryQueue.Enqueue(kvp.Key);
                });
            }

            if (this.activateEntryQueue.Count > 0)
            {
                Entry entry = this.activateEntryQueue.Dequeue();

                mergedTermList.AddRange(from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key where kvp2.Key.Resource.Equals(entry.Resource) orderby kvp1.Value.Value * kvp2.Value descending select kvp1.Key);

                foreach (string s in from similarEntry in entry.SimilarEntries from kvp1 in this.cacheDictionary from kvp2 in kvp1.Value.Key where kvp2.Key.Resource.Equals(similarEntry.Resource) orderby kvp1.Value.Value * kvp2.Value descending select kvp1.Key)
                {
                    if (!mergedTermList.Contains(s))
                    {
                        mergedTermList.Add(s);
                    }
                }
            }

            List<string> usedTermList = Activate(from sequence in this.sequenceCollection where sequence.Name.Equals("Activate") select sequence, mergedTermList.FindAll(delegate(string term)
            {
                return !this.recentTermList.Contains(term);
            }));

            if (usedTermList.Count > 0)
            {
                usedTermList.ForEach(delegate(string s)
                {
                    if (!this.recentTermList.Contains(s))
                    {
                        this.recentTermList.Add(s);
                    }
                });
            }
            else
            {
                this.recentTermList.Clear();
            }
        }

        private List<string> Activate(IEnumerable<Sequence> sequences, List<string> termList)
        {
            IEnumerable<Sequence> preparedSequences = Prepare(sequences, null, termList);
            List<string> usedTermList = new List<string>();

            foreach (Sequence sequence in preparedSequences)
            {
                foreach (object obj in sequence)
                {
                    Message message = obj as Message;

                    if (message != null)
                    {
                        foreach (object o in message)
                        {
                            Entry entry = o as Entry;

                            if (entry != null)
                            {
                                entry.Tags.Clear();

                                termList.ForEach(delegate(string s)
                                {
                                    if (!s.Equals(entry.Title))
                                    {
                                        entry.Tags.Add(s);
                                    }
                                });

                                usedTermList.Add(entry.Title);
                            }
                        }
                    }
                }
            }

            if (!TryEnqueue(preparedSequences) && termList.Count > 0)
            {
                termList.Clear();

                return Activate(sequences, termList);
            }

            return usedTermList;
        }

        private Dictionary<string, double> GetTermFrequency(Dictionary<char, List<string>> dictionary, Entry entry)
        {
            double sum = 0;
            Dictionary<string, double> termFrequencyDictionary = new Dictionary<string, double>();

            if (entry.Title != null)
            {
                StringBuilder stringBuilder = new StringBuilder(entry.Title);

                while (stringBuilder.Length > 0)
                {
                    string s1 = stringBuilder.ToString();
                    List<string> filteredTermList1;
                    string selectedTerm1 = null;

                    if (dictionary.TryGetValue(s1[0], out filteredTermList1))
                    {
                        filteredTermList1.ForEach(delegate(string term)
                        {
                            if (s1.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm1 == null ? 0 : selectedTerm1.Length))
                            {
                                selectedTerm1 = term;
                            }
                        });
                    }

                    if (String.IsNullOrEmpty(selectedTerm1))
                    {
                        stringBuilder.Remove(0, 1);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1));
                        string selectedTerm2 = null;
                        int max = 0;

                        for (int i = 0; sb.Length > 0 && i < selectedTerm1.Length; i++)
                        {
                            string s2 = sb.ToString();
                            List<string> filteredTermList2;

                            if (dictionary.TryGetValue(s2[0], out filteredTermList2))
                            {
                                filteredTermList2.ForEach(delegate(string term)
                                {
                                    if (s2.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm2 == null ? 0 : selectedTerm2.Length))
                                    {
                                        selectedTerm2 = term;
                                        max = i + selectedTerm2.Length;
                                    }
                                });
                            }

                            sb.Remove(0, 1);
                        }

                        if (!String.IsNullOrEmpty(selectedTerm2) && selectedTerm1.Length < selectedTerm2.Length)
                        {
                            if (termFrequencyDictionary.ContainsKey(selectedTerm2))
                            {
                                termFrequencyDictionary[selectedTerm2] += 1;
                            }
                            else
                            {
                                termFrequencyDictionary.Add(selectedTerm2, 1);
                            }

                            stringBuilder.Remove(0, max);
                        }
                        else
                        {
                            if (termFrequencyDictionary.ContainsKey(selectedTerm1))
                            {
                                termFrequencyDictionary[selectedTerm1] += 1;
                            }
                            else
                            {
                                termFrequencyDictionary.Add(selectedTerm1, 1);
                            }

                            stringBuilder.Remove(0, selectedTerm1.Length);
                        }
                    }
                }
            }

            if (entry.Description != null)
            {
                StringBuilder stringBuilder = new StringBuilder(Regex.Replace(entry.Description, "<.+?>", String.Empty, RegexOptions.CultureInvariant | RegexOptions.Singleline));

                while (stringBuilder.Length > 0)
                {
                    string s1 = stringBuilder.ToString();
                    List<string> filteredTermList1;
                    string selectedTerm1 = null;

                    if (dictionary.TryGetValue(s1[0], out filteredTermList1))
                    {
                        filteredTermList1.ForEach(delegate(string term)
                        {
                            if (s1.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm1 == null ? 0 : selectedTerm1.Length))
                            {
                                selectedTerm1 = term;
                            }
                        });
                    }

                    if (String.IsNullOrEmpty(selectedTerm1))
                    {
                        stringBuilder.Remove(0, 1);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1));
                        string selectedTerm2 = null;
                        int max = 0;

                        for (int i = 0; sb.Length > 0 && i < selectedTerm1.Length; i++)
                        {
                            string s2 = sb.ToString();
                            List<string> filteredTermList2;

                            if (dictionary.TryGetValue(s2[0], out filteredTermList2))
                            {
                                filteredTermList2.ForEach(delegate(string term)
                                {
                                    if (s2.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm2 == null ? 0 : selectedTerm2.Length))
                                    {
                                        selectedTerm2 = term;
                                        max = i + selectedTerm2.Length;
                                    }
                                });
                            }

                            sb.Remove(0, 1);
                        }

                        if (!String.IsNullOrEmpty(selectedTerm2) && selectedTerm1.Length < selectedTerm2.Length)
                        {
                            if (termFrequencyDictionary.ContainsKey(selectedTerm2))
                            {
                                termFrequencyDictionary[selectedTerm2] += 1;
                            }
                            else
                            {
                                termFrequencyDictionary.Add(selectedTerm2, 1);
                            }

                            stringBuilder.Remove(0, max);
                        }
                        else
                        {
                            if (termFrequencyDictionary.ContainsKey(selectedTerm1))
                            {
                                termFrequencyDictionary[selectedTerm1] += 1;
                            }
                            else
                            {
                                termFrequencyDictionary.Add(selectedTerm1, 1);
                            }

                            stringBuilder.Remove(0, selectedTerm1.Length);
                        }
                    }
                }
            }

            sum = termFrequencyDictionary.Values.Sum();
            
            if (sum > 0)
            {
                foreach (string term in termFrequencyDictionary.Keys.ToArray())
                {
                    termFrequencyDictionary[term] = termFrequencyDictionary[term] / sum;
                }
            }

            return termFrequencyDictionary;
        }

        private Dictionary<string, double> GetInverseDocumentFrequency(Dictionary<char, List<string>> dictionary, List<Entry> entryList)
        {
            Dictionary<string, double> inverseDocumentFrequencyDictionary = new Dictionary<string, double>();

            entryList.ForEach(delegate(Entry entry)
            {
                HashSet<string> hashSet = new HashSet<string>();

                if (entry.Title != null)
                {
                    StringBuilder stringBuilder = new StringBuilder(entry.Title);

                    while (stringBuilder.Length > 0)
                    {
                        string s1 = stringBuilder.ToString();
                        List<string> filteredTermList1;
                        string selectedTerm1 = null;

                        if (dictionary.TryGetValue(s1[0], out filteredTermList1))
                        {
                            filteredTermList1.ForEach(delegate(string term)
                            {
                                if (s1.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm1 == null ? 0 : selectedTerm1.Length))
                                {
                                    selectedTerm1 = term;
                                }
                            });
                        }

                        if (String.IsNullOrEmpty(selectedTerm1))
                        {
                            stringBuilder.Remove(0, 1);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1));
                            string selectedTerm2 = null;
                            int max = 0;

                            for (int i = 0; sb.Length > 0 && i < selectedTerm1.Length; i++)
                            {
                                string s2 = sb.ToString();
                                List<string> filteredTermList2;

                                if (dictionary.TryGetValue(s2[0], out filteredTermList2))
                                {
                                    filteredTermList2.ForEach(delegate(string term)
                                    {
                                        if (s2.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm2 == null ? 0 : selectedTerm2.Length))
                                        {
                                            selectedTerm2 = term;
                                            max = i + selectedTerm2.Length;
                                        }
                                    });
                                }

                                sb.Remove(0, 1);
                            }

                            if (!String.IsNullOrEmpty(selectedTerm2) && selectedTerm1.Length < selectedTerm2.Length)
                            {
                                if (!hashSet.Contains(selectedTerm2))
                                {
                                    hashSet.Add(selectedTerm2);
                                }

                                stringBuilder.Remove(0, max);
                            }
                            else
                            {
                                if (!hashSet.Contains(selectedTerm1))
                                {
                                    hashSet.Add(selectedTerm1);
                                }

                                stringBuilder.Remove(0, selectedTerm1.Length);
                            }
                        }
                    }
                }

                if (entry.Description != null)
                {
                    StringBuilder stringBuilder = new StringBuilder(Regex.Replace(entry.Description, "<.+?>", String.Empty, RegexOptions.CultureInvariant | RegexOptions.Singleline));

                    while (stringBuilder.Length > 0)
                    {
                        string s1 = stringBuilder.ToString();
                        List<string> filteredTermList1;
                        string selectedTerm1 = null;

                        if (dictionary.TryGetValue(s1[0], out filteredTermList1))
                        {
                            filteredTermList1.ForEach(delegate(string term)
                            {
                                if (s1.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm1 == null ? 0 : selectedTerm1.Length))
                                {
                                    selectedTerm1 = term;
                                }
                            });
                        }

                        if (String.IsNullOrEmpty(selectedTerm1))
                        {
                            stringBuilder.Remove(0, 1);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1));
                            string selectedTerm2 = null;
                            int max = 0;

                            for (int i = 0; sb.Length > 0 && i < selectedTerm1.Length; i++)
                            {
                                string s2 = sb.ToString();
                                List<string> filteredTermList2;

                                if (dictionary.TryGetValue(s2[0], out filteredTermList2))
                                {
                                    filteredTermList2.ForEach(delegate(string term)
                                    {
                                        if (s2.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm2 == null ? 0 : selectedTerm2.Length))
                                        {
                                            selectedTerm2 = term;
                                            max = i + selectedTerm2.Length;
                                        }
                                    });
                                }

                                sb.Remove(0, 1);
                            }

                            if (!String.IsNullOrEmpty(selectedTerm2) && selectedTerm1.Length < selectedTerm2.Length)
                            {
                                if (!hashSet.Contains(selectedTerm2))
                                {
                                    hashSet.Add(selectedTerm2);
                                }

                                stringBuilder.Remove(0, max);
                            }
                            else
                            {
                                if (!hashSet.Contains(selectedTerm1))
                                {
                                    hashSet.Add(selectedTerm1);
                                }

                                stringBuilder.Remove(0, selectedTerm1.Length);
                            }
                        }
                    }
                }

                foreach (string term in hashSet)
                {
                    if (inverseDocumentFrequencyDictionary.ContainsKey(term))
                    {
                        inverseDocumentFrequencyDictionary[term] += 1;
                    }
                    else
                    {
                        inverseDocumentFrequencyDictionary.Add(term, 1);
                    }
                }
            });

            foreach (string term in inverseDocumentFrequencyDictionary.Keys.ToArray())
            {
                inverseDocumentFrequencyDictionary[term] = Math.Log(entryList.Count / inverseDocumentFrequencyDictionary[term], Math.E);
            }

            return inverseDocumentFrequencyDictionary;
        }

        private List<string> GetTermList(Dictionary<char, List<string>> dictionary, string text)
        {
            StringBuilder stringBuilder = new StringBuilder(text);
            List<string> selectedTermList = new List<string>();

            while (stringBuilder.Length > 0)
            {
                string s1 = stringBuilder.ToString();
                List<string> filteredTermList1;
                string selectedTerm1 = null;

                if (dictionary.TryGetValue(s1[0], out filteredTermList1))
                {
                    filteredTermList1.ForEach(delegate(string term)
                    {
                        if (s1.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm1 == null ? 0 : selectedTerm1.Length))
                        {
                            selectedTerm1 = term;
                        }
                    });
                }

                if (String.IsNullOrEmpty(selectedTerm1))
                {
                    stringBuilder.Remove(0, 1);
                }
                else
                {
                    StringBuilder sb = new StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1));
                    string selectedTerm2 = null;
                    int max = 0;

                    for (int i = 0; sb.Length > 0 && i < selectedTerm1.Length; i++)
                    {
                        string s2 = sb.ToString();
                        List<string> filteredTermList2;

                        if (dictionary.TryGetValue(s2[0], out filteredTermList2))
                        {
                            filteredTermList2.ForEach(delegate(string term)
                            {
                                if (s2.StartsWith(term, StringComparison.Ordinal) && term.Length > (selectedTerm2 == null ? 0 : selectedTerm2.Length))
                                {
                                    selectedTerm2 = term;
                                    max = i + selectedTerm2.Length;
                                }
                            });
                        }

                        sb.Remove(0, 1);
                    }

                    if (!String.IsNullOrEmpty(selectedTerm2) && selectedTerm1.Length < selectedTerm2.Length)
                    {
                        if (!selectedTermList.Contains(selectedTerm2))
                        {
                            selectedTermList.Add(selectedTerm2);
                        }

                        stringBuilder.Remove(0, max);
                    }
                    else
                    {
                        if (!selectedTermList.Contains(selectedTerm1))
                        {
                            selectedTermList.Add(selectedTerm1);
                        }

                        stringBuilder.Remove(0, selectedTerm1.Length);
                    }
                }
            }

            return selectedTermList;
        }

        private IEnumerable<T> Shuffle<T>(IEnumerable<T> collection)
        {
            // Fisher-Yates algorithm
            T[] array = collection.ToArray();
            int n = array.Length; // The number of items left to shuffle (loop invariant).
            Random r = new Random(Environment.TickCount);
            
            while (n > 1)
            {
                int k = r.Next(n); // 0 <= k < n.

                n--; // n is now the last pertinent index;
                T temp = array[n]; // swap list[n] with list[k] (does nothing if k == n).
                array[n] = array[k];
                array[k] = temp;
            }

            return array;
        }

        private string BuildSelectStatement(string query)
        {
            StringBuilder stringBuilder = new StringBuilder("SELECT Resource, Title, Description, Author, Created, Modified FROM Entry ");

            if (query.Length > 0)
            {
                string[] words = Regex.Split(query, @"\s", RegexOptions.CultureInvariant);
            
                for (int i = 0; i < words.Length; i++)
                {
                    if (i > 0)
                    {
                        stringBuilder.Append("AND ");
                    }
                    else
                    {
                        stringBuilder.Append("WHERE ");
                    }

                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "(Title LIKE '%{0}%' OR Description LIKE '%{0}%' OR Author LIKE '%{0}%') ", words[i]);
                }
            }

            stringBuilder.Append("ORDER BY Modified DESC");

            return stringBuilder.ToString();
        }

        private string BuildSelectStatement(int top)
        {
            return String.Format(CultureInfo.InvariantCulture, "SELECT TOP ({0}) Resource, Title, Description, Author, Created, Modified FROM Entry ORDER BY Modified DESC", top.ToString(CultureInfo.InvariantCulture));
        }

        private string BuildSelectStatement(DateTime fromDateTime, DateTime toDateTime)
        {
            return String.Format(CultureInfo.InvariantCulture, "SELECT Resource, Title, Description, Author, Created, Modified FROM Entry WHERE Modified > '{0}' AND Modified <= '{1}' ORDER BY Modified DESC", fromDateTime.ToString("G", DateTimeFormatInfo.InvariantInfo), toDateTime.ToString("G", DateTimeFormatInfo.InvariantInfo));
        }
	}
}