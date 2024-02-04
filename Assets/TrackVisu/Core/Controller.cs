using AnotherFileBrowser.Windows;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TrackVisuData;
using TrackVisuUtils;
using UnityEngine;
using UnityEngine.UI;
using VehicleVisu;

namespace TrackVisu
{
    [DefaultExecutionOrder(-500)]
    public class Controller : MonoBehaviour
    {
        [SerializeField] int target_framerate = 90;

        [Header("CONTROLS")]
        [SerializeField] Slider time_slider;
        [SerializeField] TextMeshProUGUI time_text;
        [SerializeField] Slider speed_slider;
        [SerializeField] TextMeshProUGUI speed_text;

        [Header("Keys")]
        [SerializeField] KeyCode pause_key = KeyCode.Space;
        [SerializeField] KeyCode swap_key = KeyCode.LeftControl;

        [Header("Refs")]
        [SerializeField] FileManagement file_management;
        [SerializeField] RandomTrackGenerator generator;
        [SerializeField] List<Visualizer> visualizers;
        [SerializeField] SimpleCam ego_camera;
        [SerializeField] GameObject settings_canvas;
        [SerializeField] Toggle steps_only_toggle;

        [Header("Prefabs & UI")]
        [SerializeField] Transform buttons_parent;
        [SerializeField] Button loaded_files_button_prefab;

        static private Controller instance;

        private const int EGO_TRAJECTORY_INDEX = 0; // ego has always index 0

        private Track current_track;
        private List<Trajectory> current_trajectories;

        private float last_set_time = -1f;
        private float last_speed_factor = 1.0f;
        private float current_max_track_duration = 1f;
        private float steps_only_timer = 0f;

        private string currently_selected_filepath = null;
        private string last_selected_filepath = null;

        private Button currently_selected_button = null;

        private Driver ego_driver = null;
        private Dictionary<string, Driver> names_of_vehicles; // This remembers only the vehicles on the 3D view

        // Start is called before the first frame update
        void Start()
        {
            instance = this;
            Application.targetFrameRate = target_framerate;

            names_of_vehicles = new Dictionary<string, Driver>();
        }


        internal string CreateNewRandom(string path, int name_index)
        {
            (current_track, current_trajectories) = generator.Generate();
            string filepath = Path.Combine(path, $"random_generated_#{name_index}.csv");
            SaveAndLoad.ToMultiCsv(current_track, current_trajectories, filepath);
            Debug.Log("New random sample generated and saved to: " + filepath);
            return filepath;
        }

        internal void RealizeFromCsv(string filepath, Button button)
        {
            (current_track, current_trajectories) = SaveAndLoad.FromMultiCsv(filepath);

            // For verification by re-saving
            //(track, trajectories) = SaveAndLoad.FromMultiCsv(path_to_file_to_check);
            //SaveAndLoad.ToMultiCsv(track, trajectories, path_to_file_to_check+"_resaved.csv");

            Debug.Log("Realizing from file: " + filepath);

            names_of_vehicles.Clear();

            for (int i = 0; i < visualizers.Count; i++)
            {
                List<Driver> drivers = visualizers[i].Setup(current_track, current_trajectories, filepath.GetHashCode());
                if (i == 0)
                {
                    ego_driver = drivers[0]; // ego is always the first
                    foreach (Driver driver in drivers)
                        names_of_vehicles[driver.GetName()] = driver;
                }
            }

            if (button != null)
            {
                if (currently_selected_button)
                    currently_selected_button.gameObject.GetComponent<Image>().color = Color.white;
                button.gameObject.GetComponent<Image>().color = Color.green;
                currently_selected_button = button;
            }

            last_selected_filepath = currently_selected_filepath;
            currently_selected_filepath = filepath;
        }

        private void Update()
        {
            if (current_trajectories == null || current_trajectories.Count == 0)
                return;

            current_max_track_duration = visualizers.Max((visu) => visu.current_track_duration);

            if (speed_slider.value >= 0.001 && current_max_track_duration > 0)
            {
                if (time_slider.value != last_set_time)
                    SliderJumped();

                float time_position = time_slider.value + (Time.deltaTime * speed_slider.value) * (1f / current_max_track_duration);

                if (steps_only_toggle.isOn)
                {
                    float step_size = 1f / (current_trajectories[EGO_TRAJECTORY_INDEX].Length - 1);
                    time_position = Mathf.RoundToInt((time_position) / step_size) * step_size;
                    steps_only_timer += Time.deltaTime * speed_slider.value * 2 * (1f / current_max_track_duration);
                    if (steps_only_timer > step_size)
                    {
                        time_position += step_size;
                        steps_only_timer = 0;
                    }
                }
                else
                    steps_only_timer = 0;

                if (time_position > 1.0f)
                {
                    time_position = 0f;
                    SliderJumped();
                }
                time_slider.value = time_position;
                last_speed_factor = speed_slider.value;
            }
            else
                if (last_set_time != time_slider.value)
                SliderJumped();

            last_set_time = time_slider.value;

            float absolute_time = time_slider.value * current_max_track_duration;

            foreach (Visualizer visu in visualizers)
                visu.UpdateTime(absolute_time);

            HandleKeys();
            UpdateTexts(absolute_time);
        }

        private void SliderJumped()
        {
            Vector3 offset = ego_camera.transform.position - ego_camera.GetTarget().position;
            StartCoroutine(ForceCameraPosition(offset));
        }
        IEnumerator ForceCameraPosition(Vector3 offset)
        {
            yield return null;
            Vector3 offset_change = ((ego_camera.GetTarget().position + offset) - ego_camera.transform.position);
            if (offset_change.magnitude > 3f) // limit past which we force skip the camera position
            {
                ego_camera.transform.position = ego_camera.GetTarget().position + offset;
                ego_camera.ResetVelocity();
            }
        }

        private void UpdateTexts(float absolute_time)
        {
            float ration_in_frame = current_trajectories[EGO_TRAJECTORY_INDEX].GetRatioInKeyframeFromTimePosition(absolute_time, out int frame_index, out _, out _);
            string ego_state = (frame_index - 1 + ration_in_frame).ToString("F2");

            time_text.text = $"Time: {absolute_time.ToString("F2")}s ({Mathf.FloorToInt(time_slider.value * 100)}%) --- Ego State: {ego_state}/{current_trajectories[EGO_TRAJECTORY_INDEX].Length}";
            speed_text.text = "Speed: x" + speed_slider.value.ToString("F1");
        }

        private void HandleKeys()
        {
            if (Input.GetKeyDown(pause_key))
                speed_slider.value = (speed_slider.value >= 0.001) ? 0f : last_speed_factor;

            if (Input.GetKeyDown(swap_key))
                if (last_selected_filepath != null)
                    RealizeFromCsv(last_selected_filepath, file_management.GetButtonForFile(last_selected_filepath));

            float step_size = (0.1f * last_speed_factor) / current_max_track_duration;
            if (steps_only_toggle.isOn)
                step_size = 1f / (current_trajectories[EGO_TRAJECTORY_INDEX].Length - 1);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                time_slider.value += step_size;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                time_slider.value -= step_size;

            if (steps_only_toggle.isOn)
                time_slider.value = Mathf.RoundToInt(time_slider.value / step_size) * step_size;

            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();
        }


        private void OnValidate()
        {
            Application.targetFrameRate = target_framerate;
        }


        // Static methods
        static public void SetCameraTarget(Driver target)
        {
            // Always get the primary object from the 3D view
            if (instance.names_of_vehicles.TryGetValue(target.GetName(), out Driver driver))
                instance.ego_camera.SetTarget(driver.transform);
        }
        public void ResetCameraTargetToEgo()
        {
            if (ego_driver != null)
                instance.ego_camera.SetTarget(ego_driver.transform);
        }

        public void CaptureScreenshotNextFrame()
        {
            StartCoroutine(CaptureScreenshot());
        }

        private IEnumerator CaptureScreenshot()
        {
            int ind = 0;
            string screenshot_file = $"{currently_selected_filepath}_screenshot_{ind}.png";
            while (File.Exists(screenshot_file))
                screenshot_file = $"{currently_selected_filepath}_screenshot_{ind++}.png";
            ScreenCapture.CaptureScreenshot(screenshot_file, 2);
            settings_canvas.SetActive(false);
            yield return null; // frame of screenshot
            settings_canvas.SetActive(true);
        }
    }
}