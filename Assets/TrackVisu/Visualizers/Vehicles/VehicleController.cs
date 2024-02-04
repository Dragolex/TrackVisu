using System.Collections;
using System.Collections.Generic;
using TrackVisuData;
using TrackVisuUtils;
using UnityEngine;

namespace VehicleVisu
{
    public class VehicleController : MonoBehaviour
    {
        [SerializeField] Driver ego_driver_prefab_ego;
        [SerializeField] List<Driver> traffic_driver_prefabs;
        [SerializeField] Vector3 vehicle_scale = Vector3.one;
        [SerializeField] LookAtTargetConfig lookat_target = null;


        Driver ego_driver;
        Stack<Driver> active_traffic_drivers;
        private bool visu_mode_3d = true;

        internal void ClearTrajectories()
        {
            if (active_traffic_drivers == null)
                active_traffic_drivers = new Stack<Driver>();

            if (ego_driver != null)
                GlobalObjectPool.Store("Ego Driver", ego_driver.gameObject);
            while (active_traffic_drivers.Count > 0)
                GlobalObjectPool.Store("Driver", active_traffic_drivers.Pop().gameObject);
        }

        internal Driver PrepareTrajectory(Trajectory trajectory, bool is_ego, Vector3 track_scale)
        {
            System.Func<Driver> prefab_constructor = is_ego ? () => Instantiate(ego_driver_prefab_ego) : () => Instantiate(traffic_driver_prefabs[Random.Range(0, traffic_driver_prefabs.Count)]);
            Driver driver = GlobalObjectPool.Acquire(is_ego ? "Ego Driver" : "Driver", prefab_constructor, transform);

            Vector3 inv_track_scale = new Vector3(track_scale.y, track_scale.z, track_scale.x);
            driver.SetupVehicle(trajectory, vehicle_scale, inv_track_scale, gameObject.layer, lookat_target);

            if (is_ego)
                ego_driver = driver;
            else
                active_traffic_drivers.Push(driver);

            return driver;
        }

        internal void UpdateVehicles(float time_position, Vector3 track_scale)
        {
            if (ego_driver == null)
                return;

            ego_driver.UpdatePosition(time_position, track_scale);
            foreach (Driver driver in active_traffic_drivers)
                driver.UpdatePosition(time_position, track_scale);
        }

        public void SetVisuMode(bool model, bool basic, bool wireframe)
        {
            foreach (Driver driver in active_traffic_drivers)
                driver.SetMode(model, basic, wireframe);
            if (ego_driver != null)
                ego_driver.SetMode(model, basic, wireframe);
        }

        public void ApplyVisuMode3D()
        {
            if (visu_mode_3d)
                SetVisuMode(true, false, false);
            else
                SetVisuMode(false, true, true);
        }
        public void ToggleVisuMode3D()
        {
            visu_mode_3d = !visu_mode_3d;
            ApplyVisuMode3D();
        }

    }

    [System.Serializable]
    internal class LookAtTargetConfig
    {
        [SerializeField] public Camera cam;
        [SerializeField] Transform lookat;
        [SerializeField] Vector3 offset;
        [SerializeField] Vector3 rotation_euler;

        internal void Apply(Transform tr)
        {
            if (lookat)
            {
                //tr.LookAt(lookat.transform);
                tr.localPosition = offset;
                tr.rotation = Quaternion.LookRotation(lookat.forward) * Quaternion.Euler(rotation_euler);
            }
        }
    }
}