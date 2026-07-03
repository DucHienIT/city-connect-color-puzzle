using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// One endpoint "building": a colored rounded block with a roof strip and windows.
    /// </summary>
    public class NodeView : MonoBehaviour
    {
        public int ColorIndex { get; private set; }

        public static NodeView Create(Transform parent, int colorIndex, Color color, Vector3 worldPos)
        {
            var go = new GameObject($"Node_{colorIndex}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var view = go.AddComponent<NodeView>();
            view.ColorIndex = colorIndex;

            view.AddPart("Base", SpriteFactory.RoundedSquare, Vector2.zero, new Vector2(0.8f, 0.8f), color, 10);

            var roof = Color.Lerp(color, Color.black, 0.35f);
            view.AddPart("Roof", SpriteFactory.RoundedSquare, new Vector2(0f, 0.27f), new Vector2(0.62f, 0.14f), roof, 11);

            var window = Color.Lerp(color, Color.white, 0.55f);
            var offsets = new[]
            {
                new Vector2(-0.16f, 0.04f), new Vector2(0.16f, 0.04f),
                new Vector2(-0.16f, -0.2f), new Vector2(0.16f, -0.2f),
            };
            foreach (var offset in offsets)
                view.AddPart("Window", SpriteFactory.Square, offset, new Vector2(0.13f, 0.13f), window, 11);

            return view;
        }

        void AddPart(string partName, Sprite sprite, Vector2 localPos, Vector2 scale, Color color, int order)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
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
