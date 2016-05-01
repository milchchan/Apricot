using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Apricot
{
    public class Character
    {
        private string name = null;
        private Point origin = new Point(0, 0);
        private Point baseLocation = new Point(0, 0);
        private Point location = new Point(0, 0);
        private Size size = new Size(0, 0);
        private bool mirror = false;
        private Collection<string> typeCollection = null;
        private string script = null;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Point Origin
        {
            get
            {
                return this.origin;
            }
            set
            {
                this.origin = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Point BaseLocation
        {
            get
            {
                return this.baseLocation;
            }
            set
            {
                this.baseLocation = value;
            }
        }

        public Point Location
        {
            get
            {
                return this.location;
            }
            set
            {
                this.location = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Size Size
        {
            get
            {
                return this.size;
            }
            set
            {
                this.size = value;
            }
        }

        public bool Mirror
        {
            get
            {
                return this.mirror;
            }
            set
            {
                this.mirror = value;
            }
        }

        public bool HasTypes
        {
            get
            {
                if (this.typeCollection.Count == 0)
                {
                    return false;
                }

                return true;
            }
        }

        public Collection<string> Types
        {
            get
            {
                return this.typeCollection;
            }
            set
            {
                this.typeCollection = value;
            }
        }

        public string Script
        {
            get
            {
                return this.script;
            }
            set
            {
                this.script = value;
            }
        }

        public Character()
        {
            this.typeCollection = new Collection<string>();
        }
    }
}
