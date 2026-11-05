using UnityEngine;

[CreateAssetMenu(menuName="Block Blast/Yandex Ads Settings")]
public sealed class YandexAdsSettings : ScriptableObject
{
    [SerializeField] private bool testMode=true;
    [SerializeField] private string applicationId="20344040";
    [SerializeField] private string bannerId="R-M-20344040-1";
    [SerializeField] private string interstitialId="R-M-20344040-2";
    public bool UsesTestAds => testMode || Debug.isDebugBuild || Application.isEditor;
    public string ApplicationId => applicationId;
    public string BannerId => UsesTestAds ? "demo-banner-yandex" : bannerId;
    public string InterstitialId => UsesTestAds ? "demo-interstitial-yandex" : interstitialId;
}
