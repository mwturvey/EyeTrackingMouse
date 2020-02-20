﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;

namespace BlindConfigurationTester
{
    class  Utils
    {
        public static bool IsApplicationOpen()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.ProcessName == eye_tracking_mouse.Helpers.application_name)
                    return true;
            }
            return false;
        }

        public static bool TryCloseApplication()
        {
            while (IsApplicationOpen())
            {
                var result = MessageBox.Show(
                    "Close " + eye_tracking_mouse.Helpers.application_name + " to proceed.", 
                    "EyeTrackerMouse is running!", 
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                    return false;
            }

            return true;
        }

        public static string[] GetConfigurationsList()
        {
            try
            {
                string[] dirs = Directory.GetDirectories(Path.Combine(eye_tracking_mouse.Helpers.AppFolder, "Configurations"));
                for (int i = 0; i < dirs.Length; i++)
                {
                    dirs[i] = Path.GetFileName(dirs[i]);
                }
                return dirs;
            }catch(Exception)
            {
                return new string[0]; 
            }
        }


        private  static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        public static void CopyDir(string source, string destination)
        {
            // C# is fucking great!
            new Microsoft.VisualBasic.Devices.Computer().
                FileSystem.CopyDirectory(source, destination);
        }

        public static string GetConfigurationDir(string configuration)
        {
            if (configuration == null)
                return eye_tracking_mouse.Helpers.UserDataFolder;
            return Path.Combine(eye_tracking_mouse.Helpers.AppFolder, "Configurations", configuration);
        }

        public static void SaveToUserData(string configuration)
        {
            if (configuration == null)
                return;
            string configuration_path = GetConfigurationDir(configuration);
            if (!Directory.Exists(configuration_path))
            {
                MessageBox.Show("Configuration " + configuration + " doesn't exist!");
                return;
            }

            if (!TryCloseApplication())
                return;

            Directory.Delete(eye_tracking_mouse.Helpers.UserDataFolder, true);
            CopyDir(configuration_path, eye_tracking_mouse.Helpers.UserDataFolder);
        }

        public static void LoadFromUserData(string configuration)
        {
            string configuration_path = GetConfigurationDir(configuration);
            if (configuration_path == eye_tracking_mouse.Helpers.UserDataFolder)
                return;
            if (!TryCloseApplication())
                return;
            Directory.Delete(configuration_path, true);
            CopyDir(eye_tracking_mouse.Helpers.UserDataFolder, configuration_path);
        }

        private static void RunAppFromUserData(bool save_progress, Action before_start, Action after_start)
        {
            var user_data_backup = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (!save_progress)
            {
                if (!TryCloseApplication())
                    return;
                CopyDir(eye_tracking_mouse.Helpers.UserDataFolder, user_data_backup);
            }


            before_start?.Invoke();

            if (!IsApplicationOpen())
                Process.Start(@"D:\projects\EyeTrackingMouse\bin\x86\Release\EyeTrackingMouse.exe");

            after_start?.Invoke();

            if (!save_progress)
            {
                while (!TryCloseApplication());
                Directory.Delete(eye_tracking_mouse.Helpers.UserDataFolder, true);
                Directory.Move(user_data_backup, eye_tracking_mouse.Helpers.UserDataFolder);
            }
        }

        public static void RunApp(string configuration, bool save_progress, Action before_start, Action after_start)
        {
            if (configuration == null)
            {
                RunAppFromUserData(save_progress, before_start, after_start);
                return;
            }

            string configuration_path = GetConfigurationDir(configuration);
            if (!Directory.Exists(configuration_path))
            {
                MessageBox.Show("Configuration " + configuration + " doesn't exist!");
                return;
            }

            if (!TryCloseApplication())
                return;

            var user_data_backup = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.Move(eye_tracking_mouse.Helpers.UserDataFolder, user_data_backup);
            if (save_progress)
                Directory.Move(configuration_path, eye_tracking_mouse.Helpers.UserDataFolder);
            else
                CopyDir(configuration_path, eye_tracking_mouse.Helpers.UserDataFolder);

            before_start?.Invoke();
            Process.Start(@"D:\projects\EyeTrackingMouse\bin\x86\Release\EyeTrackingMouse.exe");
            after_start?.Invoke();

            while (!TryCloseApplication());
            if (save_progress)
                Directory.Move(eye_tracking_mouse.Helpers.UserDataFolder, configuration_path);
            else
                Directory.Delete(eye_tracking_mouse.Helpers.UserDataFolder, true);
            Directory.Move(user_data_backup, eye_tracking_mouse.Helpers.UserDataFolder);
        }

        public static void CreateConfiguration(string configuration)
        {
            string configuration_path = GetConfigurationDir(configuration);
            if (!Directory.Exists(configuration_path))
                Directory.CreateDirectory(configuration_path);
        }

        public static void RemoveConfiguration(string configuration)
        {
            string configuration_path = GetConfigurationDir(configuration);
            if (configuration == null && !TryCloseApplication())
                return;

            if (Directory.Exists(configuration_path))
                Directory.Delete(configuration_path, true);
        }
    }
}