﻿using ACadSharp;
using ACadSharp.IO;
using Microsoft.Win32;
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
        private CadDocument cadDocument; // ACadSharp library
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

        #region Enum for each buttons
        // enum corresponding to each button
        public enum ContentType
        {
            None,
            PngImage,
            BasicDraw,
            DxfFile
        }
        private ContentType currentContent = ContentType.None;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            MousePosition = "(0, 0)";
            GreyValue = "(0)";
        }

        #region Property
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Load Png Image
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

        // Draw loaded image
        private void LoadImage(string filePath)
        {
            try
            {
                ClearDrawing();
                currentContent = ContentType.PngImage;

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
        #endregion

        #region Draw basic shapes
        private void BasicDrawButton_Click(object sender, RoutedEventArgs e)
        {
            DrawBasicShapes();
        }
        private void DrawBasicShapes()
        {
            ClearDrawing();
            currentContent = ContentType.BasicDraw;

            // Vẽ hình chữ nhật
            Rectangle rectangle = new Rectangle();
            rectangle.Width = 100;
            rectangle.Height = 80;
            rectangle.Fill = Brushes.LightBlue;
            rectangle.Stroke = Brushes.Black;
            rectangle.StrokeThickness = 2;
            Canvas.SetLeft(rectangle, 250);
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
            Canvas.SetTop(ellipse, 150);
            DrawingCanvas.Children.Add(ellipse);

            // Vẽ đường thẳng
            Line line = new Line();
            line.X1 = 200;
            line.Y1 = 20;
            line.X2 = 150;
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

            //// Vẽ đường Path phức tạp.
            //Path path = new Path();
            //path.Data = Geometry.Parse("M 300,300 L 100,50 A 40,40 0 1 1 490,320 Z");
            //path.Fill = Brushes.Orange;
            //path.Stroke = Brushes.Black;
            //DrawingCanvas.Children.Add(path);
        }
        #endregion

        #region Load DXF/DWG
        private void DxfDwgButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DXF files (*.dxf)|*.dxf|" +
                         "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                if (pngImage != null)
                    pngImage = null;
                LoadDxfFile(filePath);
            }
        }

        private void LoadDxfFile(string filePath)
        {
            try
            {
                //cadDocument = new CadDocument();

                // check version before loading
                using (DxfReader reader = new DxfReader(filePath))
                {
                    int i = 0;
                    cadDocument = reader.Read();
                    DrawDxfGraphics();
                }


                // load dxf file
                ClearDrawing();
                currentContent = ContentType.DxfFile;

                if (cadDocument == null)
                {
                    MessageBox.Show("Document is null after Load!");
                }
                else
                {
                    DrawDxfGraphics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading DXF: {ex.Message}.\nStack Trace: {ex.StackTrace}");
            }
        }

        // Draw loaded dxf file
        private void DrawDxfGraphics()
        {
            DrawingCanvas.Children.Clear();
            // draw line

            var allEntity = cadDocument.Entities;


        }
        #endregion

        #region Mouse interactions

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

            switch (currentContent)
            {
                case ContentType.PngImage:
                    UpdateImagePosition();
                    break;
                case ContentType.DxfFile:
                    DrawDxfGraphics();
                    break;
                case ContentType.BasicDraw:
                    DrawBasicShapes();
                    break;
                case ContentType.None:
                    break;
            }
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
                if (cadDocument != null)
                {
                    DrawDxfGraphics();
                }

                lastMousePosition = currentPos;
            }
        }

        private bool IsMouseNearLine(Point mousePos, System.Windows.Shapes.Line line)
        {
            // Basic proximity check (expand for precision)
            double tolerance = 5 / zoomLevel;
            return mousePos.X >= line.X1 - tolerance && mousePos.X <= line.X2 + tolerance &&
                   mousePos.Y >= line.Y1 - tolerance && mousePos.Y <= line.Y2 + tolerance;
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
        #endregion

        #region Clear all stuff on canvas
        private void ClearDrawing()
        {
            DrawingCanvas.Children.Clear();
            cadDocument = null;
            pngImage = null;
            writeableBitmap = null;
            zoomLevel = 1;
            offsetX = 0;
            offsetY = 0;
        }
        #endregion
    }

}