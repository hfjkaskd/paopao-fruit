using UnityEngine;

/// <summary>Mirrors the existing business selection into the prefab's matching border.</summary>
public sealed class OrchardSelectionVisual : MonoBehaviour
{
    [SerializeField] private GameObject border;
    private void OnEnable() { Refresh(); }
    private void OnDisable() { if (border != null) border.SetActive(false); }
    public void Refresh()
    {
        if (border != null) border.SetActive(gameObject.activeInHierarchy);
    }
}
