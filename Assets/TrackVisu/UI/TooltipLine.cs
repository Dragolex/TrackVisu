using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrackVisuUi
{
    public class TooltipLine : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI left_text;
        [SerializeField] TextMeshProUGUI right_text;

        Func<string> right_string_getter = null;

        internal void Setup(string left_str, Func<string> right_string_getter, int layer)
        {
            left_text.text = left_str;
            this.right_string_getter = right_string_getter;
            FixedUpdate();

            gameObject.layer = layer;
            right_text.gameObject.layer = layer;
            right_text.gameObject.layer = layer;
        }

        void FixedUpdate()
        {
            if (right_string_getter != null)
            {
                string str = right_string_getter();
                if (str != right_text.text)
                    right_text.text = str;
            }
        }
    }
}