using System;
using System.Collections.ObjectModel;

namespace Apricot
{
    public class Message : System.Collections.IEnumerable
    {
        private System.Collections.ArrayList inlineList = null;
        private double speed = 25;
        private TimeSpan duration = TimeSpan.FromSeconds(5);
        private Collection<Entry> attachedEntryCollection = null;

        public string Text
        {
            get
            {
                System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

                foreach (object o in this.inlineList)
                {
                    string s = o as string;

                    if (s == null)
                    {
                        Entry entry = o as Entry;

                        if (entry == null)
                        {
                            stringBuilder.Append(o.ToString());
                        }
                        else
                        {
                            stringBuilder.Append(entry.Title);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(s);
                    }
                }

                return stringBuilder.ToString();
            }
        }

        public double Speed
        {
            get
            {
                return this.speed;
            }
            set
            {
                this.speed = value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return this.duration;
            }
            set
            {
                this.duration = value;
            }
        }

        public bool HasAttachments
        {
            get
            {
                return this.attachedEntryCollection.Count > 0;
            }
        }

        public Collection<Entry> Attachments
        {
            get
            {
                return this.attachedEntryCollection;
            }
            set
            {
                this.attachedEntryCollection = value;
            }
        }

        public Message()
        {
            this.inlineList = new System.Collections.ArrayList();
            this.attachedEntryCollection = new Collection<Entry>();
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return this.inlineList.GetEnumerator();
        }

        public void Add(object o)
        {
            this.inlineList.Add(o);
        }

        public void Remove(object o)
        {
            this.inlineList.Remove(o);
        }

        public void Clear()
        {
            this.inlineList.Clear();
        }
    }
}
