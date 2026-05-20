using UnityEngine;

public class PathProvider : MonoBehaviour
{
    public Transform[] waypoints;
    
    public Vector3 GetPoint(int index) => waypoints[index].position;
    public int PointsCount => waypoints.Length;
}