using UnityEngine;
using System.IO;
using BepInEx.Logging;

/// <summary>
/// RGBデータをファイルに保存・読み込みするための構造体
/// 【重要】JsonUtilityでシリアライズ可能にするため、[Serializable]属性とpublicフィールドが必要です。
/// </summary>
[System.Serializable]
public struct ColorData
{
    public float r;
    public float g;
    public float b;
    public float a; // 透明度も保存できるようにAも追加

    public ColorData(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToUnityColor()
    {
        return new Color(r, g, b, a);
    }
}

/// <summary>
/// ColorDataをJSONファイルとして保存・読み込みする静的ヘルパークラス。
/// JsonUtilityを使用してUnity標準のJSON処理を行います。
/// </summary>
public static class ColorDataPersistence
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ColorPersistence");
    private static readonly string DataFileName = "menu_color_settings.json";

    /// <summary>
    /// ColorDataをファイルに保存します。
    /// </summary>
    /// <param name="data">保存するColorData。</param>
    public static void SaveColor(ColorData data)
    {
        string filePath = Path.Combine(BepInEx.Paths.ConfigPath, DataFileName);

        try
        {
            // UnityのJsonUtilityを使用してオブジェクトをJSON文字列に変換
            string json = JsonUtility.ToJson(data, true); // trueで整形されたJSONを出力
            File.WriteAllText(filePath, json);
            Logger.LogInfo($"[Persistence] Color data saved to: {filePath}");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"[Persistence] Failed to save color data: {ex.Message}");
        }
    }

    /// <summary>
    /// ファイルからColorDataを読み込みます。ファイルがない場合はデフォルト値を返します。
    /// </summary>
    /// <returns>読み込まれたColorData、またはデフォルト値（白）。</returns>
    public static ColorData LoadColor()
    {
        string filePath = Path.Combine(BepInEx.Paths.ConfigPath, DataFileName);

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);

                // UnityのJsonUtilityを使用してJSON文字列からオブジェクトに変換
                ColorData data = JsonUtility.FromJson<ColorData>(json);
                Logger.LogInfo("[Persistence] Color data loaded successfully.");
                data.a = 1.0f;
                return data;
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[Persistence] Failed to load or deserialize color data: {ex.Message}. Returning default color.");
                // ファイルが壊れている場合はデフォルト値を返す
            }
        }

        // ファイルが存在しない、またはロードに失敗した場合はデフォルトの色（白）を返す
        Logger.LogInfo("[Persistence] Color data file not found. Returning default color (White).");
        return new ColorData(Color.white);
    }
}
