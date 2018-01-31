using System;

namespace Apricot
{
    public class Sound
    {
        private string path = null;

        public string Path
        {
            get
            {
                return this.path;
            }
            set
            {
                this.path = value;
            }
        }

        public Sound(string path)
        {
            this.path = path;
        }
    }
}
