using System.Collections;
using System.Collections.Generic;
using TrackVisuData;
using TrackVisuMeshMaker;
using TrackVisuUtils;
using UnityEngine;
using static TrackVisuData.Track;

namespace TrackVisu
{
    public class TrackVisualizer : MonoBehaviour
    {
        [SerializeField] List<TrackDesign> track_designs;
        [SerializeField] LineRenderer track_alternative_prefab;

        List<GameObject> active_track_objects;
        private bool grass_enabled = true;

        internal void Realize(Track track, float lane_width_meter, Vector3 track_scale)
        {
            if (active_track_objects == null)
                active_track_objects = new List<GameObject>();

            if (track_designs.Count != 0)
            {
                TrackDesign track_design = track_designs[Random.Range(0, track_designs.Count)];
                track_design.ClearTrackMeshes(active_track_objects);
                active_track_objects.AddRange(track_design.CreateTrackMeshes(track, lane_width_meter, track_scale, transform));
                SetGrassState(grass_enabled);
            }
            else
            {
                for (int i = 0; i < active_track_objects.Count; i++)
                    GlobalObjectPool.Store("LaneLine", active_track_objects[i]);

                foreach (LanePortionPoint[] lane_points in track.GetLanePortionPoints(lane_width_meter, track_scale))
                {
                    int len = lane_points.Length;
                    Vector3[] points = new Vector3[len];
                    for (int i = 0; i < len; i++)
                        points[i] = lane_points[i].pt;

                    MakeLine(points, 0, Color.black, lane_width_meter * 0.9f);
                }
            }
        }

        private void MakeLine(Vector3[] points, float y_offset, Color color, float width)
        {
            LineRenderer line = GlobalObjectPool.Acquire("LaneLine", () => Instantiate(track_alternative_prefab), transform);

            line.gameObject.layer = gameObject.layer;
            line.positionCount = points.Length;
            line.SetPositions(points);

            line.alignment = LineAlignment.TransformZ;

            line.startColor = color;
            line.endColor = color;

            line.startWidth = width;
            line.endWidth = width;

            line.transform.localPosition = new Vector3(0, y_offset, 0);

            active_track_objects.Add(line.gameObject);
        }

        public void ToggleGrass()
        {
            SetGrassState(!grass_enabled);
        }
        private void SetGrassState(bool on)
        {
            grass_enabled = on;
            foreach (Transform child in transform)
                if (child.name.Contains("grass"))
                    child.gameObject.SetActive(grass_enabled);
        }
    }
}