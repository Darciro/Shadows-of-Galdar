using UnityEngine;
using UnityEngine.Tilemaps; // Required if you want to ensure a Tilemap exists

/// <summary>
/// A simple marker component to identify GameObjects that contain wall Tilemaps
/// intended for transparency effects.
/// Attach this to the same GameObject as your wall Tilemap.
/// </summary>
[RequireComponent(typeof(Tilemap))] // Ensures a Tilemap is present on this GameObject
public class MarkAsTransparentWall : MonoBehaviour
{
    // This script doesn't need any specific logic, it's just a tag.
    // You could add properties here if you want per-wall-tilemap settings
    // that the WallTransparencyController could read.
}
