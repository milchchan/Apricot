using System;
using System.Collections.ObjectModel;

namespace Apricot
{
    public class Motion : ICloneable
    {
        private bool isRepeat = false;
        private double frameRate = 60;
        private int zIndex = 0;
        private String type = null;
        private int framePosition = 0;
        private Collection<Sprite> spriteCollection = null;

        public Sprite this[int index]
        {
            get
            {
                return this.spriteCollection[index];
            }
            set
            {
                this.spriteCollection[index] = value;
            }
        }

        public bool Repeats
        {
            get
            {
                return this.isRepeat;
            }
            set
            {
                this.isRepeat = value;
            }
        }

        public double FrameRate
        {
            get
            {
                return this.frameRate;
            }
            set
            {
                this.frameRate = value;
            }
        }

        public int ZIndex
        {
            get
            {
                return this.zIndex;
            }
            set
            {
                this.zIndex = value;
            }
        }

        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }

        public int Position
        {
            get
            {
                return this.framePosition;
            }
            set
            {
                this.framePosition = value;
            }
        }

        public bool HasFrames
        {
            get
            {
                return this.spriteCollection.Count > 0;
            }
        }
        
        public Sprite Current
        {
            get
            {
                return this.spriteCollection[this.framePosition];
            }
        }
        
        public Collection<Sprite> Sprites
        {
            get
            {
                return this.spriteCollection;
            }
            set
            {
                this.spriteCollection = value;
            }
        }

        public Motion()
        {
            this.spriteCollection = new Collection<Sprite>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Reset()
        {
            this.framePosition = 0;
        }

        public bool Next()
        {
            this.framePosition++;

            if (this.framePosition < this.spriteCollection.Count)
            {
                return true;
            }
            else
            {
                if (this.spriteCollection.Count > 0)
                {
                    this.framePosition = this.spriteCollection.Count - 1;
                }
                else
                {
                    this.framePosition = 0;
                }
            }

            return false;
        }

        public bool Previous()
        {
            this.framePosition--;

            if (this.framePosition >= 0)
            {
                return true;
            }
            else
            {
                this.framePosition = 0;
            }

            return false;
        }
    }
}
