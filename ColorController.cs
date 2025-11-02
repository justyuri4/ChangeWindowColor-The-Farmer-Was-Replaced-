using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

/// <summary>
/// スライダーからの通知を受け取り、UI (パネル)、ButtonColorUpdater、CodeWindowColorChanger
/// のメソッドを呼び出して、色変更を統合的に管理するコントローラ。
/// </summary>
public class ColorController : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ColorController");

    // ComponentSetupHelperから参照が設定されるフィールド
    public Image TargetImage; // メインパネルのImage

    // ★新規: ButtonColorUpdaterの参照 (メソッド呼び出し用)
    public ButtonColorUpdater ButtonUpdater;

    // ★SliderColorUpdaterとWorkspaceの参照をComponentSetupHelperから設定できるように追加
    public SliderColorUpdater SliderUpdater;
    public Workspace WorkspaceInstance;

    /// <summary>
    /// 初期化（主にスライダーにイベントを登録し、保存された色を適用）を行います。
    /// ComponentSetupHelperから呼び出されます。
    /// </summary>
    public void Initialize()
    {
        // ★修正: スライダーへのイベント登録は SliderColorUpdater.Initialize() に移動

        // 起動時に保存された色をロードし、適用する
        LoadAndApplySavedColorToAllComponents();

        Logger.LogInfo("[ColorController] Initialized.");
    }

    /// <summary>
    /// ファイルから色設定を読み込み、パネル、ボタン、CodeWindowに適用します。
    /// </summary>
    private void LoadAndApplySavedColorToAllComponents()
    {
        // ColorDataPersistenceから保存データをロード
        ColorData savedData = ColorDataPersistence.LoadColor();
        Color savedColor = savedData.ToUnityColor();

        // 1. TargetImage (パネル)に色を適用
        if (TargetImage != null)
        {
            TargetImage.color = savedColor;
        }

        // 2. ButtonColorUpdaterにボタンの更新を委譲
        if (ButtonUpdater != null)
        {
            ButtonUpdater.ApplyColor(savedColor); // ★呼び出し
        }

        // 3. CodeWindowにも色を適用 (CodeWindowが開いている場合に即時反映)
        // 今後開かれるCodeWindowのために、まず静的プロパティを更新
        CodeWindowColorPatcher.SetSelectedColor(savedColor);

        // 開いているCodeWindowがあれば、全てに色を適用
        if (WorkspaceInstance != null)
        {
            CodeWindowColorPatcher.ApplyColorToAllOpenWindows(WorkspaceInstance, savedColor);
        }

        // ★スライダーの値設定は SliderColorUpdater.Initialize() で行われます。

        Logger.LogInfo($"[ColorController] Applied saved color to all components: {savedColor}");
    }

    /// <summary>
    /// SliderColorUpdaterから新しい色を受信したときに、全コンポーネントの更新と永続化をトリガーします。
    /// このメソッドは SliderColorUpdater によって直接呼び出されます。
    /// </summary>
    /// <param name="newColorRgb">スライダーから計算された新しい色 (アルファ値は無視または1.0f)</param>
    public void OnNewColorSet(Color newColorRgb)
    {
        if (TargetImage == null)
        {
            Logger.LogWarning("[ColorController] TargetImageが設定されていないため、色変更をスキップします。");
            return;
        }

        // TargetImageの元のアルファ値を保持した最終的な色
        Color finalColor = new Color(newColorRgb.r, newColorRgb.g, newColorRgb.b, TargetImage.color.a);

        // 1. TargetImage (パネル) の色を更新
        TargetImage.color = finalColor;

        // 2. ButtonColorUpdaterにボタンの更新を委譲
        if (ButtonUpdater != null)
        {
            ButtonUpdater.ApplyColor(finalColor); // ★呼び出し
        }

        // 3. データをファイルに保存する
        ColorData newData = new ColorData(finalColor);
        ColorDataPersistence.SaveColor(newData);

        // 4. 開いているCodeWindowの色も更新する
        // 今後開かれるCodeWindowのために、まず静的プロパティを更新
        CodeWindowColorPatcher.SetSelectedColor(finalColor);

        // 開いているCodeWindowがあれば、全てに色を適用
        if (WorkspaceInstance != null)
        {
            CodeWindowColorPatcher.ApplyColorToAllOpenWindows(WorkspaceInstance, finalColor);
        }

        Logger.LogDebug($"[ColorController] Color updated to: {finalColor}");
    }
}