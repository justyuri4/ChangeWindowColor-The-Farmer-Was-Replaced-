using System;
using System.Reflection;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using BepInEx; // BepInExプラグインとして必須
using BepInEx.Logging; // ロギングのために必要
using System.IO;
using UnityEngine.UI;
using System.Linq;
using TMPro; // ★追加: TextMeshProのために必須

// BepInExプラグインとして機能させるための属性
[BepInPlugin(YourMenuMod.HarmonyId, "Your Menu Mod", "1.0.0")]
public class YourMenuMod : BaseUnityPlugin
{
    public const string HarmonyId = "com.yourname.menumod";
    private readonly Harmony harmony = new Harmony(HarmonyId);

    private const string AssetBundleNameInCode = "ChangeWindowColor.changecolorcanvas";
    private const string AssetName = "ChangeColorCanvas";

    // コードで直接指定するテキスト
    private const string DefaultModText = "Mod Configuration Menu";

    // 検索するフォントアセットの正確な名前
    private const string TargetFontName = "LiberationSans SDF";


    void Awake()
    {
        try
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"[YourMenuMod] Harmony patch '{HarmonyId}' applied successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[YourMenuMod] Failed to apply Harmony patch: {ex.Message}");
        }

        StartCoroutine(LoadEmbeddedAssetBundleAsync());
    }

    /// <summary>
    /// 埋め込みAssetBundleを非同期でロードし、含まれるPrefabをシーンにインスタンス化するコルーチン。
    /// </summary>
    private IEnumerator LoadEmbeddedAssetBundleAsync()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string fullResourceName = AssetBundleNameInCode;

        AssetBundle myLoadedAssetBundle = null;

        using (Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName))
        {
            if (resourceStream == null)
            {
                Logger.LogError($"[ModLoader] Failed to get stream. Please check the full name: {fullResourceName}");
                yield break;
            }

            AssetBundleCreateRequest createRequest = AssetBundle.LoadFromStreamAsync(resourceStream);
            yield return createRequest;
            myLoadedAssetBundle = createRequest.assetBundle;

            if (myLoadedAssetBundle == null)
            {
                Logger.LogError($"[ModLoader] Failed to load AssetBundle from stream: {fullResourceName}");
                yield break;
            }

            Logger.LogInfo($"[ModLoader] Embedded AssetBundle '{fullResourceName}' loaded successfully.");

            AssetBundleRequest assetRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>(AssetName);
            yield return assetRequest;

            // ------------------------------------
            // 既存のフォントアセットを名前で検索
            // ------------------------------------
            TMP_FontAsset existingFontAsset = FindExistingTMPFontAsset();
            if (existingFontAsset == null)
            {
                Logger.LogError($"[ModLoader] FAILED to find target font asset: '{TargetFontName}'. Text rendering will likely fail.");
            }
            else
            {
                Logger.LogInfo($"[ModLoader] Found target font asset: {existingFontAsset.name}. Using it for Mod UI.");
            }

            // 🚨 Canvas/RenderSystemの初期化完了を待つ 🚨
            yield return null;
            yield return new WaitForEndOfFrame();

            GameObject asset = assetRequest.asset as GameObject;

            if (asset != null)
            {
                GameObject instance = GameObject.Instantiate(asset);
                instance.name = $"MOD_{AssetName}_Instance";
                Logger.LogInfo("[ModLoader] Prefab instance created and placed in the scene.");

                // 5. ヘルパークラスを呼び出し、ロードしたアセットを設定する
                // フォントアセットと文字列を渡す
                ComponentSetupHelper.SetupComponents(instance, existingFontAsset, DefaultModText);

                // NREを回避するための初期化コルーチンを起動
                StartCoroutine(ForceTMPInitialization(instance));

                // 6. AssetBundleのアンロード
                if (myLoadedAssetBundle != null)
                {
                    myLoadedAssetBundle.Unload(false);
                    Logger.LogInfo("[ModLoader] AssetBundle unloaded (Assets kept in memory).");
                }
            }
            else
            {
                Logger.LogError($"[ModLoader] Failed to load Prefab '{AssetName}' from AssetBundle.");
            }
        }
    }

    /// <summary>
    /// TMP_Textコンポーネントの ForceMeshUpdate() を呼び出すことで、内部初期化を強制します。
    /// </summary>
    private IEnumerator ForceTMPInitialization(GameObject instance)
    {
        // 1. 1フレーム待機 (ComponentのStart/Awake完了を待つ)
        yield return null;

        // ★修正: さらにフレームの終わりまで待機することで、TMPの内部処理が完了する時間を確保
        yield return new WaitForEndOfFrame();

        TextMeshProUGUI tmpComponent = instance.GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpComponent != null)
        {
            try
            {
                // ForceMeshUpdate() を呼び出す
                tmpComponent.ForceMeshUpdate();
                Logger.LogInfo("[ForceInit] TMP ForceMeshUpdate() 実行完了 (再試行、遅延強化)。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[ForceInit] TMP ForceMeshUpdate() 実行中にエラーが発生: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// メモリ内の全てのアセットを検索し、名前に基づいて特定のTMPフォントを返します。
    /// </summary>
    private TMP_FontAsset FindExistingTMPFontAsset()
    {
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

        Logger.LogInfo($"[ModLoader] Found {allFonts.Length} TMP_FontAsset(s) in memory.");

        foreach (var fontAsset in allFonts)
        {
            if (fontAsset != null)
            {
                if (fontAsset.name.ToLowerInvariant().Contains(TargetFontName.ToLowerInvariant()))
                {
                    return fontAsset;
                }
            }
        }

        return null;
    }
}