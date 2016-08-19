using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Packer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Rectangle> _rectangles = new List<Rectangle>();
        private List<PackableRectangle> _packableRectangles = new List<PackableRectangle>();

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            RectanglesTextBox.Text = "40x25x8\r\n40x50x4\r\n80x50x3";
            WidthTextBox.Text = "400";
            HeighTextBox.Text = "2000";
#endif

            _random = new Random(DateTime.Now.Millisecond);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            FillCollection(RectanglesTextBox.Text);
            ArrangeRectangles();
        }

        void FillCollection(string defenitionString)
        {
            _rectangles.Clear();
            _packableRectangles.Clear();
            foreach (var s in defenitionString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var rectParams = s.Split(new[] { "x", "X", "х", "Х" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < Int32.Parse(rectParams[2]); i++)
                    {
                        _rectangles.Add(new Rectangle()
                        {
                            Height = Int32.Parse(rectParams[0]),
                            Width = Int32.Parse(rectParams[1]),
                            Stroke = Brushes.Black,
                            StrokeThickness = 3,
                            Fill = GetRandomBrush()

                        });
                    }
                }
                catch (Exception exception)
                {
                    Debug.Print("Wrong rectangle params: " + exception.Message);
                }
            }
            foreach (var rectangle in _rectangles)
            {
                _packableRectangles.Add(new PackableRectangle(rectangle));
            }

            _packableRectangles.Sort(CompareRectanglesByArea);
        }

        private static int CompareRectanglesByArea(PackableRectangle a, PackableRectangle b)
        {
            var aArea = a.Width * a.Height;
            var bArea = b.Width * b.Height;
            return -aArea.CompareTo(bArea);
        }

        private static int CompareRectanglesByMax(PackableRectangle a, PackableRectangle b)
        {
            var aMax = Math.Max(a.Height, a.Width);
            var bMax = Math.Max(b.Height, b.Width);
            return -aMax.CompareTo(bMax);
        }

        private Random _random;
        private Brush GetRandomBrush()
        {
            var color = new Color
            {
                A = 100,
                R = (byte)_random.Next(0, 255),
                G = (byte)_random.Next(0, 255),
                B = (byte)_random.Next(0, 255)
            };
            return new SolidColorBrush(color);
        }

        private void ArrangeRectangles()
        {
            var totalLength = PlaceRectangle(new Area() { Height = PackingArea.Height, Width = PackingArea.Width, X = 0, Y = 0 },
                _packableRectangles);
            PackingArea.Items.Clear();
            var placedRectangles = _packableRectangles.Where((r) => r.IsPlaced);
            var placedRectanglesArray = placedRectangles as PackableRectangle[] ?? placedRectangles.ToArray();
            if (placedRectanglesArray.Count() < _packableRectangles.Count)
                MessageBox.Show("Не удалось разместить " + (_packableRectangles.Count - placedRectanglesArray.Count()) +
                                " прямоугольников");
            foreach (var packableRectangle in placedRectanglesArray)
            {
                PackingArea.Items.Add(packableRectangle);
            }

            DrawLengthIndicator(totalLength);
        }


        private void DrawLengthIndicator(double totalLength)
        {
           // LengthIndicatorCanvas.Children.Clear();
            var verticalIndicator = new Line() {Y2 = totalLength, Stroke = Brushes.Red, StrokeThickness = 4};
            var horizontalIndicator = new Line() { Y1 = totalLength, Y2 = totalLength, X2=PackingArea.ActualWidth + LengthIndicatorCanvas.ActualWidth, Stroke = Brushes.Red, StrokeThickness = 1 };
            Canvas.SetLeft(verticalIndicator,20);

           // LengthIndicatorCanvas.Children.Add(verticalIndicator);
            
           // ScrollViewerRootGrid.Children.
        }

        private double PlaceRectangle(Area area, IEnumerable<PackableRectangle> packableRectangles)
        {
            if (area.Width.CompareTo(0) == 0 || area.Height.CompareTo(0) == 0) return 0; // no more place left
            var rectangles = packableRectangles as PackableRectangle[] ?? packableRectangles.ToArray();
            if (!rectangles.Any()) return 0; // no more rectangles left
            double metrics = Int32.MaxValue;
            foreach (var packableRectangle in rectangles)
            {
                double heightMetrics1 = -1;
                double heightMetrics2 = -1;
                if (packableRectangle.Fit(area))
                {
                    var areas = SplitArea(area, packableRectangle);
                    var freeRectangles = new List<PackableRectangle>(rectangles);
                    freeRectangles.Remove(packableRectangle); //except current
                    foreach (var freeRectangle in freeRectangles)
                    {
                        freeRectangle.IsPlaced = false;
                    }
                    PlaceRectangle(areas[0], freeRectangles); //Right area placement
                    heightMetrics1 = PlaceRectangle(areas[1], freeRectangles.Where((r) => !r.IsPlaced)) + packableRectangle.Height;

                }
                packableRectangle.Rotate();
                if (packableRectangle.Fit(area))
                {
                    var areas = SplitArea(area, packableRectangle);
                    var freeRectangles = new List<PackableRectangle>(rectangles);
                    freeRectangles.Remove(packableRectangle); //except current
                    foreach (var freeRectangle in freeRectangles)
                    {
                        freeRectangle.IsPlaced = false;
                    }
                    PlaceRectangle(areas[0], freeRectangles); //Right area placement
                    heightMetrics2 = PlaceRectangle(areas[1], freeRectangles.Where((r) => !r.IsPlaced)) + packableRectangle.Height;

                }
                if (heightMetrics1 >= 0 || heightMetrics2 >= 0)
                {
                    if (heightMetrics2 < 0) metrics = heightMetrics1;
                    else if (heightMetrics1 < 0) metrics = heightMetrics2;
                    else metrics = Math.Min(heightMetrics2, heightMetrics1);
                    if (heightMetrics1 > 0 && (heightMetrics2 > heightMetrics1 || heightMetrics2 < 0))
                    {
                        packableRectangle.Rotate(); //rotate again =)
                    }
                    //finally place it
                    packableRectangle.IsPlaced = true;
                    packableRectangle.X = area.X;
                    packableRectangle.Y = area.Y;
                    //and recursively split again
                    var areas = SplitArea(area, packableRectangle);
                    var freeRectangles = new List<PackableRectangle>(rectangles);
                    freeRectangles.Remove(packableRectangle); //except current
                    foreach (var freeRectangle in freeRectangles)
                    {
                        freeRectangle.IsPlaced = false;
                    }
                    PlaceRectangle(areas[0], freeRectangles); //Right area placement
                    PlaceRectangle(areas[1], freeRectangles.Where((r) => !r.IsPlaced));
                    break;
                }
            }

            return metrics;
        }

        private Area[] SplitArea(Area baseArea, PackableRectangle rectangle)
        {
            var areas = new Area[]
            {
                new Area()
                {
                    X = baseArea.X + rectangle.Width,
                    Y = baseArea.Y,
                    Width = baseArea.Width - rectangle.Width,
                    Height = rectangle.Height
                },
                new Area()
                {
                    X = baseArea.X,
                    Y = baseArea.Y + rectangle.Height,
                    Width = baseArea.Width,
                    Height = baseArea.Height - rectangle.Height
                }
            };
            return areas;
        }

        private void ResizeCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            PackingArea.Width = double.Parse(WidthTextBox.Text);
            PackingArea.Height = double.Parse(HeighTextBox.Text);
        }


       
    }
}
