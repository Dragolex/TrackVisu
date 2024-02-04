using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrackVisuUtils
{
    public class SyncOrientation : MonoBehaviour
    {
        [SerializeField] private Transform source;
        [SerializeField] private Vector3 offset;
        [SerializeField] private Quaternion offset_quat = Quaternion.identity;


        // Update is called once per frame
        void Update()
        {
            transform.rotation = Quaternion.Euler(offset) * offset_quat * source.rotation;
        }
    }
}