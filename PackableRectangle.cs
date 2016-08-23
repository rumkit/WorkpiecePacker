using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
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

        public double Width { get { return _rectangle.Width + 2*Allowance; } }
        public double Height { get { return _rectangle.Height + 2*Allowance; } }

        public double Allowance { get; set; }

        public Brush Background => _rectangle.Fill;
        

        public void Rotate()
        {
            var width = _rectangle.Width;
            _rectangle.Width = _rectangle.Height;
            _rectangle.Height = width;
        }


        // TODO: вместо этого будем писать шаблон для коллекции
        public void PutOnCanvas(Canvas canvas)
        {
            Canvas.SetLeft(_rectangle, X);
            Canvas.SetTop(_rectangle, Y);
            canvas.Children.Add(_rectangle);
        }


        // 
        public bool Fit(Area area)
        {
            return ((this.Width <= area.Width) && (this.Height <= area.Height));
        }

        // Вовзращает экземпляр из строки вида [длина]х[ширина]х[кол-во]
        public static IEnumerable<PackableRectangle> FromString(string defenition)
        {
            var rectangleDimensions = defenition.Split(new[] { "x", "X", "х", "Х" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Int32.Parse(rectangleDimensions[2]); i++)
            {
                var baseRectangle = new Rectangle()
                {
                    Height = Int32.Parse(rectangleDimensions[0]),
                    Width = Int32.Parse(rectangleDimensions[1])
                };
                yield return new PackableRectangle(baseRectangle);
            }

        }

        public override string ToString()
        {
            //return $"Rectangle H:{Height}, W:{Width}, X:{X}, Y:{Y}";
            //return $"{_rectangle.Width}X{_rectangle.Height}\n({Width}x{Height})";
            return $"{_rectangle.Width}X{_rectangle.Height}";
        }
    }
}
