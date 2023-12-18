using UnityEngine;

namespace Fusyon.Extensions
{
    /// <summary>
    /// Useful mathematical functions.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Remaps a value from a range of values to another.
        /// </summary>
        /// <param name="value">The value to remap.</param>
        /// <param name="oldMin">The old minimum value.</param>
        /// <param name="oldMax">The old maximum value.</param>
        /// <param name="newMin">The new minimum value.</param>
        /// <param name="newMax">The new maximum value.</param>
        /// <returns></returns>
        public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return (value - oldMin) * (newMax - newMin) / (oldMax - oldMin) + newMin;
        }

        /// <summary>
        /// Calculates the Chebyshev distance between two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>The Chebyshev distance.</returns>
        public static float ChebyshevDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
        }

        /// <summary>
        /// Applies bias to a value.
        /// </summary>
        /// <param name="value">The value to apply bias to.</param>
        /// <param name="bias">The amount of bias in the [0, 1] range.</param>
        /// <returns>A biased value.</returns>
        public static float Bias(float value, float bias = 0.5f)
        {
            return Mathf.Pow(bias, Mathf.Log(value) / Mathf.Log(0.5f));
        }
    }
}
