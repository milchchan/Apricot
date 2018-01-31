using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Apricot
{
    public class Sprite
    {
        private string path = null;
        private Point location = new Point(0, 0);
        private Size size = Size.Empty;
        private double opacity = 1;

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

        public double Opacity
        {
            get
            {
                return this.opacity;
            }
            set
            {
                this.opacity = value;
            }
        }

        public Sprite(string path)
        {
            this.path = path;
        }
    }
}