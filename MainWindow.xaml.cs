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

        

        // Возвращает рандомную полупрозрачную кисть для расскраски прямоугольников
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

        // Отображает и устанавливает на необходимую высоту красную индикаторную линию
        private void DrawLengthIndicator(double totalLength)
        {
            Canvas.SetTop(LengthIndicator, totalLength);
            LengthIndicatorLabel.Content = totalLength;
            LengthIndicator.Visibility = Visibility.Visible;
        }

        // Устанавливает заданный размер листа на котором происходит размещение
        private void SetSheetSize(double width, double height)
        {
            PackingArea.Width = width;
            PackingArea.Height = height;
        }


        // Устанавливает размер листа, указанный в текстбоксах
        private void SetSheetSize()
        {
            try
            {
                SetSheetSize(double.Parse(WidthTextBox.Text), double.Parse(HeighTextBox.Text));
            }
            catch (Exception)
            {
                MessageBox.Show("Неверные габариты листа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ResizeCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            SetSheetSize();
        }

        private void ApplyAllowancesButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllowances();
        }

        // Сохраняет значения припусков указанные в текстбоксах
        private void SetAllowances()
        {
            try
            {
                _sheetAllowance = double.Parse(SheetAllowance.Text);
                _workpieceAllowance = double.Parse(WorkPieceAllowance.Text) / 2;
            }
            catch (Exception)
            {
                MessageBox.Show("Неверные значения припусков", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            totalLength += Math.Abs(_sheetAllowance * 2 - _workpieceAllowance * 2);
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
            // Если больше нет свободного для размещения места
            if (area.Width.CompareTo(0) == 0 || area.Height.CompareTo(0) == 0)
                return 0;
            var rectangles = packableRectangles as PackableRectangle[] ?? packableRectangles.ToArray();

            // Если нет свободных для размещения прямоугольников
            if (!rectangles.Any())
                return 0;
            double metrics = Int32.MaxValue;

            foreach (var packableRectangle in rectangles)
            {
                double heightMetrics1 = -1;
                double heightMetrics2 = -1;
                // Если прямоугольник влезает в текущую область размещения
                if (packableRectangle.Fit(area))
                {
                    // Выделим 2 свободные области, образовавшиеся после размещения прямоугольника (правая и нижняя)
                    var areas = area.Split(packableRectangle);
                    var freeRectangles = new List<PackableRectangle>(rectangles);
                    // Скопируем в список все доступные прямоугольники кроме текущего
                    freeRectangles.Remove(packableRectangle);
                    foreach (var freeRectangle in freeRectangles)
                    {
                        freeRectangle.IsPlaced = false;
                    }
                    // Разместим максимальное количество прямоугольников в правой области (она никак не сказывается на общей длине)
                    PlaceRectangle(areas[0], freeRectangles);
                    // Остатки разместим в нижней области
                    heightMetrics1 = PlaceRectangle(areas[1], freeRectangles.Where((r) => !r.IsPlaced)) + packableRectangle.Height;


                }
                // Повернем первый прямоугольник
                // и поробуем повторить все размещение еще раз
                packableRectangle.Rotate();
                if (packableRectangle.Fit(area))
                {
                    var areas = area.Split(packableRectangle);
                    var freeRectangles = new List<PackableRectangle>(rectangles);
                    freeRectangles.Remove(packableRectangle); 
                    foreach (var freeRectangle in freeRectangles)
                    {
                        freeRectangle.IsPlaced = false;
                    }
                    PlaceRectangle(areas[0], freeRectangles);
                    heightMetrics2 = PlaceRectangle(areas[1], freeRectangles.Where((r) => !r.IsPlaced)) + packableRectangle.Height;

                }

                // Если хотя бы одно размещение сошлось
                // выберем одно с наименьшей положительной метрикой
                if (heightMetrics1 >= 0 || heightMetrics2 >= 0)
                {
                    if (heightMetrics2 < 0) metrics = heightMetrics1;
                    else if (heightMetrics1 < 0) metrics = heightMetrics2;
                    else metrics = Math.Min(heightMetrics2, heightMetrics1);
                    // Если первое размещение привело к более удачному варианту, то вернемся к нему,
                    // перевернув прямоугольник обратно
                    if (heightMetrics1 > 0 && (heightMetrics2 > heightMetrics1 || heightMetrics2 < 0))
                    {
                        packableRectangle.Rotate();
                    }
                    // И окончательно разместим прямоугольник, пересчитав координаты точки начала размещения
                    packableRectangle.IsPlaced = true;
                    packableRectangle.X = area.X;
                    packableRectangle.Y = area.Y;

                    // Заново рекурсивно повторим наиболее удачное размещение 
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
