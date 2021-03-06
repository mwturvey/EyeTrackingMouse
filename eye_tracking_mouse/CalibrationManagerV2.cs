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
    class CalibrationManagerV2 : ICalibrationManager
    {
        private readonly ShiftsStorage shift_storage;
        private readonly Options.CalibrationMode calibration_mode;
        private readonly ShiftStorageCache cache;
        public CalibrationManagerV2(Options.CalibrationMode mode, bool for_testing)
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
            var avg = Helpers.GetWeightedAverage(shift_storage, closest_corrections);

            ApplyShades(closest_corrections);

            foreach (var correction in closest_corrections)
            {
                correction.weight = correction.weight / correction.distance / sum_of_reverse_distances;
            }

            Helpers.NormalizeWeights(closest_corrections);

            Debug.Assert(calibration_mode.correction_fade_out_distance > 0);


            float total_weight = 0;
            foreach (var correction in closest_corrections)
            {
                var vector_from_correction_to_cursor = correction.vector_from_correction_to_cursor;
                float XYdistance = (float) Math.Sqrt(
                    vector_from_correction_to_cursor[0] * vector_from_correction_to_cursor[0] +
                    vector_from_correction_to_cursor[1] * vector_from_correction_to_cursor[1]);

                float factor = 1.0f - (float)Math.Pow(
                    XYdistance / calibration_mode.correction_fade_out_distance, 2);
                correction.weight *= factor > 0 ? factor : 0;
                total_weight += correction.weight;
            }

            Debug.Assert(total_weight <= 1);

            var result = Helpers.GetWeightedAverage(shift_storage, closest_corrections);
            result.X = (int) (result.X + avg.X * (1 - total_weight));
            result.Y = (int) (result.Y + avg.Y * (1 - total_weight));

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

        private float GetShadeOpacity(
            ShiftStorageCache.PointInfo source_of_shade,
            ShiftStorageCache.PointInfo shaded_correction)
        {
            Debug.Assert(source_of_shade.distance <= shaded_correction.distance);

            float opacity = 1;

            float angle_in_percents =(float) (Helpers.GetAngleBetweenVectors(source_of_shade, shaded_correction) * 100 / Math.PI);
            Debug.Assert(angle_in_percents <= 100);

            // Opacity descendes gradualy in the sector between opaque and transparent sectors.
            if (angle_in_percents < calibration_mode.size_of_opaque_sector_in_percents)
                opacity = 1;
            else if (angle_in_percents > 100 - calibration_mode.size_of_transparent_sector_in_percents)
                opacity = 0;
            else
                opacity = (angle_in_percents + calibration_mode.size_of_transparent_sector_in_percents - 100) /
                    (calibration_mode.size_of_opaque_sector_in_percents + calibration_mode.size_of_transparent_sector_in_percents - 100);

            float distance_from_shade_shell_to_shaded_correction = shaded_correction.distance - source_of_shade.distance;
            if (distance_from_shade_shell_to_shaded_correction < calibration_mode.shade_thickness_in_pixels)
                opacity *= distance_from_shade_shell_to_shaded_correction / calibration_mode.shade_thickness_in_pixels;

            return opacity;
        }

        private void ApplyShades(List<ShiftStorageCache.PointInfo> corrections)
        {
            for (int i = 0; i < corrections.Count;)
            {
                corrections[i].weight = 1;

                for (int j = 0; j < i; j++)
                {
                    corrections[i].weight -= GetShadeOpacity(corrections[j], corrections[i]);
                }
                if (corrections[i].weight <= 0.01)
                    corrections.RemoveAt(i);
                else
                    i++;
            }
        }

        public void AddShift(float[] cursor_position, Point shift)
        {
            cache.ChangeCursorPosition(cursor_position);
            shift_storage.AddShift(cursor_position, shift);
        }

        public void Dispose()
        {
            shift_storage.Dispose();
        }

        public void Reset()
        {
            shift_storage.Reset();
        }

        public void SaveInDirectory(string directory_path)
        {
            shift_storage.SaveInDirectory(directory_path);
        }


        public bool IsDebugWindowEnabled
        {
            get => shift_storage.IsDebugWindowEnabled;
            set
            {
                shift_storage.IsDebugWindowEnabled = value;
            }
        }
    }
}
