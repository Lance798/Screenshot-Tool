using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Screenshot_Tool
{
    /// <summary>
    /// Interaction logic for ToolbarWindow.xaml
    /// </summary>
    public enum Buttons : ushort
    {
        CLOSE,
        SAVE,
        COPY,
        UPLOAD,
        PAINT,
        QRCODE
    }

    public partial class ToolbarWindow : Window
    {
        public event ButtonPressedEventHandler ButtonPressedHandler;
        public ToolbarWindow()
        {
            InitializeComponent();
        }

        private void OnQRcodeButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.QRCODE
            };
            OnButtonPressed(args);
        }

        private void OnUploadButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.UPLOAD
            };
            OnButtonPressed(args);
        }

        private void OnPaintButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.PAINT
            };
            OnButtonPressed(args);
        }

        private void OnSaveButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.SAVE
            };
            OnButtonPressed(args);
        }
        private void OnCopyButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.COPY
            };
            OnButtonPressed(args);
        }
        private void OnCloseButtonClick(Object sender, RoutedEventArgs e)
        {
            ButtonPressedEventArgs args = new ButtonPressedEventArgs()
            {
                Button = Buttons.CLOSE
            };
            OnButtonPressed(args);
        }

        private void OnButtonPressed(ButtonPressedEventArgs args)
        {
            ButtonPressedEventHandler handler = ButtonPressedHandler;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }

    public delegate void ButtonPressedEventHandler(Object sender, ButtonPressedEventArgs e);

    public class ButtonPressedEventArgs : EventArgs
    {
        public Buttons Button { get; set; }
    }
}
