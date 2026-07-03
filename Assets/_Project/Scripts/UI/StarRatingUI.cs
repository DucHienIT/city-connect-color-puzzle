using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>Row of three stars; the middle one sits slightly higher, trophy-style.</summary>
    public class StarRatingUI
    {
        public RectTransform Root { get; }

        readonly Image[] stars = new Image[3];

        static readonly Color Gold = new Color(1f, 0.8f, 0.18f);
        static readonly Color Gray = new Color(0.27f, 0.30f, 0.40f);

        public StarRatingUI(Transform parent, float starSize, float spacing)
        {
            Root = UIFactory.CreateRect(parent, "Stars");
            for (int i = 0; i < 3; i++)
            {
                var rt = UIFactory.CreateRect(Root, $"Star_{i}");
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = SpriteFactory.Star;
                img.color = Gray;
                img.raycastTarget = false;
                float lift = i == 1 ? starSize * 0.22f : 0f;
                float scale = i == 1 ? 1.2f : 1f;
                UIFactory.Place(rt, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * spacing, lift),
                    new Vector2(starSize * scale, starSize * scale));
                stars[i] = img;
            }
        }

        public void Set(int count, bool animate)
        {
            for (int i = 0; i < 3; i++)
            {
                var img = stars[i];
                bool lit = i < count;
                img.transform.DOKill();
                img.color = lit ? Gold : Gray;
                img.transform.localScale = Vector3.one;
                if (lit && animate)
                {
                    img.transform.localScale = Vector3.zero;
                    img.transform.DOScale(1f, 0.35f)
                        .SetDelay(0.25f + i * 0.18f)
                        .SetEase(Ease.OutBack);
                }
            }
        }
    }
}
