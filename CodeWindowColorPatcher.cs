using UnityEngine;
using UnityEngine.UI; // Imageコンポーネント用
using HarmonyLib; // HarmonyパッチとAccessTools用
using BepInEx.Logging;
using System.Collections.Generic; // Dictionary用
using System.Reflection; // BindingFlags用

// CodeWindowの色変更を管理する静的ヘルパークラス
// ★メインのパッチ: CodeWindowのオープン時
[HarmonyPatch(typeof(Workspace), "OpenCodeWindow")]
public static class CodeWindowColorPatcher
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CodeWindowColorPatcher");

    // 最後に選択された色を保持する静的プロパティ
    public static Color LastSelectedColor { get; private set; } = Color.white;

    public static void SetSelectedColor(Color color)
    {
        LastSelectedColor = color;
        Logger.LogInfo($"[CodeWindowColorPatcher] LastSelectedColor updated to: {color}");
    }

    public static void ApplyColorToAllOpenWindows(Workspace workspaceInstance, Color color)
    {
        if (workspaceInstance == null)
        {
            Logger.LogError("WorkspaceInstanceがnullです。開いているCodeWindowに色を適用できません。");
            return;
        }

        foreach (var pair in workspaceInstance.codeWindows)
        {
            CodeWindow codeWindow = pair.Value;
            if (codeWindow != null)
            {
                ApplyColorToSingleCodeWindow(codeWindow, color);
            }
        }
    }

    private static void ApplyColorToSingleCodeWindow(CodeWindow newCodeWindow, Color newColor)
    {
        if (newCodeWindow == null)
        {
            Logger.LogError("CodeWindowがnullです。色を変更できません。");
            return;
        }

        // 1. Imageコンポーネントの色を新しい色に変更
        Image imageComponent = newCodeWindow.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = newColor;
        }

        // 2. CodeWindowのプライベートフィールド 'originalColor' をリフレクションで更新
        try
        {
            AccessTools.Field(typeof(CodeWindow), "originalColor").SetValue(newCodeWindow, newColor);
            Logger.LogDebug($"CodeWindow '{newCodeWindow.fileName}' の色を {newColor} に変更しました。 (originalColor更新)");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"CodeWindowの'originalColor'フィールド更新中にエラーが発生しました: {ex.Message}");
        }
    }

    // OpenCodeWindow 実行後にこのメソッドが呼ばれる
    public static void Postfix(Workspace __instance, string fileName, string code, Vector2 offset, Vector2 size)
    {
        Color colorToApply = LastSelectedColor;
        // 新しいウィンドウのオープンをトリガーとして、開いている全てのCodeWindowに色を再適用
        ApplyColorToAllOpenWindows(__instance, colorToApply);
        Logger.LogDebug($"[CodeWindowColorPatcher] OpenCodeWindow Postfix: 全てのCodeWindowに色を再適用しました。");
    }

    // ----------------------------------------------------------------------

    // MenuクラスのPlayメソッドにパッチを適用（ボタンを非表示にする）
    [HarmonyPatch(typeof(Menu), "Play")]
    public static class MenuPlayPatch
    {
        public static void Postfix(Menu __instance)
        {
            // 1. 色の再適用ロジック
            Workspace workspace = Object.FindObjectOfType<Workspace>();
            if (workspace != null)
            {
                ApplyColorToAllOpenWindows(workspace, LastSelectedColor);
            }

            // 2. MenuTogglerがアタッチされたボタン（OpenCloseButton）を非表示にする
            // ★修正: 静的参照を使用
            MenuToggler toggler = ComponentSetupHelper.MenuTogglerInstance;

            if (toggler != null)
            {
                // OpenCloseButton (MenuTogglerのOpenCloseButtonフィールドから取得) の GameObjectを取得
                GameObject buttonGO = toggler.OpenCloseButton.gameObject;

                if (buttonGO != null && buttonGO.activeSelf)
                {
                    buttonGO.SetActive(false);
                    Logger.LogInfo("[MenuPlayPatch] OpenCloseButtonを非表示にしました。 (静的参照経由)");
                }
            }
            else
            {
                Logger.LogWarning("[MenuPlayPatch] ComponentSetupHelperからMenuTogglerのインスタンスを取得できませんでした。OpenCloseButtonを非表示にできませんでした。");
            }
        }
    }

    // ----------------------------------------------------------------------

    // MenuクラスのOpenメソッドにパッチを適用（ボタンを再表示する）
    [HarmonyPatch(typeof(Menu), "Open")]
    public static class MenuOpenPatch
    {
        public static void Postfix(Menu __instance)
        {
            Logger.LogDebug("[MenuOpenPatch] Menu.Open Postfixが実行されました。");

            // MenuTogglerがアタッチされたボタン（OpenCloseButton）を再表示する
            // ★修正: 静的参照を使用
            MenuToggler toggler = ComponentSetupHelper.MenuTogglerInstance;

            if (toggler != null)
            {
                // OpenCloseButton (MenuTogglerのOpenCloseButtonフィールドから取得) の GameObjectを取得
                GameObject buttonGO = toggler.OpenCloseButton.gameObject;

                // ボタンが非アクティブな場合のみアクティブ化
                if (buttonGO != null && !buttonGO.activeSelf)
                {
                    buttonGO.SetActive(true);
                    Logger.LogInfo("[MenuOpenPatch] OpenCloseButtonを再表示しました。 (静的参照経由)");
                }
            }
            else
            {
                Logger.LogWarning("[MenuOpenPatch] ComponentSetupHelperからMenuTogglerのインスタンスを取得できませんでした。OpenCloseButtonを再表示できませんでした。");
            }
        }
    }
}