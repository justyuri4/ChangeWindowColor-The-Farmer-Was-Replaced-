using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

/// <summary>
/// ボタンクリックで特定のパネルの表示/非表示を切り替えるコンポーネント。
/// </summary>
public class MenuToggler : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MenuToggler");

    // ComponentSetupHelperから参照が設定されるフィールド
    public Button OpenCloseButton;
    public GameObject BackPanel;

    /// <summary>
    /// 初期化（主にボタンにイベントを登録）を行います。
    /// ComponentSetupHelperから呼び出されます。
    /// </summary>
    public void Initialize()
    {
        if (OpenCloseButton != null)
        {
            // 既存のリスナーをクリアしてから登録することで二重登録を防ぐ
            OpenCloseButton.onClick.RemoveAllListeners();
            OpenCloseButton.onClick.AddListener(TogglePanel);
            Logger.LogInfo("[MenuToggler] Button event registered.");
        }
        else
        {
            Logger.LogError("[MenuToggler] OpenCloseButtonが設定されていません。");
        }

        // 初期状態でパネルを非表示にする
        if (BackPanel != null)
        {
            // 既に非表示になっていなければ非表示にする
            if (BackPanel.activeSelf)
            {
                BackPanel.SetActive(false);
            }
        }
        else
        {
            Logger.LogError("[MenuToggler] BackPanelが設定されていません。");
        }
    }

    /// <summary>
    /// パネルの表示状態を切り替えます。
    /// </summary>
    public void TogglePanel()
    {
        if (BackPanel != null)
        {
            bool currentState = BackPanel.activeSelf;
            BackPanel.SetActive(!currentState);
            Logger.LogInfo($"[MenuToggler] BackPanel visibility toggled to: {!currentState}");
        }
    }
}
