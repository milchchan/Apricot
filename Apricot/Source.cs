using System;
using System.Runtime.Serialization;

namespace Apricot
{
    [DataContract(Namespace = "")]
    public class Source
    {
        private string name = null;
        private Uri location = null;

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

        [DataMember(Order = 1)]
        public Uri Location
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

        public Source(Uri uri)
        {
            this.location = uri;
        }

        public Source(string name, Uri location)
        {
            this.name = name;
            this.location = location;
        }
    }
}
