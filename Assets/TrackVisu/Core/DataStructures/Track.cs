using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrackVisuUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace TrackVisuData
{
    public class Track
    {
        public struct TrackSegment
        {
            public float longitude;
            public uint offset;
            public uint lanes;

            internal bool ContainsLane(int lane_index)
            {
                return lane_index >= offset && lane_index < offset + lanes;
            }
        }

        public List<TrackSegment> track = new List<TrackSegment>();

        public void AddSegment(float longitude, uint offset, uint lanes)
        {
            track.Add(new TrackSegment() { longitude = longitude, offset = offset, lanes = lanes });
        }

        internal string ToCsv()
        {
            Csv csv = new Csv(3, (uint)track.Count + 1);

            csv.NextColumn("Longitude");
            csv.NextColumn("Offset");
            csv.NextColumn("Lanes");

            csv.NextRow();

            foreach (TrackSegment segment in track)
            {
                csv.NextColumn(segment.longitude);
                csv.NextColumn(segment.offset);
                csv.NextColumn(segment.lanes);
                csv.NextRow();
            }

            return csv.ToString();
        }

        internal TrackSegment GetNextSegmentAt(float longitudinal_position)
        {
            foreach (TrackSegment segment in track)
                if (segment.longitude > longitudinal_position)
                    return segment;
            return track.Last();
        }

        internal static Track FromCsvLines(List<string> lines)
        {
            lines.RemoveAt(0); // skip the header line

            Track track = new Track();
            foreach (string line in lines)
            {
                string[] vals = line.Split(",");
                //for (int i = 0; i < vals.Length; i++)
                //    vals[i] = vals[i].Trim();

                track.AddSegment(
                    SafeParse.Float(vals[0]),
                    SafeParse.Uint(vals[1]),
                    SafeParse.Uint(vals[2])
                    );
            }

            return track;
        }


        public struct LanePortionPoint
        {
            // NOTE: lane_index starts from most right lane!
            internal LanePortionPoint(Vector3 position, TrackSegment segment, int lane_index)
            {
                uint most_left_lane = segment.offset + segment.lanes - 1;

                pt = position;
                nothing_on_right = lane_index <= segment.offset;
                nothing_on_left = lane_index >= (most_left_lane);
                road_on_right = lane_index > (segment.offset) && lane_index <= most_left_lane;
                road_on_left = lane_index >= segment.offset && lane_index < most_left_lane;
            }

            public Vector3 pt;
            public bool nothing_on_right;
            public bool nothing_on_left;
            public bool road_on_right;
            public bool road_on_left;
        }

        // A lane portion is a part of a lane that has no interruption
        internal List<LanePortionPoint[]> GetLanePortionPoints(float lane_width, Vector3 track_scale)
        {
            List<LanePortionPoint[]> lane_portions = new List<LanePortionPoint[]>();

            uint max_lane_index = 0;
            foreach (TrackSegment segment in track)
                max_lane_index = Math.Max(max_lane_index, segment.offset + segment.lanes);

            for (int lane_index = 0; lane_index <= max_lane_index; lane_index++)
            {
                List<LanePortionPoint> lane_points = new List<LanePortionPoint>();

                TrackSegment last_segment = new TrackSegment();
                foreach (TrackSegment segment in track)
                {
                    if (segment.ContainsLane(lane_index))
                    {
                        lane_points.Add(new LanePortionPoint(
                            new Vector3(segment.longitude, lane_index * lane_width, 0).ScaledBy(track_scale),
                            segment, lane_index));

                        if (lane_points.Count == 0)
                            PrependLaneEmerge(lane_points, lane_index, last_segment, lane_width, track_scale);
                    }
                    else // No more lane (at least interrupted)
                    { // -> add the lane to the list
                        if (lane_points.Count > 0)
                        {
                            AddLaneMerge(lane_points, lane_index, segment, lane_width, track_scale);
                            lane_portions.Add(lane_points.ToArray());
                            lane_points = new List<LanePortionPoint>(); // reset
                        }
                    }

                    last_segment = segment;
                }

                if (lane_points.Count > 0) // ensure that the last portion is included
                    lane_portions.Add(lane_points.ToArray());
            }

            return lane_portions;
        }


        private const int MERGE_FIDELITY = 12;

        // Add a point to the neighboring lane, prioritizing to the right (higher segment index)
        private void AddLaneMerge(List<LanePortionPoint> lane_points, int lane_index, TrackSegment segment, float lane_width, Vector3 track_scale)
        {
            Vector3 prev_point = lane_points.Last().pt;
            Vector3 next_point;

            if (segment.ContainsLane(lane_index + 1))
                next_point = new Vector3(segment.longitude, (lane_index + 1) * lane_width, 0).ScaledBy(track_scale);
            else
            if (segment.ContainsLane(lane_index - 1))
                next_point = new Vector3(segment.longitude, (lane_index - 1) * lane_width, 0).ScaledBy(track_scale);
            else
                return; // TODO: Smooth end

            for (int i = 1; i <= MERGE_FIDELITY; i++)
                lane_points.Add(new LanePortionPoint(
                    prev_point.LaneLerpTo(next_point, ((float)i / MERGE_FIDELITY)),
                    segment, lane_index));
        }

        // Prepend a point to the neighboring lane, prioritizing to the left (lower segment index)
        private void PrependLaneEmerge(List<LanePortionPoint> lane_points, int lane_index, TrackSegment last_segment, float lane_width, Vector3 track_scale)
        {
            Vector3 next_point = lane_points.First().pt;
            Vector3 prev_point;

            if (last_segment.ContainsLane(lane_index - 1))
                prev_point = new Vector3(last_segment.longitude, (lane_index - 1) * lane_width, 0).ScaledBy(track_scale);
            else
            if (last_segment.ContainsLane(lane_index + 1))
                prev_point = new Vector3(last_segment.longitude, (lane_index + 1) * lane_width, 0).ScaledBy(track_scale);
            else
                return;

            for (int i = 0; i < MERGE_FIDELITY; i++)
                lane_points.Insert(i, new LanePortionPoint(
                    prev_point.LaneLerpTo(next_point, ((float)i / MERGE_FIDELITY)),
                    last_segment, lane_index));
        }

        internal float GetWidth(float lane_width_meter)
        {
            uint min_offset = int.MaxValue;
            uint max_offset_plus_lanes = 0;

            foreach (TrackSegment seg in track)
            {
                if (seg.offset < min_offset)
                    min_offset = seg.offset;

                if ((seg.offset + seg.lanes) > max_offset_plus_lanes)
                    max_offset_plus_lanes = seg.offset + seg.lanes;
            }

            return (max_offset_plus_lanes - min_offset) * lane_width_meter;
        }

        internal float GetLength() => track.Last().longitude;
    }

    static public class Vector3Extensions
    {
        static public Vector3 ScaledBy(this Vector3 vec, Vector3 sec)
        {
            vec.Scale(sec);
            return vec;
        }

        static public Vector3 LaneLerpTo(this Vector3 prev_point, Vector3 next_point, float f)
        {
            // x, z regular lerp, y smooth lerp
            return new Vector3(
                Mathf.Lerp(prev_point.x, next_point.x, f),
                Mathf.SmoothStep(prev_point.y, next_point.y, Mathf.SmoothStep(0f, 1f, f)),
                Mathf.Lerp(prev_point.z, next_point.z, f));
        }

    }
}