using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace TrackVisuUi
{
    public class TooltipController : MonoBehaviour
    {
        [SerializeField] TooltipLine tooltip_line_prefab;
        [SerializeField] Image image;
        [SerializeField] float tooltip_z_pos;
        [SerializeField] float tooltip_line_height;

        static TooltipController instance;

        private Func<Vector3> screen_position_getter;
        private GameObject tooltip_caller;

        // Start is called before the first frame update
        void Start()
        {
            instance = this;
        }

        public static void OpenTooltip(GameObject tooltip_caller, Func<Vector3> world_position_getter, Camera cam, List<(string left_str, Func<string> right_str)> content)
        {
            if (instance.tooltip_caller == tooltip_caller)
                return;

            ClearLines();

            EnableOrDisable(tooltip_caller);

            instance.gameObject.layer = cam.gameObject.layer;
            instance.screen_position_getter = () => cam.WorldToScreenPoint(world_position_getter());

            foreach ((string left_str, Func<string> right_str) in content)
            {
                TooltipLine line = Instantiate(instance.tooltip_line_prefab, instance.transform);
                line.Setup(left_str, right_str, cam.gameObject.layer);
            }

            instance.Update();
        }

        private static void ClearLines()
        {
            foreach (Transform line in instance.transform)
                Destroy(line.gameObject);
        }

        private static void EnableOrDisable(GameObject tooltip_caller)
        {
            instance.tooltip_caller = tooltip_caller;
            instance.image.enabled = tooltip_caller != null;

            if (tooltip_caller == null)
                ClearLines();
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                EnableOrDisable(null);

            if (instance.tooltip_caller != null && instance.screen_position_getter != null)
            {
                Vector3 pos = screen_position_getter();
                pos.z = instance.tooltip_z_pos;
                pos.y -= tooltip_line_height * 0.5f * (instance.transform.childCount + 2);
                instance.transform.position = pos;
            }
        }
    }
}