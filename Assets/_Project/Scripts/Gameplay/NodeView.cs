using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// One board building. Cities are big blocks whose roof plate carries one pin
    /// per house they demand; houses are small blocks with a tinted car parked on
    /// the roof. Both pulse as feedback when their color completes.
    /// </summary>
    public class NodeView : MonoBehaviour
    {
        public int ColorIndex { get; private set; }

        public static NodeView CreateCity(Transform parent, int colorIndex, Color color, Vector3 worldPos, int houseCount)
        {
            const float bodyHeight = 0.55f;
            var view = CreateBase(parent, $"City_{colorIndex}", colorIndex, color, worldPos,
                0.88f, bodyHeight, 0.2f, roofSize: 0.72f, doorWidth: 0.22f);
            var t = view.transform;

            var plateColor = new Color(0.97f, 0.95f, 0.91f);
            float plateY = bodyHeight + 0.12f;
            const float pinSpacing = 0.19f;
            int pins = Mathf.Clamp(houseCount, 1, 4);
            Mesh plateMesh = pins == 1
                ? MeshFactory.Disc(0.16f, 0.03f, 20)
                : MeshFactory.RoundedBlock(pinSpacing * (pins - 1) + 0.3f, 0.03f, 0.3f, 0.15f, 5);
            var plate = MeshFactory.CreatePart(t, "PinPlate", plateMesh, plateColor);
            plate.transform.localPosition = new Vector3(0f, plateY, 0f);

            var dotMesh = MeshFactory.Disc(0.075f, 0.05f, 16);
            var dotColor = Color.Lerp(color, Color.black, 0.1f);
            for (int i = 0; i < pins; i++)
            {
                var pin = MeshFactory.CreatePart(t, "PinDot", dotMesh, dotColor);
                pin.transform.localPosition = new Vector3((i - (pins - 1) * 0.5f) * pinSpacing, plateY, 0f);
            }
            return view;
        }

        public static NodeView CreateHouse(Transform parent, int colorIndex, Color color, Vector3 worldPos)
        {
            const float bodyHeight = 0.36f;
            var view = CreateBase(parent, $"House_{colorIndex}", colorIndex, color, worldPos,
                0.62f, bodyHeight, 0.15f, roofSize: 0.5f, doorWidth: 0.15f);

            // A little tinted car parked on the roof marks this as a house to route.
            var roofCar = new GameObject("RoofCar");
            roofCar.transform.SetParent(view.transform, false);
            roofCar.transform.localPosition = new Vector3(0f, bodyHeight + 0.1f, 0f);
            roofCar.transform.localRotation = Quaternion.Euler(0f, 65f, 0f);
            roofCar.transform.localScale = Vector3.one * 0.62f;
            MeshFactory.CreatePart(roofCar.transform, "Body", MeshFactory.CarBody(), color);
            MeshFactory.CreatePart(roofCar.transform, "Details", MeshFactory.CarDetails(), Color.white);
            return view;
        }

        static NodeView CreateBase(Transform parent, string name, int colorIndex, Color color, Vector3 worldPos,
            float footprint, float bodyHeight, float cornerRadius, float roofSize, float doorWidth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var view = go.AddComponent<NodeView>();
            view.ColorIndex = colorIndex;
            var t = go.transform;

            MeshFactory.CreatePart(t, "Body",
                MeshFactory.RoundedBlock(footprint, bodyHeight, footprint, cornerRadius, 5), color);

            var roof = MeshFactory.CreatePart(t, "Roof",
                MeshFactory.RoundedBlock(roofSize, 0.12f, roofSize, cornerRadius * 0.8f, 5),
                Color.Lerp(color, Color.black, 0.22f)); // keep the hue readable on the roof
            roof.transform.localPosition = new Vector3(0f, bodyHeight, 0f);

            var door = MeshFactory.CreatePart(t, "Door",
                MeshFactory.Block(doorWidth, 0.16f, 0.08f), new Color(0.96f, 0.94f, 0.89f));
            door.transform.localPosition = new Vector3(0f, 0.05f, -footprint * 0.5f + 0.02f); // above the road surface

            return view;
        }

        public void Pulse()
        {
            transform.DOKill(true);
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.22f, 0.3f, 8, 0.7f);
        }

        void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
