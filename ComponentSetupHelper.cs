using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;
using TMPro; // TextMeshProを使用するために必須
using System; // Exceptionのために使用

/// <summary>
/// ロードされたPrefabインスタンスに対して、必要なコンポーネントを動的にアタッチし、
/// 必要なUI要素の参照を設定するためのヘルパークラス。
/// </summary>
public static class ComponentSetupHelper
{
    private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ComponentSetupHelper");

    public static MenuToggler MenuTogglerInstance { get; private set; }

    /// <summary>
    /// Prefabインスタンス内のUI要素を検索し、MenuToggler、ColorController、TMPテキストを設定します。
    /// </summary>
    public static void SetupComponents(GameObject instance, TMP_FontAsset loadedFontAsset, string textString)
    {
        if (instance == null)
        {
            Logger.LogError("[SetupHelper] インスタンスがnullです。設定をスキップします。");
            return;
        }

        SetupMenuToggler(instance);
        SetupColorController(instance);

        // TMPコンポーネントの設定
        SetupTextMeshPro(instance, loadedFontAsset, textString);
    }

    /// <summary>
    /// TextMeshProコンポーネントにフォントアセットとテキストを設定します。
    /// </summary>
    public static void SetupTextMeshPro(GameObject instance, TMP_FontAsset loadedFontAsset, string textString)
    {
        // ルートGameObjectまたは子要素にあるTMPコンポーネントを検索
        TextMeshProUGUI tmpComponent = instance.GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpComponent != null)
        {
            // 1. 【最重要】フォントアセットとマテリアルの設定
            if (loadedFontAsset != null)
            {
                tmpComponent.font = loadedFontAsset;

                // --- ★マテリアルフォールバックロジック（今回の最重要修正）---
                Material sharedMaterial = loadedFontAsset.material;

                if (sharedMaterial == null)
                {
                    try
                    {
                        // 標準の TMP - SDF Shader を検索
                        Shader tmpShader = Shader.Find("TextMeshPro/Distance Field");

                        if (tmpShader != null)
                        {
                            // シェーダーを使って新しいマテリアルを生成
                            sharedMaterial = new Material(tmpShader);
                            // フォントアセットが持つテクスチャ（アトラス）を新しいマテリアルに割り当てる
                            sharedMaterial.SetTexture(ShaderUtilities.ID_MainTex, loadedFontAsset.atlas);
                            Logger.LogWarning("[SetupHelper] マテリアルがnullのため、フォールバックマテリアルを新規作成し割り当てました。");
                        }
                        else
                        {
                            Logger.LogError("[SetupHelper] 'TextMeshPro/Distance Field'シェーダーが見つかりません。マテリアルを作成できませんでした。");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[SetupHelper] フォールバックマテリアル作成中に予期せぬエラー: {ex.Message}");
                    }
                }

                // 3. コンポーネントにマテリアルを割り当てる
                if (sharedMaterial != null)
                {
                    tmpComponent.fontSharedMaterial = sharedMaterial;
                    Logger.LogInfo("[SetupHelper] TMP Font Shared Materialを設定しました。");
                }
                else
                {
                    Logger.LogError($"[SetupHelper] Font Asset '{loadedFontAsset.name}' に有効なMaterialがありません。描画失敗の可能性があります。");
                }
                // --- ★マテリアルフォールバックロジック終わり ---

                Logger.LogInfo($"[SetupHelper] TMP Font Asset '{loadedFontAsset.name}' を設定しました。");
            }
            else
            {
                Logger.LogWarning("[SetupHelper] TMP Font Asset がnullです。TMPテキストが表示されない可能性があります。");
            }

            // 2. テキストの色を強制的に設定し、表示されない問題を解決する
            tmpComponent.color = Color.red;
            Logger.LogInfo("[SetupHelper] TMPテキストカラーを強制的に赤に設定しました。");

            // 3. テキストコンテンツの設定
            if (!string.IsNullOrEmpty(textString))
            {
                tmpComponent.text = textString;
                Logger.LogInfo("[SetupHelper] コードから直接文字列を設定しました。");
            }
            else
            {
                tmpComponent.text = "Error: Text not provided";
            }
        }
        else
        {
            Logger.LogInfo("[SetupHelper] TextMeshProUGUI コンポーネントは見つかりませんでした。スキップします。");
        }
    }


    /// <summary>
    /// MenuTogglerコンポーネントをアタッチし、必要な参照を設定します。(省略)
    /// </summary>
    public static void SetupMenuToggler(GameObject instance)
    {
        GameObject openCloseButtonGO = FindChildByNameRecursive(instance.transform, "OpenCloseButton");
        GameObject backPanelGO = FindChildByNameRecursive(instance.transform, "BackPanel");

        if (openCloseButtonGO != null && backPanelGO != null)
        {
            if (backPanelGO.GetComponent<Button>() == null)
            {
                Button backPanelButton = backPanelGO.AddComponent<Button>();
                backPanelButton.transition = Selectable.Transition.None;
                backPanelButton.interactable = true;
                Logger.LogInfo("[SetupHelper] BackPanelにイベントブロッキング用のButtonコンポーネントを追加しました。");
            }

            MenuToggler toggler = openCloseButtonGO.AddComponent<MenuToggler>();
            toggler.OpenCloseButton = openCloseButtonGO.GetComponent<Button>();
            toggler.BackPanel = backPanelGO;

            toggler.Initialize();
            MenuTogglerInstance = toggler;
            Logger.LogInfo("[SetupHelper] MenuTogglerの設定が完了しました。");
        }
        else
        {
            Logger.LogError("[SetupHelper] 'OpenCloseButton'または'BackPanel'が見つからなかったため、MenuTogglerの設定をスキップします。");
        }
    }

    /// <summary>
    /// ColorControllerコンポーネントをアタッチし、必要な参照とイベントを設定します。(省略)
    /// </summary>
    public static void SetupColorController(GameObject instance)
    {
        GameObject backPanelGO = FindChildByNameRecursive(instance.transform, "BackPanel");
        GameObject redSliderGO = FindChildByNameRecursive(instance.transform, "RedSlider");
        GameObject greenSliderGO = FindChildByNameRecursive(instance.transform, "GreenSlider");
        GameObject blueSliderGO = FindChildByNameRecursive(instance.transform, "BlueSlider");
        GameObject openCloseButtonGO = FindChildByNameRecursive(instance.transform, "OpenCloseButton");

        Workspace workspace = UnityEngine.Object.FindObjectOfType<Workspace>();

        if (backPanelGO == null)
        {
            Logger.LogError("[SetupHelper] 'BackPanel'が見つからなかったため、ColorControllerの設定をスキップします。");
            return;
        }

        Image targetImage = backPanelGO.GetComponent<Image>();
        Image buttonImage = openCloseButtonGO?.GetComponent<Image>();

        if (redSliderGO != null && greenSliderGO != null && blueSliderGO != null && targetImage != null && buttonImage != null)
        {
            ButtonColorUpdater buttonUpdater = openCloseButtonGO.AddComponent<ButtonColorUpdater>();
            buttonUpdater.TargetImage = buttonImage;

            SliderColorUpdater sliderUpdater = backPanelGO.AddComponent<SliderColorUpdater>();
            sliderUpdater.RedSlider = redSliderGO.GetComponent<Slider>();
            sliderUpdater.GreenSlider = greenSliderGO.GetComponent<Slider>();
            sliderUpdater.BlueSlider = blueSliderGO.GetComponent<Slider>();

            ColorController controller = backPanelGO.AddComponent<ColorController>();
            controller.TargetImage = targetImage;
            controller.ButtonUpdater = buttonUpdater;
            controller.SliderUpdater = sliderUpdater;
            controller.WorkspaceInstance = workspace;

            sliderUpdater.Controller = controller;

            buttonUpdater.Initialize();
            sliderUpdater.Initialize();
            controller.Initialize();

            Logger.LogInfo("[SetupHelper] ColorControllerとButtonColorUpdater、SliderColorUpdaterの設定が完了しました。");
        }
        else
        {
            Logger.LogError("[SetupHelper] 必要なUI要素の一部が見つかりませんでした。ColorControllerの設定をスキップします。");
        }
    }

    /// <summary>
    /// 再帰的に子のGameObjectを検索します。(省略)
    /// </summary>
    private static GameObject FindChildByNameRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name)
        {
            return parent.gameObject;
        }
        foreach (Transform child in parent)
        {
            GameObject result = FindChildByNameRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}