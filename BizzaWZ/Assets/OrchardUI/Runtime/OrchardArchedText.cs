using TMPro;
using UnityEngine;

/// <summary>Applies the prefab's shallow title arch when TMP rebuilds its glyph mesh.</summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public sealed class OrchardArchedText : MonoBehaviour
{
    [SerializeField] private float archHeight = 18f;
    private TMP_Text label;
    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        label.OnPreRenderText += Curve;
        label.SetVerticesDirty();
    }
    private void OnDisable()
    {
        if (label == null) return;
        label.OnPreRenderText -= Curve;
        label.SetVerticesDirty();
    }
    private void Curve(TMP_TextInfo info)
    {
        float halfWidth = label.rectTransform.rect.width * .5f;
        if (halfWidth <= 0) return;
        float center = label.rectTransform.rect.center.x;
        for (int i = 0; i < info.characterCount; i++)
        {
            var character = info.characterInfo[i];
            if (!character.isVisible) continue;
            var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            int start = character.vertexIndex;
            float x = (vertices[start].x + vertices[start + 2].x) * .5f;
            float normalized = (x - center) / halfWidth;
            float lift = archHeight * (1 - normalized * normalized);
            float angle = Mathf.Atan(-2 * archHeight * normalized / halfWidth);
            float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
            for (int vertex = start; vertex < start + 4; vertex++)
            {
                var point = vertices[vertex];
                float dx = point.x - x, dy = point.y - character.baseLine;
                vertices[vertex] = new Vector3(x + cos * dx - sin * dy, character.baseLine + lift + sin * dx + cos * dy, point.z);
            }
        }
    }
}
