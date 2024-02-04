using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TrackVisuData;
using UnityEngine;

namespace TrackVisu
{
    public class SaveAndLoad : MonoBehaviour
    {
        private const string SEPARATOR_LINE = "-----";
        internal static string ToMultiCsv(Track track, List<Trajectory> trajectories, string filepath = null)
        {
            string multi_csv = "";
            multi_csv += "\nTrack\n";
            multi_csv += track.ToCsv();
            multi_csv += SEPARATOR_LINE + "\n";

            foreach (Trajectory trajectory in trajectories)
            {
                multi_csv += $"\nTrajectory: {trajectory.name}\n";
                multi_csv += trajectory.ToCsv();
                multi_csv += SEPARATOR_LINE + "\n";
            }

            if (filepath != null)
                File.WriteAllText(filepath, multi_csv);

            return multi_csv;
        }

        internal static (Track track, List<Trajectory> trajectories) FromMultiCsv(string path_to_file_to_check)
        {
            string data = File.ReadAllText(path_to_file_to_check);

            List<List<string>> csv_lines = new List<List<string>>();

            List<string> current_lines = new List<string>();
            foreach (string line in data.Split("\n"))
            {
                string this_line = line.Trim();

                if (this_line.Length == 0)
                    continue;

                if (this_line == SEPARATOR_LINE)
                {
                    if (current_lines.Count > 0)
                        csv_lines.Add(current_lines);
                    current_lines = new List<string>();
                }
                else
                    current_lines.Add(this_line);
            }
            if (current_lines.Count > 0)
                csv_lines.Add(current_lines);


            var track_lines = csv_lines[0];
            if (track_lines[0] != "Track")
                Debug.LogError("Malformed track! First line was not 'Track' but: " + track_lines[0]);
            track_lines.RemoveAt(0); // remove the "Track" info line added by the multi-csv command
            Track track = Track.FromCsvLines(track_lines);


            List<Trajectory> trajectories = new List<Trajectory>();

            for (int i = 1; i < csv_lines.Count; i++)
            {
                var vehicle_lines = csv_lines[i];
                if (!vehicle_lines[0].StartsWith("Trajectory:"))
                    Debug.LogError("Malformed trajectory!");
                string name = vehicle_lines[0].Replace("Trajectory: ", "");
                name = name.Replace("___6", "_").Replace("9___", "");
                vehicle_lines.RemoveAt(0); // remove the "Trajectory:" info line added by the multi-csv command

                trajectories.Add(Trajectory.FromCsvLines(name, vehicle_lines));
            }

            return (track, trajectories);
        }
    }
}