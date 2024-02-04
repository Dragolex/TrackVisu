using System.Collections;
using System.Collections.Generic;
using TrackVisuData;
using UnityEngine;
using VehicleVisu;

namespace TrackVisu
{
    public class Visualizer : MonoBehaviour
    {
        [SerializeField] List<TrackVisualizer> track_visus;
        [SerializeField] VehicleController vehicle_controller;
        [SerializeField] float lane_width_meter = 3.5f;

        [Header("View Settings")]
        [SerializeField] Vector3 track_scale = Vector3.one;
        [SerializeField] bool from_ratio = false;
        [SerializeField] float height_to_width_ratio = 0.115f;
        [SerializeField] Camera camera_obj;
        [SerializeField] float camera_size_factor = 0.75f;


        public float current_track_duration { get; private set; } = 0f;

        internal List<Driver> Setup(Track current_track, List<Trajectory> current_trajectories, int random_seed)
        {
            Random.InitState(random_seed);

            if (from_ratio)
            {
                float width = current_track.GetWidth(lane_width_meter);
                float length = current_track.GetLength();

                float x_scale = ((width * (1f / height_to_width_ratio)) / length);
                track_scale = new Vector3(x_scale, 1f, 1f);

                camera_obj.orthographicSize = camera_size_factor * width;
                camera_obj.transform.position = new Vector3(track_scale.x * length * 0.5f, width * (0.55f - height_to_width_ratio), -10);
            }

            foreach (TrackVisualizer track_visu in track_visus)
                track_visu.Realize(current_track, lane_width_meter, track_scale);

            vehicle_controller.ClearTrajectories();

            List<Driver> drivers = new List<Driver>();
            current_track_duration = 0;
            foreach (Trajectory trajectory in current_trajectories)
            {
                bool is_ego = trajectory == current_trajectories[0]; // first one is ego
                current_track_duration = Mathf.Max(current_track_duration, trajectory.GetEndTime());
                Driver driver = vehicle_controller.PrepareTrajectory(trajectory, is_ego, track_scale);
                drivers.Add(driver);
                if (is_ego)
                    Controller.SetCameraTarget(driver);
            }
            vehicle_controller.ApplyVisuMode3D();

            return drivers;
        }

        internal void UpdateTime(float time_position)
        {
            vehicle_controller.UpdateVehicles(Mathf.Min(time_position, current_track_duration), track_scale);
        }
    }
}