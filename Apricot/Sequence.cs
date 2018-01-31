using System;
using System.Collections;

namespace Apricot
{
    public class Sequence : IEnumerable
    {
        private string name = null;
        private string owner = null;
        private string state = null;
        private ArrayList segmentList = null;

        public object this[int index]
        {
            get
            {
                return this.segmentList[index];
            }
            set
            {
                this.segmentList[index] = value;
            }
        }

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

        public string Owner
        {
            get
            {
                return this.owner;
            }
            set
            {
                this.owner = value;
            }
        }

        public string State
        {
            get
            {
                return this.state;
            }
            set
            {
                this.state = value;
            }
        }

        public Sequence()
        {
            this.segmentList = new ArrayList();
        }

        public IEnumerator GetEnumerator()
        {
            return this.segmentList.GetEnumerator();
        }

        public bool Any()
        {
            return this.segmentList.Count > 0;
        }

        public void Add(object o)
        {
            this.segmentList.Add(o);
        }

        public void Remove(object o)
        {
            this.segmentList.Remove(o);
        }

        public void Clear()
        {
            this.segmentList.Clear();
        }
    }
}