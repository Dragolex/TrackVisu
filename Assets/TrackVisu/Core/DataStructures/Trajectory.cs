using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrackVisuUtils;
using UnityEngine;

namespace TrackVisuData
{
    public class Trajectory
    {
        public struct TrajectoryKeyframe
        {
            public float time;
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
        }

        public string name { get; private set; } = "";
        public int Length { get => trajectory.Count; private set { } }

        private List<TrajectoryKeyframe> trajectory = new List<TrajectoryKeyframe>();

        public Trajectory(string name)
        {
            this.name = name;
        }

        public void AddKeyframe(float time, Vector3 position, Vector3 velocity, Quaternion rotation)
        {
            trajectory.Add(new TrajectoryKeyframe() { time = time, position = position, velocity = velocity, rotation = rotation });
        }

        internal string ToCsv()
        {
            Csv csv = new Csv(11, (uint)trajectory.Count + 1);

            csv.NextColumn("t");
            csv.NextColumn("x");
            csv.NextColumn("y");
            csv.NextColumn("z");
            csv.NextColumn("vx");
            csv.NextColumn("vy");
            csv.NextColumn("vz");
            csv.NextColumn("rx");
            csv.NextColumn("ry");
            csv.NextColumn("rz");
            csv.NextColumn("rw");
            csv.NextRow();

            foreach (TrajectoryKeyframe frame in trajectory)
            {
                csv.NextColumn(frame.time);

                csv.NextColumn(frame.position.x);
                csv.NextColumn(frame.position.y);
                csv.NextColumn(frame.position.z);

                csv.NextColumn(frame.velocity.x);
                csv.NextColumn(frame.velocity.y);
                csv.NextColumn(frame.velocity.z);

                csv.NextColumn(frame.rotation.x);
                csv.NextColumn(frame.rotation.y);
                csv.NextColumn(frame.rotation.z);
                csv.NextColumn(frame.rotation.w);

                csv.NextRow();
            }

            return csv.ToString();
        }

        internal static Trajectory FromCsvLines(string name, List<string> lines)
        {
            lines.RemoveAt(0); // skip the header line

            Trajectory trajectory = new Trajectory(name);
            foreach (string line in lines)
            {
                string[] vals = line.Split(",");


                float time = SafeParse.Float(vals[0]);
                Vector3 position = new Vector3(
                            SafeParse.Float(vals[1]),
                            SafeParse.Float(vals[2]),
                            SafeParse.Float(vals[3]));
                Vector3 velocity = new Vector3(
                            SafeParse.Float(vals[4]),
                            SafeParse.Float(vals[5]),
                            SafeParse.Float(vals[6]));

                Quaternion orientation = Quaternion.identity;

                if (vals.Length == 11)
                {
                    // Variant A: T, Pos, Speed, Rotation(quaternion)
                    orientation = new Quaternion(
                        SafeParse.Float(vals[7]),
                        SafeParse.Float(vals[8]),
                        SafeParse.Float(vals[9]),
                        SafeParse.Float(vals[10]));
                }
                if (vals.Length == 12)
                {
                    // Variant B: T, Pos, Speed, Acceleration (omitted), Heading, Yaw

                    float heading = SafeParse.Float(vals[10]);
                    float yaw = SafeParse.Float(vals[11]);
                    var sys_quat = System.Numerics.Quaternion.CreateFromYawPitchRoll(yaw, heading, 0);
                    orientation = new Quaternion(sys_quat.X, sys_quat.Y, sys_quat.Z, sys_quat.W);
                }

                trajectory.AddKeyframe(time, position, velocity, orientation);
            }

            return trajectory;
        }

        internal float GetEndTime()
        {
            if (trajectory.Count == 0)
                return 0;
            return trajectory.Last().time;
        }

        internal bool Interpolate(float time_position, out Vector3 pos, out Vector3 velocity, out Quaternion rotation)
        {
            pos = Vector3.zero;
            velocity = Vector3.zero;
            rotation = Quaternion.identity;

            if (trajectory.Count == 0)
                return false;

            float ratio = GetRatioInKeyframeFromTimePosition(time_position, out int frame_index, out TrajectoryKeyframe last_frame, out TrajectoryKeyframe keyframe);

            if (frame_index < trajectory.Count)
            {
                pos = last_frame.position.LaneLerpTo(keyframe.position, ratio);

                rotation = Quaternion.Lerp(last_frame.rotation, keyframe.rotation, ratio);
                velocity = Vector3.Lerp(last_frame.velocity, keyframe.velocity, ratio);

                // Interpolate for lanechange
                if (last_frame.position.y != keyframe.position.y)
                {
                    bool left = last_frame.position.y < keyframe.position.y;
                    Quaternion angled_rotation = rotation * Quaternion.Euler(0, 0, left ? 20 : -20);

                    if (ratio <= 0.5f)
                        rotation = Quaternion.Lerp(last_frame.rotation, angled_rotation, Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, ratio * 2)));
                    else
                        rotation = Quaternion.Lerp(angled_rotation, keyframe.rotation, Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, (ratio - 0.5f) * 2)));
                }

                return true;
            }

            return false;
        }

        public float GetRatioInKeyframeFromTimePosition(float time_position, out int current_frame_index, out TrajectoryKeyframe last_frame, out TrajectoryKeyframe current_keyframe)
        {
            last_frame = trajectory[0];
            current_keyframe = last_frame;
            current_frame_index = 0;
            foreach (TrajectoryKeyframe keyframe in trajectory)
            {
                if (keyframe.time > time_position)
                {
                    current_keyframe = keyframe;

                    float time_diff_to_target = time_position - last_frame.time;
                    float time_diff_to_next = keyframe.time - last_frame.time;
                    return time_diff_to_target / time_diff_to_next;
                }

                current_frame_index++;
                last_frame = keyframe;
            }

            current_frame_index = trajectory.Count;
            return 1.0f;
        }
    }
}