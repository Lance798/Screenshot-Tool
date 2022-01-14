using IronBarCode;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Control = System.Windows.Forms.Control;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Screenshot_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ToolbarWindow toolbarWindow;
        private readonly NotifyIcon notifyIcon;
        private bool isMouseDown;
        private int startX, startY;
        private Bitmap screenBMP, imageBoxBMP;

        public MainWindow()
        {
            InitializeComponent();
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

            toolbarWindow = new ToolbarWindow();
            toolbarWindow.ButtonPressedHandler += OnToolButtonClick;
            toolbarWindow.Visibility = Visibility.Hidden;
            toolbarWindow.KeyDown += KeyboardHandler;
        }

        private void OnToolButtonClick(object sender, ButtonPressedEventArgs e)
        {
            switch(e.Button)
            {
                case Buttons.QRCODE:
                    Func_QRcodeDecoder();
                    break;
                case Buttons.UPLOAD:
                    Func_UploadToImgurAsync();
                    break;
                case Buttons.PAINT:
                    Func_OpenMSPaint();
                    break;
                case Buttons.SAVE:
                    Func_Save();
                    break;
                case Buttons.COPY:
                    Func_Copy();
                    break;
                case Buttons.CLOSE:
                    Func_CloseScreenshot();
                    break;
            }
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
                Func_CloseScreenshot();
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
            {
                //imageBox.ContextMenu.IsOpen = true;
                
                toolbarWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                toolbarWindow.Left = Control.MousePosition.X - toolbarWindow.Width;
                toolbarWindow.Top = Control.MousePosition.Y;
                toolbarWindow.Visibility = Visibility.Visible;
            }

        }

        private void ImageBox_mouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            startX = Control.MousePosition.X;
            startY = Control.MousePosition.Y;
            toolbarWindow.Visibility = Visibility.Hidden;
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
                bmp.Dispose();
                if (rectHeight != 0 || rectWidth != 0)
                    labelSize.Visibility = Visibility.Visible;
                labelSize.Content = string.Format("{0}x{1}", rectangle.Width, rectangle.Height);
                labelSize.Margin = rectangle.Margin;
            }
        }

        private Bitmap AdjustBrightness(Image image, float brightness)
        {
            // Make the ColorMatrix.
            float b = brightness;
            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] {b, 0, 0, 0, 0},
                new float[] {0, b, 0, 0, 0},
                new float[] {0, 0, b, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1},
            });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(cm);

            // Draw the image onto the new bitmap while applying
            // the new ColorMatrix.
            PointF[] points =
            {
                new PointF(0, 0),
                new PointF(image.Width, 0),
                new PointF(0, image.Height),
            };
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            // Make the result bitmap.
            Bitmap bm = new Bitmap(image.Width, image.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(image, points, rect,
                    GraphicsUnit.Pixel, attributes);
            }

            // Return the result.
            return bm;
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
            screenBMP = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(screenBMP);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenBMP.Size, CopyPixelOperation.SourceCopy);
            imageBoxBMP = (Bitmap)screenBMP.Clone();
            imageBoxBMP = AdjustBrightness(imageBoxBMP, (float)0.5);
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
                MessageBox.Show("無法複製", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, bmp);
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已複製到剪貼簿", ToolTipIcon.None);
            bmp.Dispose();
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
                MessageBox.Show("無法存檔", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png";
            dialog.Filter = "圖片檔PNG (*.png)|*.png";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bmp.Save(dialog.FileName, ImageFormat.Png);
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已儲存到路徑：" + dialog.FileName, ToolTipIcon.None);
            }
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_OpenMSPaint()
        {
            Bitmap bmp = null;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法傳送圖片", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            string path = Path.GetTempPath() + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            bmp.Save(path);
            System.Diagnostics.Process.Start("mspaint.exe", path);
            Func_CloseScreenshot();
        }

        private void Func_UploadToImgurAsync()
        {
            Bitmap bmp = null;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法上傳", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            byte[] byteImage = ms.ToArray();
            var API_KEY = "6d207e02198a847aa98d0a2a901485a5";
            var bmp_base64 = Convert.ToBase64String(byteImage).Replace("+", "%2B");
            string postData = string.Format("key={0}&action=upload&source={1}", API_KEY, bmp_base64);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            bmp.Dispose();

            WebRequest request = WebRequest.Create("https://freeimage.host/api/1/upload");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            using (dataStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                dynamic json = JObject.Parse(responseFromServer);
                string url = json.image.url;
                System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, url);
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已上傳到網址：" + url + "\n連結已複製到剪貼簿", ToolTipIcon.None);

            }
            response.Close();
            Func_CloseScreenshot();
        }

        private void Func_QRcodeDecoder()
        {
            Bitmap bmp = null;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法讀取圖片", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            BarcodeResult result = BarcodeReader.QuicklyReadOneBarcode(bmp);
            if (result == null)
                MessageBox.Show("找不到QRcode", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, result.Text);
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "結果為：" + result.Text + "\n內容已複製到剪貼簿", ToolTipIcon.None);
            }
            Func_CloseScreenshot();
        }

        private void Func_CloseScreenshot()
        {
            toolbarWindow.Visibility = Visibility.Hidden;
            Hide();
            labelSize.Visibility = Visibility.Hidden;
            imageBox.ContextMenu.IsOpen = false;
        }
    }
}
