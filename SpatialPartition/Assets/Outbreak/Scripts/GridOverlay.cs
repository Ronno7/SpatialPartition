using UnityEngine;

namespace Outbreak
{
    //Draws the grid cells on the ground while the spatial partition is on
    public class GridOverlay : MonoBehaviour
    {
        public OutbreakController controller;
        public Material lineMaterial;

        [SerializeField, Min(0.01f)]
        float lineWidth = 0.08f;

        GameObject lines;


        void Start()
        {
            lines = new GameObject("Grid Lines");
            lines.transform.SetParent(transform, false);

            float mapSize = controller.MapSize;
            float cellSize = controller.CellSize;

            //Slightly above the ground so the lines don't flicker
            const float y = 0.02f;

            int cellsPerSide = Mathf.CeilToInt(mapSize / cellSize);

            for (int i = 0; i <= cellsPerSide; i++)
            {
                //The last cell can stick out past the edge of the map, so stop the lines at the edge
                float d = Mathf.Min(i * cellSize, mapSize);

                AddLine(new Vector3(d, y, 0f), new Vector3(d, y, mapSize));
                AddLine(new Vector3(0f, y, d), new Vector3(mapSize, y, d));
            }
        }


        void LateUpdate()
        {
            if (lines.activeSelf != controller.UseSpatialPartition)
            {
                lines.SetActive(controller.UseSpatialPartition);
            }
        }


        void AddLine(Vector3 from, Vector3 to)
        {
            LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();

            line.transform.SetParent(lines.transform, false);

            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);

            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            line.sharedMaterial = lineMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
