using BarcodeLib.BarcodeReader;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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
        private Bitmap bmp_raw, bmp_Background;

        public MainWindow()
        {
            InitializeComponent();
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("設定", OpenSettings);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("退出程式", OnClikExit);

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = new Icon("Resources/screenshot.ico"),
                Text = "擷取視窗螢幕",
                ContextMenu = contextMenu
            };

            Hide();
            ShowInTaskbar = false;
            KeyDown += KeyboardHandler;
            RegisterHotKey();

            toolbarWindow = new ToolbarWindow();
            toolbarWindow.ButtonPressedHandler += OnToolButtonClick;
            toolbarWindow.Visibility = Visibility.Hidden;
            toolbarWindow.KeyDown += KeyboardHandler;

            /*Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            if(Properties.Settings.Default.RunOnBoot)
                key.SetValue(curAssembly.GetName().Name, curAssembly.Location); 
            else
                key.DeleteValue(curAssembly.GetName().Name, false);
            key.Close();*/
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Show();
        }

        public void RegisterHotKey()
        {
            string[] a = Properties.Settings.Default.HotKey.Split('+');
            Enum.TryParse(a[0], out Key hotKey);
            KeyModifier modifier = 0;
            if (a.Length > 1)
                Enum.TryParse(a[1], out modifier);
            HotKey _hotKey = new HotKey(hotKey, modifier, OnHotKeyDown);
        }

        private void OnToolButtonClick(object sender, ButtonPressedEventArgs e)
        {
            switch(e.Button)
            {
                case Buttons.SEARCH:
                    Func_SearchImage();
                    break;
                case Buttons.QRCODE:
                    try
                    {
                        Func_ScanQRcode();
                    }catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case Buttons.UPLOAD:
                    Func_UploadImage();
                    break;
                case Buttons.PAINT:
                    Func_OpenMSPaint();
                    break;
                case Buttons.SAVE:
                    Func_SaveImage();
                    break;
                case Buttons.COPY:
                    Func_CopyImage();
                    break;
                case Buttons.CLOSE:
                    Func_CloseScreenshot();
                    break;
            }
        }

        private void OnClikExit(object sender, EventArgs e) { Func_Exit(); }

        private void KeyboardHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Func_CloseScreenshot();
            else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
                Func_CopyImage();
            else if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
                Func_SaveImage();
        }

        private void OnHotKeyDown(HotKey obj) { Func_Screenshot(); }

        private void ImageBox_mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            if (rectangle.Width > 0 && rectangle.Height > 0)
            {
                toolbarWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                toolbarWindow.Left = rectangle.Margin.Left + rectangle.Width - toolbarWindow.Width;
                toolbarWindow.Top = rectangle.Margin.Top + rectangle.Height;
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
                if (rectHeight == 0 || rectWidth == 0)
                    return;
                rectangle.Margin = new Thickness(rectWidth >= 0 ? startX : currentX, rectHeight >= 0 ? startY : currentY, 0, 0);
                rectangle.Width = Math.Abs(rectWidth);
                rectangle.Height = Math.Abs(rectHeight);

                imgBox_raw.Clip = new RectangleGeometry
                {
                    Rect = new Rect(
                        rectangle.Margin.Left,
                        rectangle.Margin.Top,
                        rectangle.Width,
                        rectangle.Height)
                };

                labelSize.Visibility = Visibility.Visible;
                StringBuilder builder = new StringBuilder(rectangle.Width.ToString());
                builder.Append("x");
                builder.Append(rectangle.Height.ToString());
                labelSize.Content = builder.ToString();
                labelSize.Margin = rectangle.Margin; 
            }
        }

        private Bitmap AdjustBrightness(Image image, float brightness)
        {
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

            PointF[] points =
            {
                new PointF(0, 0),
                new PointF(image.Width, 0),
                new PointF(0, image.Height),
            };

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            Bitmap bm = new Bitmap(image.Width, image.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(image, points, rect,
                    GraphicsUnit.Pixel, attributes);
            }
            return bm;
        }

        private Bitmap GetSelectedArea()
        {
            Point p = rectangle.TransformToAncestor(this).Transform(new Point(0, 0));
            Rectangle rect = new Rectangle((int)p.X, (int)p.Y, (int)rectangle.Width, (int)rectangle.Height);
            Bitmap bmp = bmp_raw.Clone(rect, bmp_raw.PixelFormat);
            return bmp;
        }

        public BitmapImage ConvertBitmap(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private string UploadImageToCloud(Bitmap bmp)
        {
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

            string url = "";
            WebResponse response = request.GetResponse();
            using (dataStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                dynamic json = JsonConvert.DeserializeObject(responseFromServer);
                url = json.image.url;
            }
            response.Close();
            return url;
        }
        private void Func_Exit()
        {
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已退出程式", ToolTipIcon.None);
            System.Windows.Application.Current.Shutdown();
        }

        private void Func_Screenshot()
        {
            isMouseDown = false;
            Rectangle rect = Screen.GetBounds(System.Drawing.Point.Empty);
            bmp_raw = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp_raw);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp_raw.Size, CopyPixelOperation.SourceCopy);
            bmp_Background = (Bitmap)bmp_raw.Clone();
            bmp_Background = AdjustBrightness(bmp_Background, (float)0.5);

            BitmapImage bitmapImage = ConvertBitmap(bmp_Background);
            imgBox_Background.Margin = new Thickness(0);
            imgBox_Background.Width = bitmapImage.Width;
            imgBox_Background.Height = bitmapImage.Height;
            imgBox_Background.Source = bitmapImage;

            imgBox_raw.Margin = new Thickness(0);
            imgBox_raw.Width = bitmapImage.Width;
            imgBox_raw.Height = bitmapImage.Height;
            imgBox_raw.Source = ConvertBitmap(bmp_raw);
            imgBox_raw.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, 0) };

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            rectangle.Width = 0;
            rectangle.Height = 0;
            Show();
        }

        private void Func_CopyImage()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法複製", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }
            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Bitmap, bmp);
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已複製到剪貼簿", ToolTipIcon.None);
            bmp.Dispose();
            Func_CloseScreenshot();
        }
        private void Func_SaveImage()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法存檔", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"),
                Filter = "PNG (*.png)|*.png|JPEG (*jpg)|*.jpg|BMP (*bmp)|*.bmp",
                InitialDirectory = Properties.Settings.Default.SavePath
                
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                switch (dialog.FilterIndex)
                {
                    case 1:
                        bmp.Save(dialog.FileName, ImageFormat.Png);
                        break;
                    case 2:
                        bmp.Save(dialog.FileName, ImageFormat.Jpeg);
                        break;
                    case 3:
                        bmp.Save(dialog.FileName, ImageFormat.Bmp);
                        break;
                }
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已儲存到路徑：" + dialog.FileName, ToolTipIcon.None);
            }
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_OpenMSPaint()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法傳送圖片", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }
            string path = Path.GetTempPath() + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            bmp.Save(path);
            System.Diagnostics.Process.Start("mspaint.exe", path);
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_UploadImage()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法上傳", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }
            string url = UploadImageToCloud(bmp);
            System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, url);
            notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "已上傳到網址：" + url + "\n連結已複製到剪貼簿", ToolTipIcon.None);
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_ScanQRcode()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法讀取圖片", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }

            string[] result = BarcodeReader.read(bmp, BarcodeReader.QRCODE);
            if (result == null)
                MessageBox.Show("找不到QRcode", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, result[0]);
                notifyIcon.ShowBalloonTip(1000, "螢幕截圖工具", "結果為：" + result[0] + "\n內容已複製到剪貼簿", ToolTipIcon.None);
            }
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_SearchImage()
        {
            Bitmap bmp;
            try
            {
                bmp = GetSelectedArea();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("無法上傳", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Func_CloseScreenshot();
                return;
            }
            StringBuilder builder = new StringBuilder("https://www.google.com/searchbyimage?&image_url=");
            builder.Append(UploadImageToCloud(bmp));
            System.Diagnostics.Process.Start(builder.ToString());
            bmp.Dispose();
            Func_CloseScreenshot();
        }

        private void Func_CloseScreenshot()
        {
            toolbarWindow.Visibility = Visibility.Hidden;
            Hide();
            labelSize.Visibility = Visibility.Hidden;
            bmp_raw.Dispose();
            bmp_Background.Dispose();
        }
    }
}
