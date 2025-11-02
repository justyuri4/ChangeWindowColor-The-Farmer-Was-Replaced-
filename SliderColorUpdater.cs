using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

/// <summary>
/// スライダーの値を管理し、色変更時にColorControllerへ通知するコンポーネント。
/// 色変更の計算とColorControllerへの直接的な通知の責務を持つ。
/// </summary>
public class SliderColorUpdater : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("SliderColorUpdater");

    // ComponentSetupHelperから参照が設定されるフィールド
    public Slider RedSlider;
    public Slider GreenSlider;
    public Slider BlueSlider;

    // ★新規: ColorControllerの参照 (メソッド呼び出し用)
    public ColorController Controller;

    /// <summary>
    /// 初期化（主にスライダーにイベントを登録し、保存された色を適用）を行います。
    /// ComponentSetupHelperから呼び出されます。
    /// </summary>
    public void Initialize()
    {
        RegisterSliderListeners();
        // 起動時のスライダーの値設定は、Controller側でロードされた色に基づいて行われます。
        LoadAndApplySavedColorToSliders();

        Logger.LogInfo("[SliderColorUpdater] Initialized and listeners registered.");
    }

    private void RegisterSliderListeners()
    {
        // 既存のリスナーを全て削除し、新しいリスナーを登録
        if (RedSlider != null)
        {
            RedSlider.onValueChanged.RemoveAllListeners();
            RedSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        if (GreenSlider != null)
        {
            GreenSlider.onValueChanged.RemoveAllListeners();
            GreenSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        if (BlueSlider != null)
        {
            BlueSlider.onValueChanged.RemoveAllListeners();
            BlueSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// ファイルから色設定を読み込み、スライダーの値に適用します。
    /// </summary>
    private void LoadAndApplySavedColorToSliders()
    {
        // ColorDataPersistenceから保存データをロード
        ColorData savedData = ColorDataPersistence.LoadColor();
        Color savedColor = savedData.ToUnityColor();

        ApplyColorToSliders(savedColor);
    }

    /// <summary>
    /// 指定された色をスライダーの値に適用します。
    /// </summary>
    /// <param name="color">適用する色。</param>
    private void ApplyColorToSliders(Color color)
    {
        // スライダーのMin/Max値に基づいて値を設定
        if (RedSlider != null)
        {
            if (RedSlider.maxValue == 1f && RedSlider.minValue == 0f) RedSlider.value = Mathf.Clamp01(color.r);
            else RedSlider.value = color.r;
        }
        if (GreenSlider != null)
        {
            if (GreenSlider.maxValue == 1f && GreenSlider.minValue == 0f) GreenSlider.value = Mathf.Clamp01(color.g);
            else GreenSlider.value = color.g;
        }
        if (BlueSlider != null)
        {
            if (BlueSlider.maxValue == 1f && BlueSlider.minValue == 0f) BlueSlider.value = Mathf.Clamp01(color.b);
            else BlueSlider.value = color.b;
        }
    }

    /// <summary>
    /// スライダーの値が変更されたときに色を計算し、ColorControllerのメソッドを直接呼び出します。
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        if (RedSlider == null || GreenSlider == null || BlueSlider == null || Controller == null)
        {
            Logger.LogWarning("[SliderColorUpdater] UI要素またはControllerが設定されていないため、色変更をスキップします。");
            return;
        }

        // スライダーの値を使って新しいColorを構成 (アルファ値は1.0fとして計算)
        Color newColor = new Color(RedSlider.value, GreenSlider.value, BlueSlider.value, 1.0f);

        // ★修正: ColorControllerの公開メソッドを直接呼び出す
        Controller.OnNewColorSet(newColor);

        Logger.LogDebug($"[SliderColorUpdater] Calculated new color: {newColor}");
    }
}