using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Apricot
{
    [DataContract(Namespace = "")]
    [KnownType(typeof(Point<double>))]
    [KnownType(typeof(TypeCollection<string>))]
    public class Character
    {
        private string name = null;
        private System.Windows.Point origin = new System.Windows.Point(0, 0);
        private System.Windows.Point baseLocation = new System.Windows.Point(0, 0);
        private Point<double> location = new Point<double>(0, 0);
        private System.Windows.Size size = new System.Windows.Size(0, 0);
        private bool mirror = false;
        private TypeCollection<string> typeCollection = null;
        private int likes = 0;
        private string script = null;

        [DataMember(Order = 0)]
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

        public System.Windows.Point Origin
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

        public System.Windows.Point BaseLocation
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

        [DataMember(Order = 1)]
        public Point<double> Location
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

        public System.Windows.Size Size
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

        [DataMember(Order = 2)]
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
                return this.typeCollection.Count > 0;
            }
        }

        [DataMember(Order = 3)]
        public TypeCollection<string> Types
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

        [DataMember(Order = 4)]
        public int Likes
        {
            get
            {
                return this.likes;
            }
            set
            {
                this.likes = value;
            }
        }

        [DataMember(Order = 5)]
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
            this.typeCollection = new TypeCollection<string>();
        }

        [DataContract(Namespace = "")]
        public struct Point<T>
        {
            private T x;
            private T y;

            [DataMember(Order = 0)]
            public T X
            {
                get
                {
                    return this.x;
                }
                set
                {
                    this.x = value;
                }
            }

            [DataMember(Order = 1)]
            public T Y
            {
                get
                {
                    return this.y;
                }
                set
                {
                    this.y = value;
                }
            }

            public Point(T x, T y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [CollectionDataContract(Name = "Types", Namespace = "", ItemName = "string")]
        public class TypeCollection<T> : ICollection<T>
        {
            private List<T> typeList = null;

            public int Count
            {
                get
                {
                    return this.typeList.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((ICollection<T>)this.typeList).IsReadOnly;
                }
            }

            public TypeCollection()
            {
                this.typeList = new List<T>();
            }

            public void Add(T item)
            {
                this.typeList.Add(item);
            }

            public void Clear()
            {
                this.typeList.Clear();
            }

            public bool Contains(T item)
            {
                return this.typeList.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                this.typeList.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this.typeList.GetEnumerator();
            }

            public bool Remove(T item)
            {
                return this.typeList.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.typeList.GetEnumerator();
            }
        }
    }
}
