
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

#if UNITY_EDITOR
public class TagObject : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // Sets the text color
        UnityEditor.Handles.color = Color.red;

        // Draws the label on the position of the game object
        UnityEditor.Handles.Label(transform.position, "SPAWN");
    }
}
#endif
