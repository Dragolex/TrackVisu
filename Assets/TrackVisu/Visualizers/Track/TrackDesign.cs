using System;
using System.Collections;
using System.Collections.Generic;
using TrackVisuData;
using TrackVisuUtils;
using UnityEngine;
using static TrackVisuData.Track;

namespace TrackVisuMeshMaker
{
    [CreateAssetMenu(fileName = "TrackDesign", menuName = "SO/TrackDesign", order = 1)]
    public class TrackDesign : ScriptableObject
    {
        [Header("General")]
        [SerializeField] MeshFilter mesh_prefab;
        [SerializeField] float max_segment_length = 3f;

        [Header("Road Surface")]
        [SerializeField] float road_height = 0.15f;
        [SerializeField] AnimationCurve road;
        [SerializeField] Material road_material;

        [Header("Nature/Terrain")]
        [SerializeField] float nature_height = 5.0f;
        [SerializeField] float nature_width = 50.0f;
        [SerializeField] float nature_perlin_scale = 20.0f;
        [SerializeField] float nature_perlin_height_factor = 0.75f;
        [SerializeField] AnimationCurve nature_left;
        [SerializeField] AnimationCurve nature_right;
        [SerializeField] Material nature_top_material;
        [SerializeField] Material nature_base_material;

        [Header("Road Edge")]
        [SerializeField] float road_edge_height = 0.5f;
        [SerializeField] float road_edge_width = 0.3f;
        [SerializeField] AnimationCurve edge_left;
        [SerializeField] AnimationCurve edge_right;
        [SerializeField] Material edge_material;

        [Header("Road Dashed Lines")]
        [SerializeField] float road_dashed_line_width = 0.3f; // 30cm on highways, 25cm in cities
        [SerializeField] float road_dashed_line_height = 0.05f;
        [SerializeField] float road_dashed_line_segment_length = 6f;
        [SerializeField] float road_dashed_line_segment_length_gap = 12f;
        [SerializeField] AnimationCurve road_dashed_line;
        [SerializeField] Material dashed_lines_material;


        // Local temporary MeshMakers
        private MeshMaker main_road_maker, nature_maker_left, nature_maker_right, edge_maker_left, edge_maker_right, dashed_lines_maker;

        public List<GameObject> CreateTrackMeshes(Track track, float lane_width_meter, Vector3 track_scale, Transform parent)
        {
            List<GameObject> meshes = new List<GameObject>();
            PrepareMeshMakers(parent, meshes);

            foreach (LanePortionPoint[] lane_points in track.GetLanePortionPoints(lane_width_meter, track_scale))
            {
                Vector3 to_edge_left = new Vector3(0, lane_width_meter / 2f, 0);
                Vector3 to_edge_right = new Vector3(0, -lane_width_meter / 2f, 0);

                SignalizeNewLanePortionToMeshMakers();

                int len = lane_points.Length;
                for (int point_index = 0; point_index < len - 1; point_index++)
                {
                    float segment_x_total = lane_points[point_index + 1].pt.x;
                    float segment_x_start = lane_points[point_index].pt.x;
                    float segment_x_end = segment_x_start;

                    // Repeat for all segments
                    while (segment_x_end < (segment_x_total - 0.001f))
                    {
                        segment_x_end = Mathf.Min(segment_x_start + max_segment_length, segment_x_total);

                        LanePortionPoint pt = lane_points[point_index];
                        LanePortionPoint npt = lane_points[point_index + 1];

                        Vector3 pos = pt.pt;
                        Vector3 npos = npt.pt;
                        pos.x = segment_x_start;
                        npos.x = segment_x_end;

                        CreateSegment(pt, pos, npos, lane_width_meter, to_edge_left, to_edge_right);

                        segment_x_start += max_segment_length;
                    }
                }


                // For debugging purpsoes, you can call the InstantiateAndReset methods here as well!
                // This will create way more meshes however.
                InstantiateMeshes();
            }

            InstantiateMeshes();
            return meshes;
        }

        private void CreateSegment(LanePortionPoint pt, Vector3 pos, Vector3 npos, float lane_width_meter, Vector3 to_edge_left, Vector3 to_edge_right)
        {
            Vector3 left_pos = pos + to_edge_left;
            Vector3 right_pos = pos + to_edge_right;
            Vector3 next_right_pos = npos + to_edge_right;
            Vector3 next_left_pos = npos + to_edge_left;

            main_road_maker.AddSegmentWithCurve(right_pos, next_right_pos, road, lane_width_meter, road_height, nature_perlin_height_factor);

            Vector3 edge_shift = new Vector3(0, road_edge_width);

            // Add terrain (nature)
            if (pt.nothing_on_left)
            {
                nature_maker_left.AddSegmentWithCurve(
                    left_pos + edge_shift, next_left_pos + edge_shift, nature_left, nature_width, nature_height,
                    perlin_noise_factor: 0.5f,
                    perlin_noise_scale: nature_perlin_scale);

                edge_maker_left.AddSegmentWithCurve(
                    left_pos, next_left_pos, edge_left, road_edge_width, road_edge_height);
            }
            if (pt.nothing_on_right)
            {
                nature_maker_right.AddSegmentWithCurve(
                    right_pos - edge_shift, next_right_pos - edge_shift, nature_right, -nature_width, nature_height,
                    perlin_noise_factor: 0.5f,
                    perlin_noise_scale: nature_perlin_scale);

                edge_maker_right.AddSegmentWithCurve(
                    right_pos, next_right_pos, edge_right, -road_edge_width, road_edge_height);
            }

            if (pt.road_on_left || pt.road_on_right)
            {
                float dashed_line_plus_gap = road_dashed_line_segment_length + road_dashed_line_segment_length_gap;
                int current_iteration = Mathf.FloorToInt(right_pos.x / dashed_line_plus_gap);
                float x_in_current_iteration = right_pos.x - current_iteration * dashed_line_plus_gap;
                float limited_x = Mathf.Min(next_right_pos.x, current_iteration * dashed_line_plus_gap + road_dashed_line_segment_length);

                if (x_in_current_iteration < road_dashed_line_segment_length)
                {
                    if (pt.road_on_left)
                        dashed_lines_maker.AddSegmentWithCurve(
                            new Vector3(left_pos.x, left_pos.y - road_dashed_line_width / 2, left_pos.z),
                            new Vector3(limited_x, next_left_pos.y - road_dashed_line_width / 2, next_left_pos.z),
                            road_dashed_line, road_dashed_line_width, road_dashed_line_height);
                    if (pt.road_on_right)
                        dashed_lines_maker.AddSegmentWithCurve(
                            new Vector3(right_pos.x, right_pos.y + road_dashed_line_width / 2, right_pos.z),
                            new Vector3(limited_x, next_right_pos.y + road_dashed_line_width / 2, next_right_pos.z),
                            road_dashed_line, -road_dashed_line_width, road_dashed_line_height);
                }
            }


            /*
            maker.Add(pos + to_edge_left);
            maker.Add(pos + to_edge_right);
            maker.Add(npos + to_edge_left);

            maker.Add(npos + to_edge_left);
            maker.Add(pos + to_edge_right);
            maker.Add(npos + to_edge_right);
            */
        }

        private void PrepareMeshMakers(Transform parent, List<GameObject> meshes)
        {
            main_road_maker = new MeshMaker(meshes, mesh_prefab, parent, 20, false);
            nature_maker_left = new MeshMaker(meshes, mesh_prefab, parent, 30, true);
            nature_maker_right = new MeshMaker(meshes, mesh_prefab, parent, 30, true);
            edge_maker_left = new MeshMaker(meshes, mesh_prefab, parent, 6, true);
            edge_maker_right = new MeshMaker(meshes, mesh_prefab, parent, 6, true);
            dashed_lines_maker = new MeshMaker(meshes, mesh_prefab, parent, 6, false);
        }
        private void SignalizeNewLanePortionToMeshMakers()
        {
            nature_maker_left.ResetVertexReusage();
            nature_maker_right.ResetVertexReusage();
            edge_maker_left.ResetVertexReusage();
            edge_maker_right.ResetVertexReusage();
        }

        private void InstantiateMeshes()
        {
            main_road_maker.InstantiateAndReset("Main Road", road_material);
            if (nature_top_material != null)
                nature_maker_left.InstantiateMesh("Left Nature Top (grass etc.)", nature_top_material);
            nature_maker_left.InstantiateAndReset("Left Nature Base", nature_base_material);
            if (nature_top_material != null)
                nature_maker_right.InstantiateMesh("Right Nature Top (grass etc.)", nature_top_material);
            nature_maker_right.InstantiateAndReset("Right Nature Base", nature_base_material);
            edge_maker_left.InstantiateAndReset("Road Left Edge", edge_material);
            edge_maker_right.InstantiateAndReset("Road Right Edge", edge_material);
            dashed_lines_maker.InstantiateAndReset("Dashed Line", dashed_lines_material);
        }

        internal void ClearTrackMeshes(List<GameObject> active_track_objects)
        {
            for (int i = 0; i < active_track_objects.Count; i++)
                GlobalObjectPool.Store("LaneMesh", active_track_objects[i]);
        }
    }
}