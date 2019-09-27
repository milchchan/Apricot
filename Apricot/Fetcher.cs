using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Apricot
{
    public class Fetcher
    {
        private Nullable<int> timeout = null;
        private string userAgent = null;
        private Collection<Uri> locationList = null;
        private List<Tuple<string, Uri, IEnumerable<Entry>>> feedList = null;

        public Nullable<int> Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return this.userAgent;
            }
            set
            {
                this.userAgent = value;
            }
        }

        public Collection<Uri> Locations
        {
            get
            {
                return this.locationList;
            }
        }

        public IEnumerable<Tuple<string, Uri, IEnumerable<Entry>>> Feeds
        {
            get
            {
                return this.feedList;
            }
        }

        public Fetcher()
        {
            this.locationList = new Collection<Uri>();
            this.feedList = new List<Tuple<string, Uri, IEnumerable<Entry>>>();
        }

        public int Collect()
        {
            Queue<Uri> uriQueue = new Queue<Uri>(this.locationList);
            Queue<Task<Tuple<Uri, Tuple<string, List<Entry>>>>> taskQueue = new Queue<Task<Tuple<Uri, Tuple<string, List<Entry>>>>>();
            List<Tuple<string, List<Entry>>> feedList = new List<Tuple<string, List<Entry>>>();
            int successfulRow = 0;

            this.feedList.Clear();
            
            while (uriQueue.Count > 0 || taskQueue.Count > 0)
            {
                while (uriQueue.Count > 0 && taskQueue.Count < 2 * Environment.ProcessorCount)
                {
                    WebRequest webRequest = WebRequest.Create(uriQueue.Dequeue());
                    Task<Tuple<Uri, Tuple<string, List<Entry>>>> task = new Task<Tuple<Uri, Tuple<string, List<Entry>>>>(delegate (object state)
                    {
                        WebRequest request = (WebRequest)state;
                        WebResponse response = null;
                        Stream s = null;
                        BufferedStream bs = null;

                        try
                        {
                            response = request.GetResponse();
                            s = response.GetResponseStream();
                            bs = new BufferedStream(s);
                            s = null;

                            XmlDocument xmlDocument = new XmlDocument();

                            xmlDocument.Load(bs);
                            xmlDocument.Normalize();

                            if (xmlDocument.DocumentElement.NamespaceURI.Equals("http://www.w3.org/1999/02/22-rdf-syntax-ns#") && xmlDocument.DocumentElement.LocalName.Equals("RDF"))
                            {
                                return Tuple.Create<Uri, Tuple<string, List<Entry>>>(request.RequestUri, ParseRss10(xmlDocument.DocumentElement));
                            }
                            else if (xmlDocument.DocumentElement.Name.Equals("rss"))
                            {
                                foreach (XmlAttribute xmlAttribute in xmlDocument.DocumentElement.Attributes)
                                {
                                    if (xmlAttribute.Name.Equals("version"))
                                    {
                                        if (xmlAttribute.Value.Equals("2.0"))
                                        {
                                            return Tuple.Create<Uri, Tuple<string, List<Entry>>>(request.RequestUri, ParseRss20(xmlDocument.DocumentElement));
                                        }

                                        break;
                                    }
                                }
                            }
                            else if (xmlDocument.DocumentElement.NamespaceURI.Equals("http://www.w3.org/2005/Atom") && xmlDocument.DocumentElement.LocalName.Equals("feed"))
                            {
                                return Tuple.Create<Uri, Tuple<string, List<Entry>>>(request.RequestUri, ParseAtom10(xmlDocument.DocumentElement));
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

                        return null;
                    }, webRequest, TaskCreationOptions.LongRunning);

                    if (this.timeout.HasValue)
                    {
                        webRequest.Timeout = this.timeout.Value;
                    }

                    if (this.userAgent != null)
                    {
                        HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;

                        if (httpWebRequest != null)
                        {
                            httpWebRequest.UserAgent = this.userAgent;
                        }
                    }

                    taskQueue.Enqueue(task);
                    task.Start();
                }

                Task<Tuple<Uri, Tuple<string, List<Entry>>>>[] tasks = taskQueue.ToArray();
                int index = Task<IEnumerable<Entry>>.WaitAny(tasks);

                taskQueue.Clear();

                for (int i = 0; i < tasks.Length; i++)
                {
                    if (index == i)
                    {
                        if (tasks[i].Exception == null && tasks[i].Result != null)
                        {
                            this.feedList.Add(Tuple.Create<string, Uri, IEnumerable<Entry>>(tasks[i].Result.Item2.Item1, tasks[i].Result.Item1, tasks[i].Result.Item2.Item2));
                        }
                    }
                    else
                    {
                        taskQueue.Enqueue(tasks[i]);
                    }
                }
            }

            foreach (System.Configuration.ConnectionStringSettings settings in System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).ConnectionStrings.ConnectionStrings)
            {
                DbProviderFactory factory = DbProviderFactories.GetFactory(settings.ProviderName);

                using (IDbConnection connection = factory.CreateConnection())
                {
                    connection.ConnectionString = settings.ConnectionString;
                    connection.Open();

                    this.feedList.ForEach(delegate (Tuple<string, Uri, IEnumerable<Entry>> feed)
                    {
                        foreach (Entry entry in feed.Item3)
                        {
                            if (entry.Resource != null)
                            {
                                string sql = null;

                                using (IDbCommand command = factory.CreateCommand())
                                {
                                    command.Connection = connection;
                                    command.CommandText = BuildSelectStatement(entry.Resource);

                                    if ((int)command.ExecuteScalar() == 0)
                                    {
                                        sql = BuildInsertStatement(entry);
                                    }
                                    else if (entry.Created < entry.Modified)
                                    {
                                        sql = BuildUpdateStatement(entry);
                                    }
                                }

                                if (sql != null)
                                {
                                    using (IDbCommand c = factory.CreateCommand())
                                    {
                                        c.Connection = connection;
                                        c.CommandText = sql;

                                        IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                                        c.Transaction = transaction;

                                        try
                                        {
                                            successfulRow += c.ExecuteNonQuery();

                                            transaction.Commit();
                                        }
                                        catch
                                        {
                                            transaction.Rollback();
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            return successfulRow;
        }

        private string BuildSelectStatement(Uri uri)
        {
            return String.Format(CultureInfo.InvariantCulture, "SELECT COUNT(Resource) FROM Entry WHERE Resource = '{0}'", uri.ToString().Replace("'", "''"));
        }

        private string BuildInsertStatement(Entry entry)
        {
            StringBuilder insertSqlBuilder = new StringBuilder("INSERT INTO Entry(Resource, Title, Description, Author, Created, Modified) VALUES(");

            if (entry.Resource == null)
            {
                insertSqlBuilder.Append("NULL, ");
            }
            else
            {
                insertSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', ", entry.Resource.ToString().Replace("'", "''"));
            }

            if (entry.Title == null)
            {
                insertSqlBuilder.Append("NULL, ");
            }
            else
            {
                insertSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', ", entry.Title.Replace("'", "''"));
            }

            if (entry.Description == null)
            {
                insertSqlBuilder.Append("NULL, ");
            }
            else
            {
                insertSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', ", entry.Description.Replace("'", "''"));
            }

            if (entry.Author == null)
            {
                insertSqlBuilder.Append("NULL, ");
            }
            else
            {
                insertSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', ", entry.Author.Replace("'", "''"));
            }

            insertSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', '{1}')", entry.Created.ToString("G", DateTimeFormatInfo.InvariantInfo), entry.Modified.ToString("G", DateTimeFormatInfo.InvariantInfo));

            return insertSqlBuilder.ToString();
        }

        private string BuildUpdateStatement(Entry entry)
        {
            StringBuilder updateSqlBuilder = new StringBuilder("UPDATE Entry SET ");

            if (entry.Title != null)
            {
                updateSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "Title = '{0}', ", entry.Title.Replace("'", "''"));
            }

            if (entry.Description != null)
            {
                updateSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "Description = '{0}', ", entry.Description.Replace("'", "''"));
            }

            if (entry.Author != null)
            {
                updateSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "Author = '{0}', ", entry.Author.Replace("'", "''"));
            }

            updateSqlBuilder.AppendFormat(CultureInfo.InvariantCulture, "Modified = '{0}' WHERE Resource = '{1}' AND '{2}' > Modified", entry.Modified.ToString("G", DateTimeFormatInfo.InvariantInfo), entry.Resource.ToString().Replace("'", "''"), entry.Modified.ToString("G", DateTimeFormatInfo.InvariantInfo));

            return updateSqlBuilder.ToString();
        }

        private Tuple<string, List<Entry>> ParseRss10(XmlNode xmlRootNode)
        {
            const string namespaceURI = "http://purl.org/rss/1.0/";
            List<Entry> entryList = new List<Entry>();
            string title = null;
            string dcCreator = null;
            DateTime dcDate = new DateTime(0);
            Uri imageUri = null;

            foreach (XmlNode childNode in xmlRootNode.ChildNodes)
            {
                if (childNode.NamespaceURI.Equals(namespaceURI))
                {
                    if (childNode.LocalName.Equals("channel"))
                    {
                        foreach (XmlNode xmlNode in childNode.ChildNodes)
                        {
                            if (xmlNode.NamespaceURI.Equals(namespaceURI) && xmlNode.LocalName.Equals("title"))
                            {
                                title = xmlNode.InnerText;
                            }
                            else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("creator"))
                            {
                                dcCreator = xmlNode.InnerText;
                            }
                            else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("date"))
                            {
                                DateTime dt;

                                if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                {
                                    dcDate = dt;
                                }
                            }
                        }
                    }
                    else if (childNode.LocalName.Equals("image"))
                    {
                        foreach (XmlNode xmlNode in childNode.ChildNodes)
                        {
                            if (xmlNode.NamespaceURI.Equals(namespaceURI) && xmlNode.LocalName.Equals("url"))
                            {
                                Uri uri;

                                if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                                {
                                    imageUri = uri;
                                }
                            }
                        }
                    }
                }
            }

            foreach (XmlNode childNode in xmlRootNode.ChildNodes)
            {
                if (childNode.NamespaceURI.Equals(namespaceURI) && childNode.LocalName.Equals("item"))
                {
                    Entry entry = new Entry();
                    string description = null;
                    string contentEncoded = null;

                    entry.Author = dcCreator;
                    entry.Created = entry.Modified = dcDate;
                    entry.Image = imageUri;

                    foreach (XmlNode xmlNode in childNode.ChildNodes)
                    {
                        if (xmlNode.NamespaceURI.Equals(namespaceURI) && xmlNode.LocalName.Equals("title"))
                        {
                            entry.Title = xmlNode.InnerText;
                        }
                        else if (xmlNode.NamespaceURI.Equals(namespaceURI) && xmlNode.LocalName.Equals("link"))
                        {
                            Uri uri;

                            if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                            {
                                entry.Resource = uri;
                            }
                        }
                        else if (xmlNode.NamespaceURI.Equals(namespaceURI) && xmlNode.LocalName.Equals("description"))
                        {
                            description = xmlNode.InnerText;
                        }
                        else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("creator"))
                        {
                            entry.Author = xmlNode.InnerText;
                        }
                        else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("date"))
                        {
                            DateTime dt;

                            if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                entry.Created = entry.Modified = dt;
                            }
                        }
                        else if (xmlNode.NamespaceURI.Equals("http://purl.org/rss/1.0/modules/content/") && xmlNode.LocalName.Equals("encoded"))
                        {
                            contentEncoded = xmlNode.InnerText;
                        }
                    }

                    if (contentEncoded == null)
                    {
                        entry.Description = description;
                    }
                    else
                    {
                        entry.Description = contentEncoded;
                    }

                    if (entry.Created.Ticks == 0)
                    {
                        if (entry.Modified.Ticks == 0)
                        {
                            entry.Created = DateTime.Today;
                        }
                        else
                        {
                            entry.Created = entry.Modified;
                        }
                    }

                    if (entry.Created > entry.Modified)
                    {
                        entry.Modified = entry.Created;
                    }

                    entryList.Add(entry);
                }
            }

            return Tuple.Create<string, List<Entry>>(title, entryList);
        }

        private Tuple<string, List<Entry>> ParseRss20(XmlNode xmlRootNode)
        {
            List<Entry> entryList = new List<Entry>();
            string title = null;
            string webMaster = null;
            DateTime pubDate = new DateTime(0);
            Uri imageUri = null;

            foreach (XmlNode childNode in xmlRootNode.ChildNodes)
            {
                if (childNode.Name.Equals("channel"))
                {
                    foreach (XmlNode xmlNode in childNode.ChildNodes)
                    {
                        if (xmlNode.Name.Equals("title"))
                        {
                            title = xmlNode.InnerText;
                        }
                        else if (xmlNode.Name.Equals("webMaster") || xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("creator"))
                        {
                            webMaster = xmlNode.InnerText;
                        }
                        else if (xmlNode.Name.Equals("pubDate"))
                        {
                            try
                            {
                                pubDate = ParseRfc822(xmlNode.InnerText);
                            }
                            catch
                            {
                                DateTime dt;

                                if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                {
                                    pubDate = dt;
                                }
                            }
                        }
                        else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("date"))
                        {
                            DateTime dt;

                            if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                pubDate = dt;
                            }
                        }
                    }
                }
                else if (childNode.Name.Equals("image"))
                {
                    foreach (XmlNode xmlNode in childNode.ChildNodes)
                    {
                        if (xmlNode.Name.Equals("url"))
                        {
                            Uri uri;

                            if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                            {
                                imageUri = uri;
                            }
                        }
                    }
                }
            }

            foreach (XmlNode childNode in xmlRootNode.ChildNodes)
            {
                if (childNode.Name.Equals("channel"))
                {
                    foreach (XmlNode itemNode in childNode.ChildNodes)
                    {
                        if (itemNode.Name.Equals("item"))
                        {
                            Entry entry = new Entry();

                            entry.Author = webMaster;
                            entry.Created = entry.Modified = pubDate;
                            entry.Image = imageUri;

                            foreach (XmlNode xmlNode in itemNode.ChildNodes)
                            {
                                if (xmlNode.Name.Equals("title"))
                                {
                                    entry.Title = xmlNode.InnerText;
                                }
                                else if (xmlNode.Name.Equals("link"))
                                {
                                    Uri uri;

                                    if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                                    {
                                        entry.Resource = uri;
                                    }
                                }
                                else if (xmlNode.Name.Equals("description"))
                                {
                                    entry.Description = xmlNode.InnerText;
                                }
                                else if (xmlNode.Name.Equals("pubDate"))
                                {
                                    try
                                    {
                                        entry.Created = entry.Modified = ParseRfc822(xmlNode.InnerText);
                                    }
                                    catch
                                    {
                                        DateTime dt;

                                        if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                        {
                                            entry.Created = entry.Modified = dt;
                                        }
                                    }
                                }
                                else if (xmlNode.NamespaceURI.Equals("http://purl.org/dc/elements/1.1/") && xmlNode.LocalName.Equals("date"))
                                {
                                    DateTime dt;

                                    if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                    {
                                        entry.Created = entry.Modified = dt;
                                    }
                                }
                            }

                            if (entry.Created.Ticks == 0)
                            {
                                if (entry.Modified.Ticks == 0)
                                {
                                    entry.Created = DateTime.Today;
                                }
                                else
                                {
                                    entry.Created = entry.Modified;
                                }
                            }

                            if (entry.Created > entry.Modified)
                            {
                                entry.Modified = entry.Created;
                            }

                            entryList.Add(entry);
                        }
                    }
                }
            }

            return Tuple.Create<string, List<Entry>>(title, entryList);
        }

        private Tuple<string, List<Entry>> ParseAtom10(XmlNode xmlRootNode)
        {
            const string namespaceURI = "http://www.w3.org/2005/Atom";
            List<Entry> entryList = new List<Entry>();
            string title = null;
            string author = null;
            Uri icon = null;
            Uri logo = null;
            DateTime updated = new DateTime(0);

            foreach (XmlNode xmlNode in xmlRootNode.ChildNodes)
            {
                if (xmlNode.NamespaceURI.Equals(namespaceURI))
                {
                    if (xmlNode.LocalName.Equals("title"))
                    {
                        title = xmlNode.InnerText;
                    }
                    else if (xmlNode.LocalName.Equals("updated"))
                    {
                        DateTime dt;

                        if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            updated = dt;
                        }
                    }
                    else if (xmlNode.LocalName.Equals("author"))
                    {
                        foreach (XmlNode xmlChildNode in xmlNode.ChildNodes)
                        {
                            if (xmlChildNode.NamespaceURI.Equals(namespaceURI) && xmlChildNode.LocalName.Equals("name"))
                            {
                                author = xmlChildNode.InnerText;
                            }
                        }
                    }
                    else if (xmlNode.LocalName.Equals("icon"))
                    {
                        Uri uri;

                        if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                        {
                            icon = uri;
                        }
                    }
                    else if (xmlNode.LocalName.Equals("logo"))
                    {
                        Uri uri;

                        if (Uri.TryCreate(xmlNode.InnerText, UriKind.Absolute, out uri))
                        {
                            logo = uri;
                        }
                    }
                }
            }

            foreach (XmlNode xmlEntryNode in xmlRootNode.ChildNodes)
            {
                if (xmlEntryNode.NamespaceURI.Equals(namespaceURI) && xmlEntryNode.LocalName.Equals("entry"))
                {
                    string summary = null;
                    string content = null;
                    Entry entry = new Entry();

                    entry.Created = entry.Modified = updated;
                    entry.Author = author;

                    if (logo != null)
                    {
                        entry.Image = logo;
                    }
                    else if (icon != null)
                    {
                        entry.Image = icon;
                    }

                    foreach (XmlNode xmlNode in xmlEntryNode.ChildNodes)
                    {
                        if (xmlNode.NamespaceURI.Equals(namespaceURI))
                        {
                            if (xmlNode.LocalName.Equals("title"))
                            {
                                entry.Title = xmlNode.InnerText;
                            }
                            else if (xmlNode.LocalName.Equals("published") || xmlNode.LocalName.Equals("issued"))
                            {
                                DateTime dt;

                                if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                {
                                    entry.Created = dt;
                                }
                            }
                            else if (xmlNode.LocalName.Equals("updated") || xmlNode.LocalName.Equals("modified"))
                            {
                                DateTime dt;

                                if (DateTime.TryParse(xmlNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                {
                                    entry.Modified = dt;
                                }
                            }
                            else if (xmlNode.LocalName.Equals("link"))
                            {
                                string href = null;
                                string rel = null;

                                foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                                {
                                    if (xmlAttribute.Name.Equals("href"))
                                    {
                                        href = xmlAttribute.Value;
                                    }
                                    else if (xmlAttribute.Name.Equals("rel"))
                                    {
                                        rel = xmlAttribute.Value;
                                    }
                                }

                                if (href != null && (rel == null ? true : rel.Equals("alternate")))
                                {
                                    Uri uri;

                                    if (Uri.TryCreate(href, UriKind.Absolute, out uri))
                                    {
                                        entry.Resource = uri;
                                    }
                                }
                            }
                            else if (xmlNode.LocalName.Equals("author"))
                            {
                                foreach (XmlNode xmlChildNode in xmlNode.ChildNodes)
                                {
                                    if (xmlChildNode.NamespaceURI.Equals(namespaceURI) && xmlChildNode.LocalName.Equals("name"))
                                    {
                                        entry.Author = xmlChildNode.InnerText;
                                    }
                                }
                            }
                            else if (xmlNode.LocalName.Equals("summary"))
                            {
                                summary = xmlNode.InnerText;
                            }
                            else if (xmlNode.LocalName.Equals("content"))
                            {
                                content = xmlNode.InnerText;
                            }
                        }
                    }

                    if (content == null)
                    {
                        entry.Description = summary;
                    }
                    else
                    {
                        entry.Description = content;
                    }

                    if (entry.Created.Ticks == 0)
                    {
                        if (entry.Modified.Ticks == 0)
                        {
                            entry.Created = DateTime.Today;
                        }
                        else
                        {
                            entry.Created = entry.Modified;
                        }
                    }

                    if (entry.Created > entry.Modified)
                    {
                        entry.Modified = entry.Created;
                    }

                    entryList.Add(entry);
                }
            }

            return Tuple.Create<string, List<Entry>>(title, entryList);
        }

        private DateTime ParseRfc822(string s)
        {
            Match match = Regex.Match(s, @"(?<=\s)([+-][0-9]{4}|[A-Z]+)$", RegexOptions.CultureInvariant);

            if (match.Success)
            {
                DateTime dt = Convert.ToDateTime(s.Substring(0, match.Index), CultureInfo.InvariantCulture);

                if (s[match.Index] == '+')
                {
                    dt = dt.AddHours(-Convert.ToInt32(s.Substring(match.Index + 1, 2), CultureInfo.InvariantCulture));
                    dt = dt.AddMinutes(-Convert.ToInt32(s.Substring(match.Index + 3, 2), CultureInfo.InvariantCulture));
                }
                else if (s[match.Index] == '-')
                {
                    dt = dt.AddHours(Convert.ToInt32(s.Substring(match.Index + 1, 2), CultureInfo.InvariantCulture));
                    dt = dt.AddMinutes(Convert.ToInt32(s.Substring(match.Index + 3, 2), CultureInfo.InvariantCulture));
                }
                else
                {
                    switch (match.Value)
                    {
                        case "A":
                            dt = dt.AddHours(1);
                            break;
                        case "B":
                            dt = dt.AddHours(2);
                            break;
                        case "C":
                            dt = dt.AddHours(3);
                            break;
                        case "D":
                            dt = dt.AddHours(4);
                            break;
                        case "E":
                            dt = dt.AddHours(5);
                            break;
                        case "F":
                            dt = dt.AddHours(6);
                            break;
                        case "G":
                            dt = dt.AddHours(7);
                            break;
                        case "H":
                            dt = dt.AddHours(8);
                            break;
                        case "I":
                            dt = dt.AddHours(9);
                            break;
                        case "K":
                            dt = dt.AddHours(10);
                            break;
                        case "L":
                            dt = dt.AddHours(11);
                            break;
                        case "M":
                            dt = dt.AddHours(12);
                            break;
                        case "N":
                            dt = dt.AddHours(-1);
                            break;
                        case "O":
                            dt = dt.AddHours(-2);
                            break;
                        case "P":
                            dt = dt.AddHours(-3);
                            break;
                        case "Q":
                            dt = dt.AddHours(-4);
                            break;
                        case "R":
                            dt = dt.AddHours(-5);
                            break;
                        case "S":
                            dt = dt.AddHours(-6);
                            break;
                        case "T":
                            dt = dt.AddHours(-7);
                            break;
                        case "U":
                            dt = dt.AddHours(-8);
                            break;
                        case "V":
                            dt = dt.AddHours(-9);
                            break;
                        case "W":
                            dt = dt.AddHours(-10);
                            break;
                        case "X":
                            dt = dt.AddHours(-11);
                            break;
                        case "Y":
                            dt = dt.AddHours(-12);
                            break;
                        case "Z":
                        case "UT":
                        case "GMT":
                            break;
                        case "EST":
                            dt = dt.AddHours(5);
                            break;
                        case "EDT":
                            dt = dt.AddHours(4);
                            break;
                        case "CST":
                            dt = dt.AddHours(6);
                            break;
                        case "CDT":
                            dt = dt.AddHours(5);
                            break;
                        case "MST":
                            dt = dt.AddHours(7);
                            break;
                        case "MDT":
                            dt = dt.AddHours(6);
                            break;
                        case "PST":
                            dt = dt.AddHours(8);
                            break;
                        case "PDT":
                            dt = dt.AddHours(7);
                            break;
                        default:
                            return dt;
                    }
                }

                return dt.Add(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
            }

            return Convert.ToDateTime(s.Substring(0, s.Length), CultureInfo.InvariantCulture);
        }
    }
}
