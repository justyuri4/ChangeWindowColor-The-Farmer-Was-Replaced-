using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

/// <summary>
/// OpenCloseButtonの色変更の責務を持つコンポーネント。
/// ColorControllerからApplyColorメソッドが直接呼び出される。
/// </summary>
public class ButtonColorUpdater : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ButtonColorUpdater");

    // ComponentSetupHelperから参照が設定されるフィールド
    public Image TargetImage; // OpenCloseButtonのImage

    /// <summary>
    /// 初期化（保存された色を読み込み、ボタンの色を適用）を行います。
    /// ComponentSetupHelperから呼び出されます。
    /// </summary>
    public void Initialize()
    {
        if (TargetImage == null)
        {
            Logger.LogError("[ButtonColorUpdater] TargetImage (OpenCloseButton Image) is not set.");
            return;
        }

        // ColorDataPersistenceから保存データをロード
        ColorData savedData = ColorDataPersistence.LoadColor();
        Color savedColor = savedData.ToUnityColor();

        // 読み込んだ色をボタンのImageに適用 (新しいApplyColorメソッドを使用)
        ApplyColor(savedColor);

        Logger.LogInfo($"[ButtonColorUpdater] Initial color applied: {TargetImage.color}");
    }

    /// <summary>
    /// ColorControllerから呼び出され、指定された色をボタンに適用します。
    /// </summary>
    /// <param name="newColorRgb">適用する色 (RGB値のみを使用)</param>
    public void ApplyColor(Color newColorRgb)
    {
        if (TargetImage == null)
        {
            Logger.LogWarning("[ButtonColorUpdater] TargetImageが設定されていないため、色変更をスキップします。");
            return;
        }

        // ボタンのImageの元のアルファ値を保持しつつ、新しい色を適用
        // newColorRgbのアルファ値は無視し、TargetImageの既存のアルファ値を使用します。
        TargetImage.color = new Color(newColorRgb.r, newColorRgb.g, newColorRgb.b, TargetImage.color.a);

        Logger.LogDebug($"[ButtonColorUpdater] Button color updated to: {TargetImage.color}");
    }
}