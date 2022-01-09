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

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("設定");
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("退出程式", onClikExit);

            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Icon = new Icon("Resources/screenshot.ico");
            notifyIcon.Text = "擷取視窗螢幕";
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.MouseClick += onClickNotifyIcon;
            Hide();
            ShowInTaskbar = false;
            KeyDown += keyboardHandler;
            HotKey _hotKey = new HotKey(Key.F8, 0, onHotKeyDown);
        }

        private void onClickNotifyIcon(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            func_screenshot();
        }

        private void onClikExit(object sender, EventArgs e)
        {
            func_Exit();
        }

        private void keyboardHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
                labelSize.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
                func_copy();
            else if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
                func_save();
            
        }

        private void onHotKeyDown(HotKey obj) { func_screenshot(); }

        private void imageBox_mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            if (rectangle.Width > 0 && rectangle.Height > 0)
                imageBox.ContextMenu.IsOpen = true;
        }

        private void imageBox_mouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            startX = Control.MousePosition.X;
            startY = Control.MousePosition.Y;
        }

        private void imageBox_mouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
                Bitmap bmp = replaceImage(imageBoxBMP, (int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height, screenBMP);
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
        private Bitmap replaceImage(Bitmap image, int x, int y, int width, int height, Bitmap imageSource)
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
        public static Bitmap decreaseBrightness(Bitmap Image)
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

        private void MenuItem_Click_Save(object sender, RoutedEventArgs e) { func_save(); }

        private void menuItem_ClickCopy(object sender, RoutedEventArgs e)  { func_copy(); }

        private Bitmap getSelectedArea()
        {
            Point p = rectangle.TransformToAncestor(this).Transform(new Point(0, 0));
            Rectangle rect = new Rectangle((int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height);
            Bitmap bmp = screenBMP.Clone(rect, screenBMP.PixelFormat);
            return bmp;
        }

        private void func_Exit()
        {
            System.Windows.Application.Current.Shutdown();
        }
        private void func_screenshot()
        {
            isMouseDown = false;
            Rectangle rect = Screen.GetBounds(System.Drawing.Point.Empty);
            screenBMP = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(screenBMP);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenBMP.Size, CopyPixelOperation.SourceCopy);
            imageBoxBMP = (Bitmap)screenBMP.Clone();
            decreaseBrightness(imageBoxBMP);
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

        private void func_copy()
        {
            Bitmap bmp = getSelectedArea();
            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, bmp);
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "截圖成功！", ToolTipIcon.None);
            Hide();
            labelSize.Visibility = Visibility.Hidden;
        }
        private void func_save()
        {
            
            Bitmap bmp = getSelectedArea();
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "圖片檔PNG (*.png)";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已儲存到路徑：" + dialog.FileName, ToolTipIcon.None);
            bmp.Save(dialog.FileName, ImageFormat.Png);
            Hide();

        }
    }
}
