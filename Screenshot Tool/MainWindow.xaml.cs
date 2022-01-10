using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Control = System.Windows.Forms.Control;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Screenshot_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private bool isMouseDown;
        private int startX, startY;
        private Bitmap screenBMP, imageBoxBMP;

        public MainWindow()
        {
            InitializeComponent();

            ToolbarWindow toolbarWindow = new ToolbarWindow();
            toolbarWindow.Show();
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("設定");
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("退出程式", OnClikExit);

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = new Icon("Resources/screenshot.ico"),
                Text = "擷取視窗螢幕",
                ContextMenu = contextMenu
            };
            notifyIcon.MouseClick += OnClickNotifyIcon;
            Hide();
            ShowInTaskbar = false;
            KeyDown += KeyboardHandler;
            HotKey _hotKey = new HotKey(Key.F8, 0, OnHotKeyDown);
        }

        private void OnClickNotifyIcon(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Func_Screenshot();
        }

        private void OnClikExit(object sender, EventArgs e)
        {
            Func_Exit();
        }

        private void KeyboardHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
                labelSize.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
                Func_Copy();
            else if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
                Func_Save();
            
        }

        private void OnHotKeyDown(HotKey obj) { Func_Screenshot(); }

        private void ImageBox_mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            if (rectangle.Width > 0 && rectangle.Height > 0)
                imageBox.ContextMenu.IsOpen = true;
        }

        private void ImageBox_mouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            startX = Control.MousePosition.X;
            startY = Control.MousePosition.Y;
        }

        private void ImageBox_mouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isMouseDown)
            {
                double currentX = Control.MousePosition.X;
                double currentY = Control.MousePosition.Y;
                double rectWidth = currentX - startX;
                double rectHeight = currentY - startY;
                rectangle.Margin = new Thickness(rectWidth >= 0 ? startX : currentX, rectHeight >= 0 ? startY : currentY, 0, 0);
                rectangle.Width = Math.Abs(rectWidth);
                rectangle.Height = Math.Abs(rectHeight);
                Point p = rectangle.TransformToAncestor(this).Transform(new Point(0, 0));
                Bitmap bmp = ReplaceImage(imageBoxBMP, (int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height, screenBMP);
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(),
                                                    IntPtr.Zero,
                                                    Int32Rect.Empty,
                                                    BitmapSizeOptions.FromEmptyOptions());
                imageBox.Source = bitmapSource;
                if (rectHeight != 0 || rectWidth != 0)
                    labelSize.Visibility = Visibility.Visible;
                labelSize.Content = string.Format("{0}x{1}", rectangle.Width, rectangle.Height);
                labelSize.Margin = rectangle.Margin;
            }
        }
        private Bitmap ReplaceImage(Bitmap image, int x, int y, int width, int height, Bitmap imageSource)
        {
            Bitmap newBMP = (Bitmap)image.Clone();
            Point p = rectangle.TransformToAncestor(this).Transform(new Point(0, 0));
            Rectangle rect = new Rectangle((int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height);
            try
            {
                Bitmap source = imageSource.Clone(rect, imageSource.PixelFormat);
                Graphics g = Graphics.FromImage(newBMP);
                g.DrawImage(source, new PointF((float)p.X, (float)p.Y));
                g.Dispose();
                source.Dispose();
            }
            catch { }
            return newBMP;
        }
        public static Bitmap DecreaseBrightness(Bitmap Image)
        {
            LockBitmap lockbmp = new LockBitmap(Image);
            lockbmp.LockBits();
            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    int newR = lockbmp.GetPixel(i, j).R / 2;
                    int newG = lockbmp.GetPixel(i, j).G / 2;
                    int newB = lockbmp.GetPixel(i, j).B / 2;
                    Color newColor = Color.FromArgb(newR, newG, newB);
                    lockbmp.SetPixel(i, j, newColor);
                }
            }
            lockbmp.UnlockBits();
            return Image;
        }

        private void MenuItem_Click_Save(object sender, RoutedEventArgs e) { Func_Save(); }

        private void MenuItem_Click_Copy(object sender, RoutedEventArgs e)  { Func_Copy(); }

        private Bitmap GetSelectedArea()
        {
            Point p = rectangle.TransformToAncestor(this).Transform(new Point(0, 0));
            Rectangle rect = new Rectangle((int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height);
            Bitmap bmp = screenBMP.Clone(rect, screenBMP.PixelFormat);
            return bmp;
        }

        private void Func_Exit()
        {
            System.Windows.Application.Current.Shutdown();
        }
        private void Func_Screenshot()
        {
            isMouseDown = false;
            Rectangle rect = Screen.GetBounds(System.Drawing.Point.Empty);
            screenBMP = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(screenBMP);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenBMP.Size, CopyPixelOperation.SourceCopy);
            imageBoxBMP = (Bitmap)screenBMP.Clone();
            DecreaseBrightness(imageBoxBMP);
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(imageBoxBMP.GetHbitmap(),
                                                IntPtr.Zero,
                                                Int32Rect.Empty,
                                                BitmapSizeOptions.FromEmptyOptions());

            imageBox.Margin = new Thickness(0);
            imageBox.Width = bitmapSource.Width;
            imageBox.Height = bitmapSource.Height;
            imageBox.Source = bitmapSource;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            rectangle.Width = 0;
            rectangle.Height = 0;

            Show();
        }

        private void Func_Copy()
        {
            Bitmap bmp = null;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show("無法複製", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, bmp);
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "截圖成功！", ToolTipIcon.None);
            Func_CloseScreenshot();
        }
        private void Func_Save()
        {
            Bitmap bmp = null;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                System.Windows.Forms.MessageBox.Show("無法存檔", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "圖片檔PNG (*.png)|*.png";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bmp.Save(dialog.FileName, ImageFormat.Png);
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已儲存到路徑：" + dialog.FileName, ToolTipIcon.None);
            }
            Func_CloseScreenshot();
        }

        private void Func_CloseScreenshot()
        {
            Hide();
            labelSize.Visibility = Visibility.Hidden;
            imageBox.ContextMenu.IsOpen = false;
        }
    }
}
