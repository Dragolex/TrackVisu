using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrackVisuUtils
{
    [DefaultExecutionOrder(-999)]
    public class GlobalObjectPool : MonoBehaviour
    {
        static private GlobalObjectPool S;
        private Dictionary<string, Queue<GameObject>> available;

        void Start()
        {
            S = this;
            available = new Dictionary<string, Queue<GameObject>>();
        }

        static public T Acquire<T>(string requested_type, Func<T> create, Transform parent = null, bool worldposition_stays = true) where T : Component
        {
            Queue<GameObject> available_objs;
            if (!S.available.TryGetValue(requested_type, out available_objs))
            {
                available_objs = new Queue<GameObject>();
                S.available[requested_type] = available_objs;
            }

            GameObject obj;
            if (available_objs.TryDequeue(out obj))
            {
                if (parent)
                    obj.transform.SetParent(parent, worldposition_stays);
                obj.SetActive(true);
                return obj.GetComponent<T>();
            }

            T comp = create();
            if (parent)
                comp.transform.SetParent(parent, worldposition_stays);
            return comp;
        }

        static public void Store(string requested_type, GameObject obj)
        {
            obj.transform.SetParent(S.transform, true);
            obj.SetActive(false);

            Queue<GameObject> available_objs;
            if (!S.available.TryGetValue(requested_type, out available_objs))
            {
                available_objs = new Queue<GameObject>();
                S.available[requested_type] = available_objs;
            }
            available_objs.Enqueue(obj);
        }

        static public void Clear(string requested_type, bool remove_type = false)
        {
            if (S.available.TryGetValue(requested_type, out Queue<GameObject> available_objs))
            {
                foreach (GameObject obj in available_objs)
                    Destroy(obj);
                available_objs.Clear();
            }
            if (remove_type && S.available.ContainsKey(requested_type))
                S.available.Remove(requested_type);
        }

        static public void DisposeAllTypes()
        {
            foreach (Queue<GameObject> available_objs in S.available.Values)
            {
                foreach (GameObject obj in available_objs)
                    Destroy(obj);
                available_objs.Clear();
            }
            S.available.Clear();
        }

        static public int CountAvailable(string requested_type)
        {
            if (S.available.TryGetValue(requested_type, out Queue<GameObject> available_objs))
                return available_objs.Count;
            return 0;
        }
    }
}