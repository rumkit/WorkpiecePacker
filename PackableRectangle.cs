using System;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Packer
{
    public class PackableRectangle
    {
        private readonly Rectangle _rectangle;
        
        public PackableRectangle(Rectangle rectangle)
        {
            _rectangle = rectangle;
        }

        public bool IsPlaced { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public double Width { get { return _rectangle.Width; } }
        public double Height { get { return _rectangle.Height; } }

        public void Rotate()
        {
            var width = _rectangle.Width;
            _rectangle.Width = _rectangle.Height;
            _rectangle.Height = width;
        }

        public void PutOnCanvas(Canvas canvas)
        {
            Canvas.SetLeft(_rectangle,X);
            Canvas.SetTop(_rectangle, Y);
            canvas.Children.Add(_rectangle);
        }

        public bool Fit(Area area)
        {
            return ((this.Width <= area.Width) && (this.Height <= area.Height));
        }

        public override string ToString()
        {
            return String.Format("Rectangle H:{0}, W:{1}, X:{2}, Y:{3}", Height, Width, X, Y);
        }
    }
}
