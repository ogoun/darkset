using Darknet.Dataset.Merger.Model;
using Darknet.Dataset.Merger.ViewModel;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Darknet.Dataset.Merger.Windows
{
    /// <summary>
    /// Interaction logic for DatasetMakeWindow.xaml
    /// </summary>
    public partial class DatasetMakeWindow : Window
    {
        private readonly string _folder;
        private KeyMode _keyMode = KeyMode.None;

        #region Dirty hack
        WinEventDelegate dele = null;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();
            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        /// <summary>
        /// This hack allows to disable keymode when opened new window over current with pressed ctrl, alt or shift
        /// </summary>
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var title = GetActiveWindowTitle();
            if (!string.Equals(title, this.Title))
            {
                _keyMode = KeyMode.None;
                border.SetKeyMode(_keyMode);
            }
        }
        #endregion 

        public DatasetMakeWindow(string folderPath)
        {
            InitializeComponent();
            _folder = folderPath;
            this.DataContext = new MakeDatasetContext();
            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
        }        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                (this.DataContext as MakeDatasetContext).SetFolder(_folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.DialogResult = false;
                Close();
            }
            border.OnBBoxesChanged += Border_OnBBoxesChanged;
        }

        private void Border_OnBBoxesChanged()
        {
            (this.DataContext as MakeDatasetContext).UpdateBBoxes();
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0)
            {
                (this.DataContext as MakeDatasetContext).SetCurrentImage(e.AddedItems[0] as ImageInfo);
            }
        }


        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt ||
                e.Key == Key.RightAlt)
            {
                _keyMode &= ~KeyMode.Alt;
            }
            if (e.Key == Key.LeftShift ||
                e.Key == Key.RightShift)
            {
                _keyMode &= ~KeyMode.Shift;
            }
            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.RightCtrl)
            {
                _keyMode &= ~KeyMode.Ctrl;
            }
            border.SetKeyMode(_keyMode);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt ||
                 e.Key == Key.RightAlt)
            {
                _keyMode |= KeyMode.Alt;
            }
            if (e.Key == Key.LeftShift ||
                e.Key == Key.RightShift)
            {
                _keyMode |= KeyMode.Shift;
            }
            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.RightCtrl)
            {
                _keyMode |= KeyMode.Ctrl;
            }
            border.SetKeyMode(_keyMode);
        }
    }
}
