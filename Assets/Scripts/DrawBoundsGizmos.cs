using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class DrawBoundsGizmos : MonoBehaviour
{
#if UNITY_EDITOR
    public enum ColliderOrRenderer
    {
        Collider,
        Renderer
    }

    public enum DrawWhen
    {
        Always,
        Selected,
        ParentSelected
    }

    public ColliderOrRenderer type;
    public DrawWhen drawWhen;

    private static readonly Vector3 gizmosSize = Vector3.one * .1f;

    public void OnDrawGizmosSelected()
    {
        if (drawWhen == DrawWhen.ParentSelected)
        {
            DrawGizmos();
        }
        else if (drawWhen == DrawWhen.Selected && Selection.activeTransform == transform)
        {
            DrawGizmos();
        }
    }

    private void OnDrawGizmos()
    {
        if (drawWhen == DrawWhen.Always)
        {
            DrawGizmos();
        }
    }

    private void DrawGizmos()
    {
        try
        {
            Bounds B = type == ColliderOrRenderer.Renderer ? GetComponent<Renderer>().bounds : GetComponent<Collider>().bounds;
            DrawGizmosFor(B);
        }
        catch
        {
            Debug.Log("waduhek");
            // nothing to draw the bounds of!
        }
    }

    public static void DrawGizmosFor(Bounds B)
    {
        var xVals = new[] {
            B.max.x, B.min.x
        };
        var yVals = new[] {
            B.max.y, B.min.y
        };
        var zVals = new[] {
            B.max.z, B.min.z
        };

        for (int i = 0; i < xVals.Length; i++)
        {
            var x = xVals[i];
            for (int j = 0; j < yVals.Length; j++)
            {
                var y = yVals[j];
                for (int k = 0; k < zVals.Length; k++)
                {
                    var z = zVals[k];

                    var point = new Vector3(x, y, z);
                    Gizmos.DrawCube(point, gizmosSize);

                    if (i == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(xVals[1], y, z));
                    }
                    if (j == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(x, yVals[1], z));
                    }
                    if (k == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(x, y, zVals[1]));
                    }

                }
            }
        }
    }
#endif
}

