﻿using System;
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

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for CalibrationSettings.xaml
    /// </summary>
    public partial class CalibrationSettings : Window
    {
        public CalibrationSettings()
        {
            InitializeComponent();

            UpdateControls();
            ignore_changes = false;
        }

        private void UpdateControls()
        {
            lock (Helpers.locker)
            {
                var key_bindings = Options.Instance.key_bindings;
                string calibration_buttons = key_bindings[Key.CalibrateUp].ToString() + "/"
                    + key_bindings[Key.CalibrateLeft].ToString() + "/"
                    + key_bindings[Key.CalibrateDown].ToString() + "/"
                    + key_bindings[Key.CalibrateRight].ToString();

                CalibrationPointsCount.ToolTip =
                    "Maximum number of arrows in CALIBRATION VIEW. \n" +
                    "Each arrow points from the cursor position BEFORE correction to the position AFTER correction.\n\n" +
                    "Suppose you want to click point A, but because of imperfect precision the cursor is at point B.\n" +
                    "You press " + calibration_buttons +" until the cursor reach point A and then you click it. \n" +
                    "This action creates an arrow pointing from B (imprecise cursor position) to A(click point after correction).\n\n" +
                    "More arrows result in better precision but longer algorithm learning and higher CPU usage.\n" +
                    "You may want to decrease Min distance between arrows if you set large Max arrows count.";

                CalibrationZoneSize.ToolTip =
                    "Minimum distance between two arrows.\n" +
                    "If you make a correction too close to an existing arrow this arrow will be rewritten.\n" +
                    "Smaller distance result in better precision but longer algorithm learning and higher CPU usage.\n" +
                    "You may want to increase Max arrows count if you make Min distance small.";

                ConsideredZonesCount.ToolTip = 
                    "Defines how many arrows will be used to calculate the resulting shift. \n" +
                    "Closer arrows have more influence on the resulting shift than farther ones.";

                UpdatePeriodMs.ToolTip =
                    "Energy saving option. The calibration algorithm will iterate once per this period of time. \n" +
                    "Bigger period results in less CPU load, but the cursor may shake.";

                CalibrationZoneSize.Value = Options.Instance.calibration_mode.zone_size;
                CalibrationPointsCount.Value = Options.Instance.calibration_mode.max_zones_count;
                ConsideredZonesCount.Value = Options.Instance.calibration_mode.considered_zones_count;
                UpdatePeriodMs.Value = Options.Instance.calibration_mode.update_period_ms;


                LeftEye.Value = Options.Instance.calibration_mode.additional_dimensions_configuration.LeftEye;
                RightEye.Value = Options.Instance.calibration_mode.additional_dimensions_configuration.RightEye;
                AngleBetweenEyes.Value = Options.Instance.calibration_mode.additional_dimensions_configuration.AngleBetweenEyes;
                HeadPosition.Value = Options.Instance.calibration_mode.additional_dimensions_configuration.HeadPosition;
                HeadDirection.Value = Options.Instance.calibration_mode.additional_dimensions_configuration.HeadDirection;
                AngleBetweenEyes.Z.Visibility = Visibility.Hidden;

                if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.MultiDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 0;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.SingleDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 1;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CustomCalibrationMode.Visibility = Visibility.Visible;
                    CalibrationModeCombo.SelectedIndex = 2;
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ignore_changes)
                return;
            lock (Helpers.locker)
            {
                if (sender == CalibrationZoneSize)
                {
                    Options.Instance.calibration_mode.zone_size = (int)CalibrationZoneSize.Value;
                }
                else if (sender == CalibrationPointsCount)
                {
                    Options.Instance.calibration_mode.max_zones_count = (int)CalibrationPointsCount.Value;
                }
                else if (sender == ConsideredZonesCount)
                {
                    Options.Instance.calibration_mode.considered_zones_count = (int)ConsideredZonesCount.Value;
                }
                else if (sender == UpdatePeriodMs) {
                    Options.Instance.calibration_mode.update_period_ms = (int)UpdatePeriodMs.Value;
                }

                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;

            }
        }

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            if (ignore_changes)
                return;

            lock (Helpers.locker)
            {
                if (sender == LeftEye)
                {
                    Options.Instance.calibration_mode.additional_dimensions_configuration.LeftEye = LeftEye.Value;
                }
                else
                if (sender == RightEye)
                {
                    Options.Instance.calibration_mode.additional_dimensions_configuration.RightEye = RightEye.Value;
                } 
                else
                if (sender == AngleBetweenEyes)
                {
                    Options.Instance.calibration_mode.additional_dimensions_configuration.AngleBetweenEyes = AngleBetweenEyes.Value;
                }
                else
                if (sender == HeadDirection)
                {
                    Options.Instance.calibration_mode.additional_dimensions_configuration.HeadDirection = HeadDirection.Value;
                }
                else
                if (sender == HeadPosition)
                {
                    Options.Instance.calibration_mode.additional_dimensions_configuration.HeadPosition = HeadPosition.Value;
                }

                Options.CalibrationMode.Changed?.Invoke(this, null);
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;
            }

        }

        private void CalibrationModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignore_changes)
                return;

            lock (Helpers.locker)
            {
                if (CalibrationModeCombo.SelectedIndex == 0)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.MultiDimensionPreset;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else if (CalibrationModeCombo.SelectedIndex == 1)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.SingleDimensionPreset;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }

                Options.CalibrationMode.Changed?.Invoke(this, null);
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;
            }
        }

        private bool ignore_changes = true;

        private void CalibrationViewButton_Click(object sender, RoutedEventArgs e)
        {
            CalibrationManager.Instance.IsDebugWindowEnabled = !CalibrationManager.Instance.IsDebugWindowEnabled;
        }
    }
}
