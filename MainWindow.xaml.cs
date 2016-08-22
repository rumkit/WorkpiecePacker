using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        private const double DefaultSheetWidth = 500;
        private const double DefaultSheetHeight = 1000;
        private const double DefaultSheetAllowance = 10;
        private const double DefaultWorkpieceAllowance = 2.5;

        private Random _random;
        private double _sheetAllowance = DefaultSheetAllowance;
        private double _workpieceAllowance = DefaultWorkpieceAllowance;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            RectanglesTextBox.Text = "30x40x11\r\n150x80x4\r\n251x110x3";
#endif
            // Установим значения по умолчанию
            WidthTextBox.Text = DefaultSheetWidth.ToString(CultureInfo.CurrentCulture);
            HeighTextBox.Text = DefaultSheetHeight.ToString(CultureInfo.CurrentCulture);
            SheetAllowance.Text = _sheetAllowance.ToString(CultureInfo.CurrentCulture);
            WorkPieceAllowance.Text = _workpieceAllowance.ToString(CultureInfo.CurrentCulture);


            _random = new Random(DateTime.Now.Millisecond);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Установим дефолтные габариты листа и значения припусков
            SetSheetSize();
            SetAllowances();
            
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            FillCollection(RectanglesTextBox.Text);
            ArrangeRectangles(PackingArea.ActualWidth, PackingArea.ActualHeight);
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
                            Height = Double.Parse(rectParams[0]),
                            Width = Double.Parse(rectParams[1]),
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
                _packableRectangles.Add(new PackableRectangle(rectangle) {Allowance = _workpieceAllowance});
            }

            _packableRectangles.Sort(CompareRectanglesByArea);
        }

        

        
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

        private void DrawLengthIndicator(double totalLength)
        {
            Canvas.SetTop(LengthIndicator, totalLength);
            LengthIndicatorLabel.Content = totalLength;
            LengthIndicator.Visibility = Visibility.Visible;

        }

        private void SetSheetSize(double width, double height)
        {
            PackingArea.Width = width;
            PackingArea.Height = height;
        }

        private void SetSheetSize()
        {
            try
            {
                SetSheetSize(double.Parse(WidthTextBox.Text), double.Parse(HeighTextBox.Text));
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка", "Неверные габариты листа", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResizeCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            SetSheetSize();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetAllowances();
        }

        private void SetAllowances()
        {
            try
            {
                _sheetAllowance = double.Parse(SheetAllowance.Text);
                _workpieceAllowance = double.Parse(WorkPieceAllowance.Text) / 2;
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка", "Неверные значения припусков", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Размещение заготовок на листе
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

        private void ArrangeRectangles(double areaWidth, double areaHeight)
        {
            var sheetMargin = _sheetAllowance - _workpieceAllowance;
            if (sheetMargin <= 0) sheetMargin = _workpieceAllowance;
            var totalLength = PlaceRectangle(new Area() { Height = areaHeight - 2 * sheetMargin, Width = areaWidth - 2 * sheetMargin, X = 0 + sheetMargin, Y = 0 + sheetMargin},
                _packableRectangles);
#error Неправильный пересчет результирующей длины
            totalLength += _sheetAllowance * 2 + _workpieceAllowance * 4;
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
                    var areas = area.Split(packableRectangle);
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
                    var areas = area.Split(packableRectangle);
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
                    var areas = area.Split(packableRectangle);
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

        #endregion

        private void ScrollView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LengthIndicator.Width = (sender as ScrollViewer).ActualWidth;
        }
    }
}
