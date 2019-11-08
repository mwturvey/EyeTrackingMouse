﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for CalibrationWindow.xaml
    /// </summary>
    public partial class CalibrationWindow : Window
    {

        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        private readonly List<Petzold.Media2D.ArrowLine> arrows = new List<Petzold.Media2D.ArrowLine>();

        private void OnShiftStorageCahnged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lock (Helpers.locker)
                {
                    foreach (var arrow in arrows)
                    {
                        Canvas.Children.Remove(arrow);
                    }

                    arrows.Clear();

                    foreach (var shift in ShiftsStorage.Instance.shifts)
                    {
                        var arrow = new Petzold.Media2D.ArrowLine();
                        arrow.X1 = shift.Item1.X;
                        arrow.Y1 = shift.Item1.Y;
                        arrow.X2 = shift.Item1.X + shift.Item2.X;
                        arrow.Y2 = shift.Item1.Y + shift.Item2.Y;
                        arrow.Stroke = Brushes.Red;
                        arrow.StrokeThickness = 3;

                        Canvas.Children.Add(arrow);
                        arrows.Add(arrow);
                    }
                }
            }));
        }

        public CalibrationWindow()
        {
            InitializeComponent();
            Description.Text =
                "CALIBRATIONS COUNT: " + ShiftsStorage.Instance.shifts.Count + "/" + Options.Instance.calibration_max_zones_count + " \n" +
                "HIDE CALIBRATION VIEW: " + Options.Instance.key_bindings[Key.Modifier].ToString().ToUpper() + "+" + Options.Instance.key_bindings[Key.ShowCalibrationView] + "\n" +
                "YOU CAN RESET CALIBRATIONS VIA TRAY ICON MENU";

            OnShiftStorageCahnged(null, null);
            ShiftsStorage.Instance.Changed += OnShiftStorageCahnged;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }
    }
}
