﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{

    // Takes average of closest corrections.
    class CalibrationManagerV0 : ICalibrationManager
    {
        private readonly ShiftsStorage shift_storage;
        private readonly Options.CalibrationMode calibration_mode;
        private readonly ShiftStorageCache cache;
        public CalibrationManagerV0(Options.CalibrationMode mode, bool for_testing)
        {
            calibration_mode = mode;
            cache = new ShiftStorageCache(mode);
            shift_storage = new ShiftsStorage(calibration_mode, cache, for_testing);
        }

        public Point GetShift(float[] cursor_position)
        {
            cache.ChangeCursorPosition(cursor_position);
            shift_storage.calibration_window?.OnCursorPositionUpdate(cursor_position);

            var closest_corrections = cache.ClosestPoints;
            if (closest_corrections == null)
            {
                Debug.Assert(shift_storage.Corrections.Count() == 0);
                return new Point(0, 0);
            }

            float sum_of_reverse_distances = 0;
            foreach (var index in closest_corrections)
            {
                sum_of_reverse_distances += (1 / index.distance);
            }

            foreach (var correction in closest_corrections)
            {
                correction.weight = 1 / correction.distance / sum_of_reverse_distances;
            }

            var result = Helpers.GetWeightedAverage(shift_storage, closest_corrections);

            if (shift_storage.calibration_window != null)
            {
                var lables = new List<Tuple<string /*text*/, int /*correction index*/>>();
                foreach (var correction in closest_corrections)
                    lables.Add(new Tuple<string, int>((int)(correction.weight * 100) + "%", correction.index));
                shift_storage.calibration_window.UpdateCorrectionsLables(lables);
                shift_storage.calibration_window.UpdateCurrentCorrection(new UserCorrection(cursor_position, result));
            }

            return result;
        }

        public void AddShift(float[] coordinates, Point shift)
        {
            cache.ChangeCursorPosition(coordinates);
            shift_storage.AddShift(coordinates, shift);
        }

        public void Dispose()
        {
            shift_storage.Dispose();
        }

        public void Reset()
        {
            shift_storage.Reset();
        }

        public bool IsDebugWindowEnabled
        {
            get => shift_storage.IsDebugWindowEnabled;
            set => shift_storage.IsDebugWindowEnabled = value;
        }

        public void SaveInDirectory(string directory_path)
        {

            shift_storage.SaveInDirectory(directory_path);
        }

    }
}
