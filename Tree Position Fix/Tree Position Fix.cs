using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace TreePositionFix
{
    [StaticConstructorOnStartup]
    public static class TreePositionFix
    {
        static TreePositionFix()
        {
            var harmony = new Harmony("Emperor.TreePositionFix");
            harmony.PatchAll();
            Log.Message("[TreePositionFix] Harmony patches applied.");
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Print))]
    public static class Patch_Plant_Print
    {
        private static readonly FieldInfo GrowthIntField =
            AccessTools.Field(typeof(Plant), "growthInt");

        private const float ZOffset = 0.45f;

        public static bool Prefix(Plant __instance, SectionLayer layer)
        {
            float growthInt = (float)GrowthIntField.GetValue(__instance);

            Vector3 trueCenter = __instance.TrueCenter();
            Map map = __instance.Map;
            IntVec3 position = __instance.Position;
            bool snowCover = position.GetSnowDepth(map) > 0.8f;

            Rand.PushState();
            Rand.Seed = position.GetHashCode();

            int meshCountToDraw = Mathf.CeilToInt(growthInt * (float)__instance.def.plant.maxMeshCount);
            if (meshCountToDraw < 1)
                meshCountToDraw = 1;

            float visualSize = __instance.def.plant.visualSizeRange.LerpThroughRange(growthInt);
            float drawSizeScaled = __instance.def.graphicData.drawSize.x * visualSize;

            int drawnCount = 0;
            int[] positionIndices = PlantPosIndices.GetPositionIndices(__instance);
            bool shadowClampFlag = false;

            for (int i = 0; i < positionIndices.Length; i++)
            {
                int posIndex = positionIndices[i];
                Vector3 drawPos;

                if (__instance.def.plant.maxMeshCount != 1)
                {
                    int gridSize = 1;
                    switch (__instance.def.plant.maxMeshCount)
                    {
                        case 1: gridSize = 1; break;
                        case 4: gridSize = 2; break;
                        case 9: gridSize = 3; break;
                        case 16: gridSize = 4; break;
                        case 25: gridSize = 5; break;
                        default:
                            Log.Error(__instance.def?.ToString() +
                                " must have plant.MaxMeshCount that is a perfect square.");
                            gridSize = 3;
                            break;
                    }

                    float cellFraction = 1f / (float)gridSize;
                    drawPos = position.ToVector3();
                    drawPos.y = __instance.def.Altitude;
                    drawPos.x += 0.5f * cellFraction;
                    drawPos.z += 0.5f * cellFraction;
                    int row = posIndex / gridSize;
                    int col = posIndex % gridSize;
                    drawPos.x += (float)row * cellFraction;
                    drawPos.z += (float)col * cellFraction;
                    float jitter = cellFraction * 0.3f;
                    drawPos += Gen.RandomHorizontalVector(jitter);
                }
                else
                {
                    drawPos = trueCenter + Gen.RandomHorizontalVector(0.05f);
                    float cellBottom = (float)position.z;
                    if (drawPos.z - visualSize / 2f < cellBottom)
                    {
                        drawPos.z = cellBottom + visualSize / 2f;
                        shadowClampFlag = true;
                    }
                    if (__instance.def.plant.IsTree)
                        drawPos.z += ZOffset;
                }

                bool flip = Rand.Bool;

                Material snowMat = null;
                if (snowCover)
                {
                    Graphic snowGraphic = __instance.SnowOverlayGraphic;
                    if (snowGraphic != null)
                        snowMat = snowGraphic.MatSingleFor(__instance);
                }

                Material plantMat = __instance.Graphic.MatSingleFor(__instance);
                Graphic_Random graphicRandom = __instance.Graphic as Graphic_Random;
                if (graphicRandom != null)
                {
                    int subCount = graphicRandom.SubGraphicsCount;
                    int subIndex = Rand.Range(0, subCount);
                    plantMat = graphicRandom.SubGraphicAtIndex(subIndex).MatSingle;
                    if (snowCover)
                    {
                        Graphic_Random snowRandom = __instance.SnowOverlayGraphic as Graphic_Random;
                        if (snowRandom != null)
                            snowMat = snowRandom.SubGraphicAtIndex(subIndex).MatSingle;
                    }
                }

                Vector2[] uvs;
                Color32 color;
                Graphic.TryGetTextureAtlasReplacementInfo(
                    plantMat, __instance.def.category.ToAtlasGroup(),
                    flip, false, out plantMat, out uvs, out color);

                Color32[] windColors = new Color32[4];
                PlantUtility.SetWindExposureColors(windColors, __instance);

                Vector2 size = new Vector2(drawSizeScaled, drawSizeScaled);
                Printer_Plane.PrintPlane(
                    layer, drawPos, size, plantMat, 0f, flip, uvs, windColors,
                    0.1f, (float)(__instance.HashOffset() % 1024));

                if (snowCover && snowMat != null)
                {
                    Graphic.TryGetTextureAtlasReplacementInfo(
                        snowMat, __instance.def.category.ToAtlasGroup(),
                        flip, false, out snowMat, out uvs, out color);
                    Printer_Plane.PrintPlane(
                        layer, drawPos.WithYOffset(0.0003658537f), size, snowMat,
                        0f, flip, uvs, windColors,
                        0.1f, (float)(__instance.HashOffset() % 1024));
                }

                drawnCount++;
                if (drawnCount >= meshCountToDraw)
                    break;
            }

            if (__instance.def.graphicData.shadowData != null)
            {
                Vector3 shadowPos = trueCenter
                    + __instance.def.graphicData.shadowData.offset * visualSize;

                if (shadowClampFlag)
                {
                    shadowPos.z = position.ToVector3Shifted().z
                        + __instance.def.graphicData.shadowData.offset.z;
                }

                if (__instance.def.plant.maxMeshCount == 1 && __instance.def.plant.IsTree)
                {
                    if (shadowClampFlag)
                    {
                        shadowPos.z = position.ToVector3Shifted().z
                            + __instance.def.graphicData.shadowData.offset.z
                            + ZOffset;
                    }
                    else
                    {
                        shadowPos.z += ZOffset;
                    }
                }

                shadowPos.y -= 0.03658537f;
                Vector3 shadowVolume = __instance.def.graphicData.shadowData.volume * visualSize;
                Printer_Shadow.PrintShadow(layer, shadowPos, shadowVolume, Rot4.North);
            }

            Rand.PopState();
            return false;
        }
    }
}