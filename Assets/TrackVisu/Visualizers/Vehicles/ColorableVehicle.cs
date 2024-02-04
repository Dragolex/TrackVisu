using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleVisu
{
    public class ColorableVehicle : MonoBehaviour
    {
        [SerializeField] MeshRenderer mesh_renderer;
        [SerializeField] int material_index = 0;
        [Tooltip("Leave as Clear to not use.")]
        [SerializeField] Color optional_permanent_color = Color.clear;

        private bool got_color = false;
        private Color color = Color.white;

        internal Color GetColor()
        {
            if (optional_permanent_color.a > 0)
                return optional_permanent_color;

            if (!got_color)
                color = Random.ColorHSV(0f, 1f, 0.6f, 0.8f, 0.5f, 1f);
            got_color = true;
            return color;
        }

        private void OnEnable()
        {
            if (mesh_renderer != null)
                if (material_index == 0)
                    mesh_renderer.material.color = GetColor();
                else
                    mesh_renderer.materials[material_index].color = GetColor();
        }
    }
}