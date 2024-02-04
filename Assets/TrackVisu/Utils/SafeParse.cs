using System;
using System.Globalization;
using UnityEngine;

namespace TrackVisuUtils
{
    static public class SafeParse
    {
        static public float Float(string str)
        {
            try
            {
                return float.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse the following string to float: \"{str}\" due to error: {e.Message}");
            }
            return 0;
        }
        static public uint Uint(string str)
        {
            try
            {
                return uint.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse the following string to uint: \"{str}\" due to error: {e.Message}");
            }
            return 0;
        }
    }
}