using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrackVisu;
using TrackVisuData;
using TrackVisuUi;
using TrackVisuUtils;
using UnityEngine;

namespace VehicleVisu
{
    public class Driver : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] TextMeshPro name_field;
        [SerializeField] Transform vehicle_transform;
        [SerializeField] MeshRenderer square_shape;
        [SerializeField] GameObject wireframe_shape;

        [Header("Vehicle")]
        [SerializeField] List<ColorableVehicle> vehicle_visualizer_prefabs;
        [SerializeField] Vector3 shape_scale = new Vector3(2, 1, 4);
        [SerializeField] Vector3 vehicle_visualizer_orientation = Vector3.zero;


        private Trajectory trajectory;
        private ColorableVehicle vehicle = null;
        private LookAtTargetConfig lookat_target;

        private Vector3 last_real_trajectory_position;
        private Vector3 trajectory_velocity;

        internal void SetupVehicle(Trajectory trajectory, Vector3 vehicle_scale, Vector3 track_scale, int layer, LookAtTargetConfig lookat_target)
        {
            this.trajectory = trajectory;
            this.lookat_target = lookat_target;
            name_field.text = trajectory.name;

            if (vehicle != null)
                GlobalObjectPool.Store("Vehicle Model", vehicle.gameObject);

            vehicle = GlobalObjectPool.Acquire("Vehicle Model", () => Instantiate(vehicle_visualizer_prefabs[Random.Range(0, vehicle_visualizer_prefabs.Count)]), vehicle_transform, worldposition_stays: false);
            Quaternion rotation = Quaternion.Euler(vehicle_visualizer_orientation);
            vehicle.transform.localPosition = Vector3.zero;
            vehicle.transform.localScale = rotation * vehicle_scale;
            vehicle.transform.localRotation = rotation;

            Color col = vehicle.GetColor();
            col.a = 0.5f;
            square_shape.material.color = col;

            square_shape.transform.localScale = shape_scale.ScaledBy(track_scale);
            wireframe_shape.transform.localScale = shape_scale.ScaledBy(track_scale);

            gameObject.layer = layer;
            foreach (var child in transform.GetComponentsInChildren<Transform>(includeInactive: true))
                child.gameObject.layer = layer;
        }
        internal void UpdatePosition(float time_position, Vector3 track_scale)
        {
            if (trajectory.Interpolate(time_position, out Vector3 pos, out Vector3 velocity, out Quaternion rotation))
            {
                last_real_trajectory_position = pos;
                transform.localPosition = pos.ScaledBy(track_scale);
                transform.localRotation = rotation;
                trajectory_velocity = velocity;
            }
        }

        public void SetMode(bool model, bool basic, bool wireframe)
        {
            vehicle.gameObject.SetActive(model);
            wireframe_shape.gameObject.SetActive(wireframe);
            square_shape.gameObject.SetActive(basic);
        }
        public string GetName()
        {
            return name_field.text;
        }

        private void LateUpdate()
        {
            lookat_target.Apply(name_field.transform);
        }

        void OnMouseDown()
        {
            Controller.SetCameraTarget(this);
        }

        private void OnMouseEnter()
        {
            Driver self = this;
            TooltipController.OpenTooltip(gameObject, () => self.transform.position, lookat_target.cam,
                new List<(string left, System.Func<string> right)>()
                {
                ("X:", () => self.last_real_trajectory_position.x.ToString("F2")+"m"),
                ("Y:", () => self.last_real_trajectory_position.y.ToString("F2")+"m"),
                ("V:", () => (self.trajectory_velocity.magnitude*3.6f).ToString("F2")+"km/h"),
                ("xV:", () => self.trajectory_velocity.x.ToString("F2") + "m/s"),
                ("yV:", () => self.trajectory_velocity.y.ToString("F2") + "m/s"),
                });
        }
    }
}