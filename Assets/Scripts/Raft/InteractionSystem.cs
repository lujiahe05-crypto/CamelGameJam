using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    const float InteractRange = 4f;

    IInteractable currentTarget;
    string currentHint;

    void Update()
    {
        if (RaftUI.IsUIOpen)
        {
            ClearHint();
            return;
        }

        currentTarget = null;
        currentHint = null;

        var cam = Camera.main;
        if (cam == null) return;

        var inv = RaftGame.Instance != null ? RaftGame.Instance.Inv : null;
        if (inv == null) return;

        ItemType heldItem = inv.GetSelectedItemType();
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        // Raycast for interactable objects
        if (Physics.Raycast(ray, out RaycastHit hit, InteractRange))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                currentHint = interactable.GetInteractionHint(heldItem);
            }
        }

        // Special case: empty cup + looking at water
        if (currentTarget == null && heldItem == ItemType.EmptyCup)
        {
            Plane waterPlane = new Plane(Vector3.up, new Vector3(0, RaftGame.WaterLevel, 0));
            if (waterPlane.Raycast(ray, out float waterDist) && waterDist <= InteractRange)
            {
                currentHint = "\u6309E\u8200\u53d6\u6d77\u6c34";
            }
        }

        // Display hint
        if (RaftGame.Instance.UI != null)
            RaftGame.Instance.UI.SetInteractionHint(currentHint);

        // E key interaction
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTarget != null)
            {
                currentTarget.Interact(heldItem);
            }
            else if (heldItem == ItemType.EmptyCup)
            {
                // Scoop seawater
                Plane waterPlane = new Plane(Vector3.up, new Vector3(0, RaftGame.WaterLevel, 0));
                if (waterPlane.Raycast(ray, out float waterDist) && waterDist <= InteractRange)
                {
                    inv.Remove(ItemType.EmptyCup, 1);
                    inv.Add(ItemType.SeawaterCup, 1);
                    if (RaftGame.Instance.UI != null)
                        RaftGame.Instance.UI.ShowToast("\u5df2\u88c5\u53d6\u6d77\u6c34");
                }
            }
        }
    }

    void ClearHint()
    {
        if (currentHint != null)
        {
            currentHint = null;
            if (RaftGame.Instance != null && RaftGame.Instance.UI != null)
                RaftGame.Instance.UI.SetInteractionHint(null);
        }
    }
}
