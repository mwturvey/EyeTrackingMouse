﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTestRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            var caibration_mode = BlindConfigurationTester.Helpers.GetCalibrationMode(null);
            var calibration_manager = BlindConfigurationTester.Helpers.SetupCalibrationManager(caibration_mode);
            var data_points = BlindConfigurationTester.DataSet.Load("0roman").data_points;

            int avg_time;
            var result = BlindConfigurationTester.Helpers.RunPerfTest(
                calibration_manager,
                data_points,
                caibration_mode.additional_dimensions_configuration,
                out avg_time);

            if (args.Length < 1 || args[0] == "time")
            {
                Console.WriteLine(avg_time);
            }
            else if (args[0] == "utility")
            {
                Console.WriteLine(result.UtilityFunction);
            }
            else
            {
                Console.Error.WriteLine("allowed arguments are 'time' and 'utility'");
                return -1;
            }
            return 0;
        }
    }
}
