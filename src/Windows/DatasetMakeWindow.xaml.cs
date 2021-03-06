﻿using Darknet.Dataset.Merger.Model;
using Darknet.Dataset.Merger.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
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
        IntPtr m_hhook;
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
        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);
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
            m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
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
                border.SetSelectedBBox(null);
            }
        }


        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftAlt ||
                key == Key.RightAlt)
            {
                _keyMode &= ~KeyMode.Alt;
            }
            if (key == Key.LeftShift ||
                key == Key.RightShift)
            {
                _keyMode &= ~KeyMode.Shift;
            }
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl)
            {
                _keyMode &= ~KeyMode.Ctrl;
            }
            border.SetKeyMode(_keyMode);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftAlt ||
                 key == Key.RightAlt)
            {
                _keyMode |= KeyMode.Alt;
            }
            if (key == Key.LeftShift ||
                key == Key.RightShift)
            {
                _keyMode |= KeyMode.Shift;
            }
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl)
            {
                _keyMode |= KeyMode.Ctrl;
            }
            border.SetKeyMode(_keyMode);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UnhookWinEvent(m_hhook);
        }

        private void lbBboxes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0)
            {
                var a = e.AddedItems[0] as Annotation;
                if (a.Width > 0 && a.Height > 0)
                {
                    border.SetSelectedBBox(a);
                }
                else
                {
                    border.SetSelectedBBox(null);
                }
            }
        }

        private void ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.Delete)
            {
                (this.DataContext as MakeDatasetContext).RemoveImageCommand.Execute(null);
            }
        }
    }
}
