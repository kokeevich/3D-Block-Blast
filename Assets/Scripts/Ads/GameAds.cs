using System;
using System.Globalization;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using YandexMobileAds;
using YandexMobileAds.Base;
#endif

/// <summary>Native ads live outside scenes; scene navigation never waits for an ad to load.</summary>
public sealed class GameAds : MonoBehaviour
{
    private const string LossesKey="blockblast.ads.losses",ShownKey="blockblast.ads.lastShown";
    private static GameAds instance;
    private AdExposureRules exposure;
    private YandexAdsSettings settings;
    private bool menu,bannerWanted,showing;
    public int BannerHeightPixels { get; private set; }
    public bool IsShowing => showing;
    public static GameAds Get()
    {
        if (instance!=null) return instance;
        var root=new GameObject("GameAds"); DontDestroyOnLoad(root);
        return instance=root.AddComponent<GameAds>();
    }
    private void Awake()
    {
        instance=this;
        long.TryParse(PlayerPrefs.GetString(ShownKey,"0"),NumberStyles.Integer,CultureInfo.InvariantCulture,out long last);
        exposure=new AdExposureRules(PlayerPrefs.GetInt(LossesKey,0),last);
        settings=Resources.Load<YandexAdsSettings>("YandexAdsSettings");
#if UNITY_ANDROID && !UNITY_EDITOR
        if (settings==null) { Debug.LogWarning("YandexAdsSettings missing; ads disabled."); return; }
        try
        {
            YandexAds.SetUserConsent(false); // Never manufacture consent; location/targeting remain off.
            YandexAds.SetLocationTracking(false);
            loader=new InterstitialAdLoader();
            LoadInterstitial();
        }
        catch (Exception e) { Debug.LogWarning("Ads initialization unavailable: "+e.Message); }
#endif
    }
    public void RecordLoss()
    { exposure.RecordLoss(); PlayerPrefs.SetInt(LossesKey,exposure.Losses); PlayerPrefs.Save(); }
    public void SetMenuBanner(bool inMenu,bool unobstructed)
    {
        menu=inMenu; bannerWanted=inMenu && unobstructed;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!menu) { DestroyBanner(); BannerHeightPixels=0; }
        else UpdateBannerVisibility();
#endif
    }
    public void AfterLoss(Action continuation)
    {
        if (showing) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (settings!=null && interstitial!=null && Time.realtimeSinceStartup-loadedAt<3500 && exposure.CanShow(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) && Application.isFocused)
        {
            afterAd=continuation; showing=true; adPresented=false;
            listenerWasPaused=AudioListener.pause; AudioListener.pause=true;
            showDeadline=Time.realtimeSinceStartup+10;
            UpdateBannerVisibility();
            try { interstitial.Show(); } catch (Exception e) { Debug.LogWarning("Ad presentation failed: "+e.Message); FinishAd(); }
            return;
        }
#endif
        continuation();
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    private bool listenerWasPaused;
    private Action afterAd;
    private Banner banner;
    private InterstitialAdLoader loader;
    private Interstitial interstitial;
    private bool bannerReady,bannerVisible,loading,adPresented;
    private float nextBanner,nextInterstitial,loadDeadline,loadedAt,showDeadline;
    private int width,height;
    private Rect safeArea;
    private void Update()
    {
        if (settings==null || loader==null) return;
        float now=Time.realtimeSinceStartup;
        if (loading && now>=loadDeadline)
        { loader.CancelLoading(); loading=false; loadGeneration++; nextInterstitial=now+30; }
        if (interstitial!=null && !showing && now-loadedAt>3500) { interstitial.Destroy(); interstitial=null; }
        if (interstitial==null && !loading && !showing && now>=nextInterstitial) LoadInterstitial();
        if (showing && !adPresented && Application.isFocused && now>=showDeadline) FinishAd();
        if (!menu) return;
        if (width!=Screen.width || height!=Screen.height || safeArea!=Screen.safeArea)
        { DestroyBanner(); width=Screen.width; height=Screen.height; safeArea=Screen.safeArea; nextBanner=now; }
        if (banner==null && now>=nextBanner) LoadBanner();
        UpdateBannerVisibility();
    }
    private void LoadBanner()
    {
        nextBanner=Time.realtimeSinceStartup+30;
        try
        {
            float inset=Mathf.Max(Screen.safeArea.xMin,Screen.width-Screen.safeArea.xMax);
            int dp=Mathf.Max(1,ScreenUtils.ConvertPixelsToDp((int)(Screen.width-2*inset)));
            var size=BannerAdSize.Sticky(dp);
            BannerHeightPixels=size.Height;
            var requested=new Banner(size,AdPosition.BottomCenter); banner=requested;
            requested.Hide();
            requested.OnAdLoaded+=(sender,args) => { if (banner!=requested) return; bannerReady=true; nextBanner=0; UpdateBannerVisibility(); };
            requested.OnAdFailedToLoad+=(sender,args) =>
            { if (banner!=requested) return; Debug.LogWarning("Banner unavailable: "+args.Message); DestroyBanner(); nextBanner=Time.realtimeSinceStartup+30; };
            requested.LoadAd(new AdRequest(settings.BannerId));
        }
        catch (Exception e) { Debug.LogWarning("Banner unavailable: "+e.Message); DestroyBanner(); }
    }
    private void UpdateBannerVisibility()
    {
        if (banner==null) return;
        bool visible=menu && bannerWanted && bannerReady && !showing && Application.isFocused;
        if (visible==bannerVisible) return;
        bannerVisible=visible;
        if (visible) banner.Show(); else banner.Hide();
    }
    private void DestroyBanner()
    {
        var old=banner; banner=null; bannerReady=false; bannerVisible=false;
        if (old!=null) old.Destroy();
    }
    private int loadGeneration;
    private void LoadInterstitial()
    {
        if (loader==null || loading) return;
        int generation=++loadGeneration; loading=true;
        loadDeadline=Time.realtimeSinceStartup+30; nextInterstitial=loadDeadline;
        try
        {
            loader.LoadAd(new AdRequest(settings.InterstitialId),
                ad =>
                {
                    if (this==null || generation!=loadGeneration) { ad.Destroy(); return; }
                    loading=false; interstitial=ad; loadedAt=Time.realtimeSinceStartup;
                    ad.OnAdShown+=(sender,args) =>
                    {
                        if (!showing || interstitial!=ad) return;
                        adPresented=true; exposure.RecordShown(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        PlayerPrefs.SetString(ShownKey,exposure.LastShown.ToString(CultureInfo.InvariantCulture)); PlayerPrefs.Save();
                    };
                    ad.OnAdDismissed+=(sender,args) => { if (interstitial==ad) FinishAd(); };
                    ad.OnAdFailedToShow+=(sender,args) =>
                    { if (interstitial!=ad) return; Debug.LogWarning("Interstitial unavailable: "+args.Message); FinishAd(); };
                },
                error =>
                {
                    if (this==null || generation!=loadGeneration) return;
                    loading=false; nextInterstitial=Time.realtimeSinceStartup+30;
                    Debug.LogWarning("Interstitial load failed: "+error.Message);
                });
        }
        catch (Exception e) { loading=false; nextInterstitial=Time.realtimeSinceStartup+30; Debug.LogWarning("Interstitial unavailable: "+e.Message); }
    }
    private void FinishAd()
    {
        if (!showing) return;
        showing=false;
        var old=interstitial; interstitial=null; if (old!=null) old.Destroy();
        AudioListener.pause=listenerWasPaused;
        var continuation=afterAd; afterAd=null;
        nextInterstitial=Time.realtimeSinceStartup+1;
        continuation?.Invoke();
    }
#endif
    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        loadGeneration++; loader?.CancelLoading(); DestroyBanner();
        interstitial?.Destroy(); if (showing) AudioListener.pause=listenerWasPaused;
#endif
        if (instance==this) instance=null;
    }
}
