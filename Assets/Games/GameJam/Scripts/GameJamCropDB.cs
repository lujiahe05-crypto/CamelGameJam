using UnityEngine;

public class GameJamCropStageDef
{
    public float progress;
    public string prefabPath;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localScale = Vector3.one;
}

public class GameJamCropDef
{
    public string cropId;
    public string displayName;
    public string seedItemId;
    public float growDuration;
    public GameJamHarvestReward[] harvestRewards;
    public GameJamCropStageDef[] stages;
}

public static class GameJamCropDB
{
    public const string PlanterItemId = "种植盆";
    public const string RadishSeedItemId = "萝卜种子";
    public const string RadishItemId = "萝卜";

    static GameJamCropDef defaultCrop;

    public static GameJamCropDef DefaultCrop
    {
        get
        {
            if (defaultCrop == null)
            {
                defaultCrop = new GameJamCropDef
                {
                    cropId = "radish",
                    displayName = RadishItemId,
                    seedItemId = RadishSeedItemId,
                    growDuration = 3f,
                    harvestRewards = new[]
                    {
                        new GameJamHarvestReward(RadishItemId, 3),
                    },
                    stages = new[]
                    {
                        new GameJamCropStageDef
                        {
                            progress = 0.15f,
                            prefabPath = "Games/GameJam/assets/Model/plant/Plant_radish_1.prefab",
                            localPosition = new Vector3(0f, 0.12f, 0f),
                            localScale = new Vector3(0.6f, 0.6f, 0.6f)
                        },
                        new GameJamCropStageDef
                        {
                            progress = 0.66f,
                            prefabPath = "Games/GameJam/assets/Model/plant/Plant_radish.prefab",
                            localPosition = new Vector3(0f, 0.14f, 0f),
                            localScale = new Vector3(0.9f, 0.9f, 0.9f)
                        },
                        new GameJamCropStageDef
                        {
                            progress = 1f,
                            prefabPath = "Games/GameJam/assets/Model/plant/Plant_radish.prefab",
                            localPosition = new Vector3(0f, 0.14f, 0f),
                            localScale = Vector3.one
                        }
                    }
                };
            }

            return defaultCrop;
        }
    }
}
