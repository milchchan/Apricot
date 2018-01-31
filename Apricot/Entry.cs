using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace Apricot
{
    public class Entry : ICloneable
    {
        private bool isEnabled = true;
        private bool isReadOnly = true;
        private Uri resource = null;
        private string title = null;
        private string description = null;
        private string author = null;
        private DateTime createdDateTime = new DateTime(0);
        private DateTime modifiedDateTime = new DateTime(0);
        private Uri imageUri = null;
        private Queue<Uri> imageUriQueue = null;
        private Nullable<double> score = null;
        private Collection<string> tagCollection = null;
        private Collection<Entry> similarEntryCollection = null;

        public bool Enabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                this.isEnabled = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return this.isReadOnly;
            }
            set
            {
                this.isReadOnly = value;
            }
        }

        public Uri Resource
        {
            get
            {
                return this.resource;
            }
            set
            {
                this.resource = value;
            }
        }

        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = value;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
                this.imageUriQueue = null;
            }
        }

        public string Author
        {
            get
            {
                return this.author;
            }
            set
            {
                this.author = value;
            }
        }

        public DateTime Created
        {
            get
            {
                return this.createdDateTime;
            }
            set
            {
                this.createdDateTime = value;
            }
        }

        public DateTime Modified
        {
            get
            {
                return this.modifiedDateTime;
            }
            set
            {
                this.modifiedDateTime = value;
            }
        }

        public Uri Image
        {
            get
            {
                return this.imageUri;
            }
            set
            {
                this.imageUri = value;
            }
        }

        public Nullable<double> Score
        {
            get
            {
                return this.score;
            }
            set
            {
                this.score = value;
            }
        }

        public bool HasTags
        {
            get
            {
                return this.tagCollection.Count > 0;
            }
        }

        public Collection<string> Tags
        {
            get
            {
                return this.tagCollection;
            }
            set
            {
                this.tagCollection = value;
            }
        }

        public bool HasSimilarEntries
        {
            get
            {
                return this.similarEntryCollection.Count > 0;
            }
        }

        public Collection<Entry> SimilarEntries
        {
            get
            {
                return similarEntryCollection;
            }
            set
            {
                this.similarEntryCollection = value;
            }
        }

        public bool HasMultipleImages
        {
            get
            {
                return this.imageUriQueue != null && this.imageUriQueue.Count >= 2;
            }
        }

        public Entry()
        {
            this.tagCollection = new Collection<string>();
            this.similarEntryCollection = new Collection<Entry>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void NextImage()
        {
            if (this.imageUri == null)
            {
                if (this.description != null)
                {
                    this.imageUriQueue = new Queue<Uri>(GetImageUriList(this.description));
                }
            }

            if (this.imageUriQueue != null && this.imageUriQueue.Count > 0)
            {
                this.imageUri = this.imageUriQueue.Dequeue();
                this.imageUriQueue.Enqueue(this.imageUri);
            }
        }

        private List<Uri> GetImageUriList(string text)
        {
            List<Uri> uriList = new List<Uri>();

            for (Match match = Regex.Match(text, "img.*?src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline); match.Success; match = match.NextMatch())
            {
                if (match.Groups[1].Value.Length > 0)
                {
                    Uri uri;

                    if (Uri.TryCreate(match.Groups[1].Value, UriKind.RelativeOrAbsolute, out uri) && !uriList.Contains(uri))
                    {
                        uriList.Add(uri);
                    }
                }
            }

            return uriList;
        }
    }
}
