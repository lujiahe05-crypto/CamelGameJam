using System.Collections.Generic;
using UnityEngine;

public class ThronefallCommandSystem : MonoBehaviour
{
    List<ThronefallAlly> allAllies = new List<ThronefallAlly>();
    List<ThronefallAlly> selectedAllies = new List<ThronefallAlly>();

    bool isDragging;
    Vector3 dragStart;
    const float DragThreshold = 10f;

    public bool HasSelection => selectedAllies.Count > 0;

    public void RegisterAlly(ThronefallAlly ally)
    {
        if (!allAllies.Contains(ally))
            allAllies.Add(ally);
    }

    public void UnregisterAlly(ThronefallAlly ally)
    {
        allAllies.Remove(ally);
        selectedAllies.Remove(ally);
    }

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;
        if (game.CurrentPhase == ThronefallGame.GamePhase.GameOver) return;
        if (game.UI != null && game.UI.IsBranchPanelOpen) return;

        CleanDestroyedAllies();

        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            float dist = Vector2.Distance(Input.mousePosition, dragStart);
            if (dist > DragThreshold)
            {
                isDragging = true;
                if (game.UI != null)
                    game.UI.ShowSelectionRect(dragStart, Input.mousePosition);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                BoxSelect(dragStart, Input.mousePosition);
                if (game.UI != null)
                    game.UI.HideSelectionRect();
            }
            else
            {
                ClickSelect(Input.mousePosition);
            }
            isDragging = false;
        }

        if (Input.GetMouseButtonDown(1) && selectedAllies.Count > 0)
        {
            SelectedFollowHero();
        }

        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            && selectedAllies.Count > 0)
        {
            StationSelected();
        }
    }

    void BoxSelect(Vector2 start, Vector2 end)
    {
        Rect screenRect = GetScreenRect(start, end);

        foreach (var ally in allAllies)
        {
            if (ally != null && ally.IsAlive)
                ally.SetSelected(false);
        }
        selectedAllies.Clear();

        var cam = Camera.main;
        if (cam == null) return;

        foreach (var ally in allAllies)
        {
            if (ally == null || !ally.IsAlive) continue;
            Vector3 screenPos = cam.WorldToScreenPoint(ally.Position);
            if (screenPos.z > 0 && screenRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                selectedAllies.Add(ally);
                ally.SetSelected(true);
            }
        }
    }

    void ClickSelect(Vector2 mousePos)
    {
        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var ally = hit.collider.GetComponent<ThronefallAlly>();
            if (ally != null && ally.IsAlive)
            {
                bool wasSelected = ally.IsSelected;

                foreach (var a in selectedAllies)
                {
                    if (a != null) a.SetSelected(false);
                }
                selectedAllies.Clear();

                if (!wasSelected)
                {
                    ally.SetSelected(true);
                    selectedAllies.Add(ally);
                }
                return;
            }
        }

        foreach (var a in selectedAllies)
        {
            if (a != null) a.SetSelected(false);
        }
        selectedAllies.Clear();
    }

    void SelectedFollowHero()
    {
        for (int i = 0; i < selectedAllies.Count; i++)
        {
            if (selectedAllies[i] != null && selectedAllies[i].IsAlive)
                selectedAllies[i].SetFollowing(i);
        }
    }

    void StationSelected()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.Player == null) return;

        Vector3 heroPos = game.Player.Position;
        int count = selectedAllies.Count;

        for (int i = 0; i < count; i++)
        {
            if (selectedAllies[i] == null || !selectedAllies[i].IsAlive) continue;
            Vector3 offset = GetFormationOffset(i, count);
            selectedAllies[i].SetStationed(heroPos + offset);
            selectedAllies[i].SetSelected(false);
        }
        selectedAllies.Clear();
    }

    Vector3 GetFormationOffset(int index, int total)
    {
        if (total <= 1) return Vector3.zero;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(total));
        int row = index / cols;
        int col = index % cols;
        float offsetX = (col - (cols - 1) * 0.5f) * 2f;
        float offsetZ = -row * 2f;
        return new Vector3(offsetX, 0, offsetZ);
    }

    Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        float xMin = Mathf.Min(a.x, b.x);
        float xMax = Mathf.Max(a.x, b.x);
        float yMin = Mathf.Min(a.y, b.y);
        float yMax = Mathf.Max(a.y, b.y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    public void DeselectAll()
    {
        foreach (var ally in selectedAllies)
        {
            if (ally != null) ally.SetSelected(false);
        }
        selectedAllies.Clear();
    }

    void CleanDestroyedAllies()
    {
        for (int i = allAllies.Count - 1; i >= 0; i--)
        {
            if (allAllies[i] == null || !allAllies[i].IsAlive)
                allAllies.RemoveAt(i);
        }
        for (int i = selectedAllies.Count - 1; i >= 0; i--)
        {
            if (selectedAllies[i] == null || !selectedAllies[i].IsAlive)
                selectedAllies.RemoveAt(i);
        }
    }
}
