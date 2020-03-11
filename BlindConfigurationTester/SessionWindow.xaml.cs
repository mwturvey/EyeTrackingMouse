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

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for SessionWindow.xaml
    /// </summary>
    public partial class SessionWindow : Window
    {
        List<Tuple<int, int>> points;
        int current_index = 0;
        bool dataset_training_mode = false;

        DateTime time_when_user_started_looking_at_point = DateTime.Now;
        bool is_user_looking_at_point = false;
        int size_of_circle;

        eye_tracking_mouse.TobiiCoordinatesProvider coordinatesProvider;

        public List<DataPoint> DataPoints { get; private set; }

        public SessionWindow(List<Tuple<int, int>> points, int size_of_circle, bool dataset_training_mode)
        {
            eye_tracking_mouse.Options.Instance.calibration_mode.additional_dimensions_configuration = new eye_tracking_mouse.AdditionalDimensionsConfguration
            {
                HeadDirection = new eye_tracking_mouse.Vector3Bool { X = true, Y = true, Z = true },
                HeadPosition = new eye_tracking_mouse.Vector3Bool { X = true, Y = true, Z = true },
                LeftEye = new eye_tracking_mouse.Vector3Bool { X = true, Y = true, Z = true },
                RightEye = new eye_tracking_mouse.Vector3Bool { X = true, Y = true, Z = true },
                AngleBetweenEyes = new eye_tracking_mouse.Vector3Bool { X = true, Y = true, Z = false }
            };

            this.points = points;
            InitializeComponent();
            Circle.Width = size_of_circle;
            Circle.Height = size_of_circle;

            this.dataset_training_mode = dataset_training_mode;
            if (dataset_training_mode)
                DataPoints = new List<DataPoint>();

            this.size_of_circle = size_of_circle;

            Dispatcher.BeginInvoke((Action)(() => { NextPoint(null, null); }));
        }

        private void OnCoordinates(eye_tracking_mouse.TobiiCoordinates coordinates)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {

                Point gaze_point = new Point(coordinates.gaze_point.X, coordinates.gaze_point.Y);
                if (PresentationSource.FromVisual(Circle) == null)
                    return;

                Point location_of_point_on_screen = this.Circle.PointToScreen(new Point(size_of_circle / 2, size_of_circle / 2));

                if (Point.Subtract(gaze_point, location_of_point_on_screen).Length < 200)
                {
                    if (!is_user_looking_at_point)
                    {
                        is_user_looking_at_point = true;
                        time_when_user_started_looking_at_point = DateTime.Now;
                    }

                    if ((DateTime.Now - time_when_user_started_looking_at_point).TotalSeconds > 1)
                    {
                        eye_tracking_mouse.MouseButtons.Move(
                            (int)location_of_point_on_screen.X,
                            (int)location_of_point_on_screen.Y);

                        DataPoints.Add(new DataPoint
                        {
                            error = (Point)Point.Subtract(location_of_point_on_screen, gaze_point),
                            tobii_coordinates = coordinates
                        });

                        NextPoint(null, null);
                    }

                }
                else
                {
                    is_user_looking_at_point = false;
                }
            }));

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (current_index != points.Count)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Aborting this session will fuck up scientific objectivity!\n" +
                    "Do you really want to do that?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            coordinatesProvider?.Dispose();
        }

        public void NextPoint(object sender, EventArgs args)
        {
            if (current_index == points.Count)
            {
                Close();
                return;
            }

            Circle.SetValue(Canvas.LeftProperty, points[current_index].Item1 % (Canvas.ActualWidth - 20));
            Circle.SetValue(Canvas.TopProperty, points[current_index].Item2 % (Canvas.ActualHeight - 20));

            TextBlock_PointsLeft.Text = ++current_index + "/" + points.Count;

            if (dataset_training_mode && coordinatesProvider == null)
            {
                coordinatesProvider = new eye_tracking_mouse.TobiiCoordinatesProvider(OnCoordinates);
                coordinatesProvider.UpdateTobiiStreams(null, null);
            }
        }
    }
}
