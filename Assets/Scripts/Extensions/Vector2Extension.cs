using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public static class Vector2Extension
{
    public static Vector2 Rotate(this Vector2 vec, float degAngle) {
        float radAngle = degAngle * Mathf.PI / 180f;
        float x = vec.x;
        float y = vec.y;

        return new Vector2(x * Mathf.Cos(radAngle) - y * Mathf.Sin(radAngle), x * Mathf.Sin(radAngle) + y * Mathf.Cos(radAngle));
    }

    public static bool LineLineIntersection(Vector2 point1, Vector2 vec1, Vector2 point2, Vector2 vec2, out Vector2 intersectionPoint) {
        Vector3 vec3 = point2 - point1;
        Vector3 crossVec1and2 = Vector3.Cross(vec1, vec2);
        Vector3 crossVec3and2 = Vector3.Cross(vec3, vec2);

        float planarFactor = Vector3.Dot(vec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f) {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersectionPoint = point1 + (vec1 * s);
            return true;
        } else {
            intersectionPoint = Vector3.zero;
            return false;
        }
    }
}
