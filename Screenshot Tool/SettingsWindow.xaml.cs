using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Screenshot_Tool
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            TextBox_HotKey.Text = Properties.Settings.Default.HotKey;
            TextBox_FolderPath.Text = Properties.Settings.Default.SavePath;
            CheckBox_RunOnBoot.IsChecked = Properties.Settings.Default.RunOnBoot;
        }

        private void TextBox_Hot_Key_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            string str = e.Key.ToString();

            if (Keyboard.Modifiers.ToString() != "None" && !(Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.LeftCtrl)
                && !(Keyboard.IsKeyDown(Key.RightCtrl) && e.Key == Key.RightCtrl)
                && !(Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.LeftShift)
                && !(Keyboard.IsKeyDown(Key.RightShift) && e.Key == Key.RightShift)
                && !(Keyboard.IsKeyDown(Key.LeftAlt) && e.Key == Key.LeftAlt)
                && !(Keyboard.IsKeyDown(Key.RightAlt) && e.Key == Key.RightAlt))
                str += " + " + Keyboard.Modifiers.ToString().Replace("Control", "Ctrl");

            TextBox_HotKey.Text = str;
            Properties.Settings.Default.HotKey = str;
        }
        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            TextBox_FolderPath.Text = dialog.SelectedPath;
            Properties.Settings.Default.SavePath = dialog.SelectedPath;
        }

        private void CheckBox_RunOnBoot_OnChange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RunOnBoot = (bool)CheckBox_RunOnBoot.IsChecked;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            MessageBox.Show("請重開程式讓設定生效");
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            Close();
        }
    }
}
