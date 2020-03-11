﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BlindConfigurationTester
{
    public class DataSet
    {
        public int number_of_completed_sessions;

        [JsonIgnore]
        public string name;
        public List<Session> sessions = new List<Session> { new Session {
            points_count = 50, size_of_circle = 6, instructions = "This text will be shown to user before session." } };

        [JsonIgnore]
        List<DataPoint> data_points = new List<DataPoint>();

        public DataSet(string name_value)
        {
            this.name = name_value;
        }

        public static string DataSetsFolder { get { return Path.Combine(Utils.DataFolder, "DataSets"); } }
        [JsonIgnore]
        public string DataSetResultsFolder { get { return GetDataSetFolder(name); } }

        public static string GetDataSetFolder(string study_name)
        {
            return Path.Combine(DataSetsFolder, study_name);
        }

        public static DataSet Load(string data_set_name)
        {
            string json_path = Path.Combine(GetDataSetFolder(data_set_name), "config.json");
            string dataset_path = Path.Combine(GetDataSetFolder(data_set_name), "data_set.json");
            if (File.Exists(json_path))
            {
                while (true)
                {
                    try
                    {
                        var data_set = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(json_path));
                        data_set.name = data_set_name;
                        if (File.Exists(dataset_path))
                            data_set.data_points = JsonConvert.DeserializeObject<List<DataPoint>>(File.ReadAllText(dataset_path));
                        return data_set;
                    }
                    catch (IOException)
                    {
                    }
                    catch
                    {
                        return null;
                    }
                }
            }


            return null;
        }

        public string GetInfo()
        {
            string info =
               "Study " + name + "\n" +
               "Number of completed sessions: " + number_of_completed_sessions + "\n";

            return info;
        }

        public void SaveToFile()
        {
            if (!Directory.Exists(DataSetResultsFolder))
                Directory.CreateDirectory(DataSetResultsFolder);
            File.WriteAllText(Path.Combine(DataSetResultsFolder, "config.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
            File.WriteAllText(Path.Combine(DataSetResultsFolder, "data_set.json"), JsonConvert.SerializeObject(data_points, Formatting.None));
        }

        internal void StartTrainingSession()
        {
            if (number_of_completed_sessions >= sessions.Count)
            {
                MessageBox.Show("All sessions are finished");
                return;
            }

            Session session = sessions[number_of_completed_sessions];
            int session_seed = unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId);
            var new_data_points = session.Start(true, session_seed);
            data_points.InsertRange(data_points.Count, new_data_points);
            number_of_completed_sessions++;
            SaveToFile();
        }
    }
}
