using Microsoft.Win32;
using netDxf;
using netDxf.Header;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PCBViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DxfDocument dxfDocument;
        private double zoomLevel = 1.0;
        private double offsetX;
        private double offsetY;
        private Image pngImage;
        private const double MinZoom = 0.1;  // Zoom tối thiểu (10%)
        private const double MaxZoom = 10.0; // Zoom tối đa (1000%)
        private bool isDrag = false;
        private Point lastMousePosition;
        private string _mousePosition;
        private string _greyValue;
        private WriteableBitmap writeableBitmap;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string MousePosition
        {
            get => _mousePosition;
            set
            {
                _mousePosition = value;
                OnPropertyChanged(nameof(MousePosition));
            }
        }

        public string GreyValue
        {
            get => _greyValue;
            set
            {
                _greyValue = value;
                OnPropertyChanged(nameof(GreyValue));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            MousePosition = "(0, 0)";
            GreyValue = "(0)";
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "PNG files (*.png)|*.png|" +
                         "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                LoadImage(filePath);
            }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                // neu co anh da ve tren canvas
                if (pngImage != null)
                {
                    DrawingCanvas.Children.Remove(pngImage);
                }

                // tao doi tuong Bitmap tu file PNG
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(filePath);
                bitmapImage.EndInit();

                // tao writeableBitmap de truy cap pixel
                writeableBitmap = new WriteableBitmap(bitmapImage);

                // tao Image control
                pngImage = new Image
                {
                    //Source = bitmapImage,
                    Source = writeableBitmap,
                    Stretch = Stretch.UniformToFill,
                };

                // tinh toan zoom level
                double widthRatio = DrawingCanvas.ActualWidth / writeableBitmap.PixelWidth;
                double heightRatio = DrawingCanvas.ActualHeight / writeableBitmap.PixelHeight;
                zoomLevel = Math.Min(widthRatio, heightRatio);

                // Tính kích thước ảnh sau khi scale
                double scaledWidth = writeableBitmap.PixelWidth * zoomLevel;
                double scaledHeight = writeableBitmap.PixelHeight * zoomLevel;

                // Căn giữa ảnh bằng cách tính offset
                offsetX = (DrawingCanvas.ActualWidth - scaledWidth) / 2;  // Khoảng cách từ trái để căn giữa
                offsetY = (DrawingCanvas.ActualHeight - scaledHeight) / 2; // Khoảng cách từ trên để căn giữa


                // update vi tri anh
                UpdateImagePosition();

                // them image vao canvas
                DrawingCanvas.Children.Add(pngImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateImagePosition()
        {
            if (pngImage != null)
            {
                double newWidth = writeableBitmap.PixelWidth * zoomLevel;
                double newHeight = writeableBitmap.PixelHeight * zoomLevel;

                Canvas.SetLeft(pngImage, offsetX);
                Canvas.SetTop(pngImage, offsetY);
                pngImage.Width = newWidth;
                pngImage.Height = newHeight;
            }
        }

        private void UpdateZoom()
        {
            if (pngImage != null)
            {
                pngImage.Width = writeableBitmap.PixelWidth * zoomLevel;
                pngImage.Height = writeableBitmap.PixelHeight * zoomLevel;
            }
        }

        private void DxfDwgButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "PNG files (*.png)|*.png|" +
                         "DXF files (*.dxf)|*.dxf|" +
                         "DWG files (*.dwg)|*.dwg|" +
                         "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                LoadDxfFile(filePath);
                DrawGraphics();
            }
        }

        private void LoadDxfFile(string filePath)
        {
            try
            {
                dxfDocument = new DxfDocument();

                // check version before loading
                DxfVersion dxfVersion = DxfDocument.CheckDxfFileVersion(filePath);
                if (dxfVersion < DxfVersion.AutoCad2000)
                {
                    MessageBox.Show("The selected file version: " + dxfVersion + " is not compatible with netDxf library which only from AutoCad13");
                    return;
                }

                // load file
                dxfDocument = DxfDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading DXF: {ex.Message}");
            }
        }

        private void DrawBasicShapes()
        {
            // Vẽ hình chữ nhật
            Rectangle rectangle = new Rectangle();
            rectangle.Width = 100;
            rectangle.Height = 80;
            rectangle.Fill = Brushes.LightBlue;
            rectangle.Stroke = Brushes.Black;
            rectangle.StrokeThickness = 2;
            Canvas.SetLeft(rectangle, 50);
            Canvas.SetTop(rectangle, 50);
            DrawingCanvas.Children.Add(rectangle);

            // Vẽ hình tròn
            Ellipse ellipse = new Ellipse();
            ellipse.Width = 70;
            ellipse.Height = 70;
            ellipse.Fill = Brushes.LightGreen;
            ellipse.Stroke = Brushes.Red;
            ellipse.StrokeThickness = 1;
            Canvas.SetLeft(ellipse, 150);
            Canvas.SetTop(ellipse, 100);
            DrawingCanvas.Children.Add(ellipse);

            // Vẽ đường thẳng
            Line line = new Line();
            line.X1 = 20;
            line.Y1 = 20;
            line.X2 = 200;
            line.Y2 = 150;
            line.Stroke = Brushes.DarkGray;
            line.StrokeThickness = 3;
            DrawingCanvas.Children.Add(line);

            // Vẽ đa giác
            Polygon polygon = new Polygon();
            polygon.Points = new PointCollection() { new Point(10, 10), new Point(100, 50), new Point(50, 120) };
            polygon.Fill = Brushes.Yellow;
            polygon.Stroke = Brushes.Black;
            DrawingCanvas.Children.Add(polygon);

            // Vẽ đường Path phức tạp.
            Path path = new Path();
            path.Data = Geometry.Parse("M 10,10 L 100,50 A 40,40 0 1 1 50,120 Z");
            path.Fill = Brushes.Orange;
            path.Stroke = Brushes.Black;
            DrawingCanvas.Children.Add(path);
        }

        private void DrawGraphics()
        {
            DrawBasicShapes();

            foreach (var element in DrawingCanvas.Children)
            {
                if (element != pngImage)
                {
                    DrawingCanvas.Children.Remove(element as UIElement);
                    break;
                }
            }

            if (dxfDocument == null) return;

            // draw line
            foreach (netDxf.Entities.Line line in dxfDocument.Entities.Lines)
            {
                System.Windows.Shapes.Line uiLine = new System.Windows.Shapes.Line
                {
                    X1 = line.StartPoint.X * zoomLevel,
                    Y1 = line.StartPoint.Y * zoomLevel,
                    X2 = line.EndPoint.X * zoomLevel,
                    Y2 = line.EndPoint.Y * zoomLevel,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                };

                //Canvas.SetLeft(uiLine, 0);
                //Canvas.SetTop(uiLine, 0);
                DrawingCanvas.Children.Add(uiLine);
            }

            // draw circle
            foreach (netDxf.Entities.Circle circle in dxfDocument.Entities.Circles)
            {
                System.Windows.Shapes.Ellipse uiEllipse = new System.Windows.Shapes.Ellipse
                {
                    Width = circle.Radius * 2 * zoomLevel,
                    Height = circle.Radius * 2 * zoomLevel,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 1,
                };

                Canvas.SetLeft(uiEllipse, (circle.Center.X * zoomLevel) + offsetX - circle.Radius * zoomLevel);
                Canvas.SetTop(uiEllipse, (circle.Center.Y * zoomLevel) + offsetY - circle.Radius * zoomLevel);
                DrawingCanvas.Children.Add(uiEllipse);
            }
        }

        private void DrawingCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(DrawingCanvas);
            double oldZoom = zoomLevel;

            // vi tri chuot tuong doi tren anh truoc khi zoom
            double mouseXOnImage = (mousePos.X - offsetX) / oldZoom;
            double mouseYOnImage = (mousePos.Y - offsetY) / oldZoom;

            if (e.Delta > 0) // zoom-in
            {
                zoomLevel *= 1.1;
            }
            else // zoom-out
                zoomLevel /= 1.1;

            // gioi han min va max zoom
            if (zoomLevel < MinZoom) zoomLevel = MinZoom;
            if (zoomLevel > MaxZoom) zoomLevel = MaxZoom;

            // dieu chinh offset de vi tri chuot co dinh
            offsetX = mousePos.X - (mouseXOnImage * zoomLevel);
            offsetY = mousePos.Y - (mouseYOnImage * zoomLevel);

            LimitOffsets();

            UpdateImagePosition();

            DrawGraphics(); // redraw with updated zoom
        }

        private void LimitOffsets()
        {
            if (pngImage == null) return;

            double imageWidth = writeableBitmap.PixelWidth * zoomLevel;
            double imageHeight = writeableBitmap.PixelHeight * zoomLevel;

            // Giới hạn offsetX
            if (imageWidth <= DrawingCanvas.ActualWidth) // Nếu ảnh nhỏ hơn Viewer theo chiều ngang
            {
                offsetX = (DrawingCanvas.ActualWidth - imageWidth) / 2; // Căn giữa
            }
            else
            {
                // Không để cạnh trái ảnh vượt quá mép phải Viewer
                if (offsetX > 0) offsetX = 0;

                // Không để cạnh phải ảnh vượt quá mép trái Viewer
                if (offsetX < DrawingCanvas.ActualWidth - imageWidth)
                    offsetX = DrawingCanvas.ActualWidth - imageWidth;
            }

            // Giới hạn offsetY
            if (imageHeight <= DrawingCanvas.Height) // Nếu ảnh nhỏ hơn Viewer theo chiều dọc
            {
                offsetY = (DrawingCanvas.ActualHeight - imageHeight) / 2; // Căn giữa
            }
            else
            {
                // Không để cạnh trên ảnh vượt quá mép dưới Viewer
                if (offsetY > 0) offsetY = 0;

                // Không để cạnh dưới ảnh vượt quá mép trên Viewer
                if (offsetY < DrawingCanvas.ActualHeight - imageHeight)
                    offsetY = DrawingCanvas.ActualHeight - imageHeight;
            }
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Cập nhật tọa độ chuột
            Point mousePos = e.GetPosition(DrawingCanvas);
            MousePosition = $"({(int)mousePos.X}, {(int)mousePos.Y})";

            // Hien thi grey scale
            if (writeableBitmap != null)
            {
                // toa do tren anh goc
                double imageX = (mousePos.X - offsetX) / zoomLevel;
                double imageY = (mousePos.Y - offsetY) / zoomLevel;

                // chi check neu trong anh
                if (imageX >= 0 && imageX < writeableBitmap.PixelWidth && imageY >= 0 && imageY < writeableBitmap.PixelHeight)
                {
                    int x = (int)imageX;
                    int y = (int)imageY;

                    // get value
                    byte[] pixels = new byte[4];
                    writeableBitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
                    byte r = pixels[2];
                    byte g = pixels[1];
                    byte b = pixels[0];
                    int grey = (int)(0.299 * r + 0.587 * g + 0.114 * b);
                    GreyValue = $"({grey})";
                }
                else
                {
                    GreyValue = "(0)";
                }
            }
            else
            {
                GreyValue = "(0)";
            }

            // xu ly khi re chuot
            if (isDrag)
            {
                Point currentPos = e.GetPosition(DrawingCanvas);
                double deltaX = currentPos.X - lastMousePosition.X;
                double deltaY = currentPos.Y - lastMousePosition.Y;
                offsetX += deltaX;
                offsetY += deltaY;

                // gioi han offset
                LimitOffsets();

                UpdateImagePosition();
                DrawGraphics();

                lastMousePosition = currentPos;
            }
        }

        private bool IsMouseNearLine(Point mousePosition, System.Windows.Shapes.Line line)
        {
            double tolerance = 5 / zoomLevel;
            return mousePosition.X >= line.X1 - tolerance && mousePosition.X <= line.X2 + tolerance &&
                   mousePosition.Y >= line.Y1 - tolerance && mousePosition.Y <= line.Y2 + tolerance;
        }

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (pngImage == null || e.RightButton != MouseButtonState.Pressed)
                return;

            isDrag = true;
            lastMousePosition = e.GetPosition(DrawingCanvas);
            DrawingCanvas.CaptureMouse();
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrag && e.RightButton == MouseButtonState.Released)
            {
                isDrag = false;
                DrawingCanvas.ReleaseMouseCapture();
            }
        }

    }
}