using System.Collections;
using System.Collections.Generic;
using TrackVisuData;
using UnityEngine;

namespace TrackVisu
{
    public class RandomTrackGenerator : MonoBehaviour
    {
        [Header("Sizes")]
        [SerializeField] float segment_length_meter = 25;
        [SerializeField] float lane_width_meter = 3.5f;

        [Header("Probabilities (Each track segment)")]
        [SerializeField] float offset_change_probability = 0.15f;
        [SerializeField] float lanes_count_change_probability = 0.15f;

        [Header("Probabilities (Each trajectory second)")]
        [SerializeField] float vehicle_lane_change_probability = 0.015f;
        [SerializeField] float vehicle_velocity_change_probability = 0.025f;

        [Header("Generation")]
        public uint vehicles = 3;
        public uint segments = 50;
        public uint min_continuous_lane_segments = 4;
        public uint min_continuous_lane_following = 4;
        public int seed = -1;
        public float trajectory_interval_s = 2f;

        [Header("Velocities")]
        public float min_velocity_mps = 40 / 3.6f;
        public float max_velocity_mps = 80 / 3.6f;
        public float velocity_change_mps = 10 / 3.6f; // changes by random chance


        [Header("Lanes")]
        public uint min_lanes = 1;
        public uint max_lanes = 4;
        public uint max_lanes_offset = 2;


        private const int EGO_IDX = 0;

        internal (Track track, List<Trajectory> trajectories) Generate()
        {
            if (seed == -1)
                Random.InitState((int)System.DateTime.Now.Ticks);
            else
                Random.InitState(seed);

            Track track = GenerateTrack();
            List<Trajectory> trajectories = GenerateTrajectories(track);

            return (track, trajectories);
        }

        private Track GenerateTrack()
        {
            Track track = new Track();

            uint current_offset = (uint)Random.Range(0, max_lanes_offset);
            uint current_lanes = (uint)Random.Range(min_lanes, max_lanes);
            float longitude = 0;

            uint last_altered_lane_countdown = min_continuous_lane_segments;
            for (int seg = 0; seg < segments; seg++)
            {
                if (last_altered_lane_countdown == 0)
                {
                    bool altered_lane = false;
                    current_offset = ChangeProbabilistically(current_offset, 0, max_lanes_offset, offset_change_probability, ref altered_lane);
                    current_lanes = ChangeProbabilistically(current_lanes, min_lanes, max_lanes, lanes_count_change_probability, ref altered_lane);
                    if (altered_lane)
                        last_altered_lane_countdown = min_continuous_lane_segments;
                }
                else
                    last_altered_lane_countdown--;

                track.AddSegment(longitude, current_offset, current_lanes);
                longitude += segment_length_meter;
            }

            return track;
        }


        struct VehicleState
        {
            public float pos; // longitudinal distance to zero (start);
            public float velocity;
            public uint lane;

            public void AddAsKeyframe(Trajectory trajectory, float time, float lane_width_meter)
            {
                trajectory.AddKeyframe(
                    time,
                    new Vector3(x: pos, y: lane * lane_width_meter),
                    new Vector3(x: velocity, y: 0),
                    Quaternion.identity);
            }
        }
        private List<Trajectory> GenerateTrajectories(Track track)
        {
            // Create the initial state of all vehicles
            List<VehicleState> current_vehicles = CreateInitialVehicleStates(track);
            List<Trajectory> trajectories = CreateInitialTrajectories(current_vehicles);

            float track_length_meter = track.track.Count * segment_length_meter;
            float time = 0;

            uint[] last_altered_lane = new uint[vehicles];
            for (uint vehicle_ind = 0; vehicle_ind < vehicles; vehicle_ind++)
                last_altered_lane[vehicle_ind] = min_continuous_lane_following;

            // Each loop is assumed to have passed one trajectory_interval_s
            while (current_vehicles[EGO_IDX].pos < track_length_meter) // while ego hasn't reached the end yet
            {
                time += trajectory_interval_s;

                for (int vehicle_ind = 0; vehicle_ind < vehicles; vehicle_ind++)
                {
                    VehicleState state = current_vehicles[vehicle_ind];
                    state.velocity = ChangeProbabilistically(state.velocity, min_velocity_mps, max_velocity_mps, vehicle_velocity_change_probability, velocity_change_mps);
                    state.pos += state.velocity * trajectory_interval_s; // since every loop has trajectory_interval_s seconds passed

                    var next_segment = track.GetNextSegmentAt(state.pos);

                    if (last_altered_lane[vehicle_ind] == 0)
                    {
                        bool altered_lane = false;
                        state.lane = ChangeProbabilistically(state.lane, next_segment.offset, next_segment.offset + next_segment.lanes - 1, vehicle_lane_change_probability, ref altered_lane);
                        if (altered_lane)
                            last_altered_lane[vehicle_ind] = min_continuous_lane_following;
                    }
                    else
                        last_altered_lane[vehicle_ind]--;

                    state.lane = (uint)Mathf.Min(Mathf.Max(state.lane, next_segment.offset), next_segment.offset + next_segment.lanes - 1);

                    state.AddAsKeyframe(trajectories[vehicle_ind], time, lane_width_meter);

                    current_vehicles[vehicle_ind] = state;
                }
            }

            return trajectories;
        }

        private List<Trajectory> CreateInitialTrajectories(List<VehicleState> current_vehicles)
        {
            List<Trajectory> trajectories = new List<Trajectory>();

            Trajectory ego_trajectory = new Trajectory("Ego");
            current_vehicles[EGO_IDX].AddAsKeyframe(ego_trajectory, 0f, lane_width_meter); // add initial keyframe
            trajectories.Add(ego_trajectory); // ego at index 0

            for (int i = 1; i < current_vehicles.Count; i++)
            {
                Trajectory trajectory = new Trajectory($"Traffic_{i}");
                current_vehicles[i].AddAsKeyframe(trajectory, 0f, lane_width_meter); // add initial keyframe
                trajectories.Add(trajectory);
            }

            return trajectories;
        }

        private List<VehicleState> CreateInitialVehicleStates(Track track)
        {
            float track_length_meter = track.track.Count * segment_length_meter;

            List<VehicleState> current_vehicles = new List<VehicleState>();
            for (int vehicle_ind = 0; vehicle_ind < vehicles; vehicle_ind++)
            {
                float longitudinal_position = Random.Range(-0.5f * track_length_meter, 0.5f * track_length_meter);
                var start_segment = track.GetNextSegmentAt(longitudinal_position);

                current_vehicles.Add(new VehicleState()
                {
                    pos = longitudinal_position,
                    velocity = Random.Range(min_velocity_mps, max_velocity_mps),
                    lane = (uint)Random.Range((int)start_segment.offset, (int)(start_segment.offset + start_segment.lanes))
                });
            }

            VehicleState ego = current_vehicles[EGO_IDX];
            ego.pos = 0; // ego always start at 0
            current_vehicles[EGO_IDX] = ego;

            return current_vehicles;
        }

        private uint ChangeProbabilistically(uint current, uint min, uint max, float probability, ref bool altered_value)
        {
            if (Random.value < probability)
            {
                altered_value = true;

                if (current == min)
                    return current + 1;
                if (current == max)
                    return current - 1;

                return current + (uint)((Random.value < 0.5f) ? -1 : 1);
            }
            return current;
        }
        private float ChangeProbabilistically(float current, float min, float max, float probability, float ammount)
        {
            if (Random.value < probability)
            {
                if (current < (min + ammount))
                    return current + ammount;
                if (current > (max - ammount))
                    return current - ammount;

                return current + ((Random.value < 0.5f) ? -ammount : ammount);
            }
            return current;
        }
    }
}