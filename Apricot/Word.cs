using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Apricot
{
    [DataContract(Namespace = "")]
    [KnownType(typeof(AttributeCollection<string>))]
    public class Word
    {
        private string name = null;
        private AttributeCollection<string> attributeCollection = null;

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

        public bool HasAttributes
        {
            get
            {
                return this.attributeCollection.Count > 0;
            }
        }

        [DataMember(Order = 1)]
        public AttributeCollection<string> Attributes
        {
            get
            {
                return this.attributeCollection;
            }
            set
            {
                this.attributeCollection = value;
            }
        }

        public Word()
        {
            this.attributeCollection = new AttributeCollection<string>();
        }

        [CollectionDataContract(Name = "Attributes", Namespace = "", ItemName = "string")]
        public class AttributeCollection<T> : ICollection<T>
        {
            private List<T> attributeList = null;

            public int Count
            {
                get
                {
                    return this.attributeList.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((ICollection<T>)this.attributeList).IsReadOnly;
                }
            }

            public AttributeCollection()
            {
                this.attributeList = new List<T>();
            }

            public void Add(T item)
            {
                this.attributeList.Add(item);
            }

            public void Clear()
            {
                this.attributeList.Clear();
            }

            public bool Contains(T item)
            {
                return this.attributeList.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                this.attributeList.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this.attributeList.GetEnumerator();
            }

            public bool Remove(T item)
            {
                return this.attributeList.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.attributeList.GetEnumerator();
            }
        }
    }
}
