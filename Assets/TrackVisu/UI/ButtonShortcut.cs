using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrackVisuUi
{
    public class ButtonShortcut : MonoBehaviour
    {
        [SerializeField] KeyCode key;

        private Button button;
        private Toggle toggle;
        // Start is called before the first frame update
        void Start()
        {
            button = GetComponent<Button>();
            if (!button)
            {
                toggle = GetComponent<Toggle>();
                if (!toggle)
                    throw new System.Exception("ButtonShortcut needs a button or a toggle component!");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                if (button)
                    button.onClick?.Invoke();
                else
                    toggle.isOn = !toggle.isOn;
            }
        }
    }
}