using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;
using HyperionApiClient.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ExampleUnityCanvasTransport : UnityCanvasTransport
{
    public ExampleUnityCanvasTransport(TransportOptions options) : base(options)
    {
    }

    private AnchorLink _anchorLink;
    private LinkSession _linkSession;

    private const string Identifier = "example";

    #region Login-Screen
    [Header("Login Screen Panel Components")]
    public string ESRLink = ""; // Link that will be converted to a QR code and can be copy from
    public string VersionURL = "https://www.github.com/greymass/anchor-link"; // Link that will show the url for the version
    public string DownloadURL = "https://www.greymass.com/en/anchor/download"; // Link that will go to the download page for anchor

    public GameObject LoginPanel;   // The holding panel for the login details
    public GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

    //Buttons
    public Button CloseLoginScreenButton;
    public Button StaticQRCodeHolderTargetButton;
    public Button ResizableQRCodeHolderTargetButton;
    public Button HyperlinkCopyButton;
    public Button LaunchAnchorButton;

    #endregion

    #region Sign and countdown timer
    [Header("Countdown timer")]
    public GameObject SignPanel;
    public TextMeshProUGUI CountdownTextGUI;
    public string CountdownText
    {
        get
        {
            return CountdownTextGUI.text;
        }

        set
        {
            CountdownTextGUI.text = value;
        }
    }

    #endregion

    #region Other panels
    [Header("Other panels")]
    public GameObject CustomTransferPanel;
    public GameObject LoadingPanel;
    public GameObject SuccessPanel;
    public GameObject FailurePanel;
    #endregion

    public async void StartSession()
    {
        _anchorLink = new AnchorLink(new LinkOptions()
        {
            Transport = this,
            ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
            Rpc = "https://eos.greymass.com",
            ZlibProvider = new NetZlibProvider(),
            Storage = new JsonLocalStorage()
        });

        try
        {
            await CreateSession();
        }

        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public async Task CreateSession()
    {
        try
        {
            var loginResult = await _anchorLink.Login(Identifier);

            _linkSession = loginResult.Session;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    // tries to restore session, called when document is loaded
    public async Task RestoreSession()
    {
        var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
        _linkSession = restoreSessionResult;

        if (_linkSession != null)
            Debug.Log($"{_linkSession.Auth.actor} logged-in");
    }


    public async void TryTransferTokens(GameObject TransferDetailsPanel)
    {
        string _frmAcc = "";
        string _toAcc = "";
        string _qnty = "";
        string _memo = "";

        foreach (var _entry in TransferDetailsPanel.GetComponentsInChildren<TMP_InputField>())
        {
            if (_entry.name == "FromAccountInputField(TMP)")
                _frmAcc = _entry.text;

            else if (_entry.name == "ToAccountInputField(TMP)")
                _toAcc = _entry.text;

            else if (_entry.name == "QuantityAccountInputField(TMP)")
                _frmAcc = $"{_entry.text} EOS";

            else if (_entry.name == "MemoAccountInputField(TMP)")
                _frmAcc = _entry.text;
        }

        //await Transfer
        //(
        //    _frmAcc,
        //    _toAcc,
        //    _qnty,
        //    _memo
        //);

        await Transfer();
    }
    // transfer tokens using a session  // For testing
    public async Task Transfer2(string fromAccount = "", string toAccount = "teamgreymass", string quanitiy = "0.0001 EOS", string memo = "Anchor is the best! Thank you <3")
    {
        var action = new EosSharp.Core.Api.v1.Action()
        {
            account = "eosio.token",
            name = "transfer",
            authorization = new List<PermissionLevel>() { _linkSession.Auth },
            data = new Dictionary<string, object>()
                {
                    { "from", fromAccount == "" ?  _linkSession.Auth.actor: fromAccount},
                    { "to", toAccount },
                    { "quantity", quanitiy },
                    { "memo", memo }
                }
        };

        var transactResult = await _linkSession.Transact(new TransactArgs() { Action = action });
        Console.WriteLine($"Transaction broadcast! {transactResult.Processed}");
    }

    // transfer tokens using a session
    public async Task Transfer()
    {
        var action = new EosSharp.Core.Api.v1.Action()
        {
            account = "eosio.token",
            name = "transfer",
            authorization = new List<PermissionLevel>() { _linkSession.Auth },
            data = new Dictionary<string, object>()
                {
                    { "from", _linkSession.Auth.actor },
                    { "to", "teamgreymass" },
                    { "quantity", "0.0001 EOS" },
                    { "memo", "Anchor is the best! Thank you <3" }
                }
        };

        var transactResult = await _linkSession.Transact(new TransactArgs() { Action = action });
        Console.WriteLine($"Transaction broadcast! {transactResult.Processed}");
    }

    // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
    public override void ShowLoading()
    {
        LoadingPanel.SetActive(true);
    }

    // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
    public override void OnSuccess(SigningRequest request, TransactResult result)
    {
        SuccessPanel.SetActive(true);
    }

    // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
    public override void OnFailure(SigningRequest request, Exception exception)
    {
        Debug.LogWarning($"FailurePanel's name is {FailurePanel.name}");

        FailurePanel.SetActive(true);
    }

    // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
    public override void DisplayRequest(SigningRequest request)
    {
        LoginPanel.SetActive(true);

        ESRLink = request.Encode(false, false);  // This returns ESR link to be converted

        var _tex = StringToQRCodeTexture2D(ESRLink);

        StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
            ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    }

    // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
    public override void ShowDialog(string title = null, string subtitle = null, string type = null, System.Action action = null,
        object content = null)
    {
        throw new NotImplementedException();
    }


    #region Canvas function-calls
    public void OnVersionButtonPressed()
    {
        Application.OpenURL(VersionURL);
    }

    public void OnDownloadAnchorButtonPressed()
    {
        Application.OpenURL(DownloadURL);
    }


    public void OnLoginPanelCloseButtonPressed()
    {
        Debug.LogWarning("Close the login panel");
    }

    public void OnStaticQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
    {
        resizableQRCodePanel.gameObject.SetActive(true);

        resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomIn");
    }

    public void OnResizableQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
    {
        resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomOut");

        StartCoroutine(ResizableQRCodePanelZoomOut(resizableQRCodePanel));
    }

    private IEnumerator ResizableQRCodePanelZoomOut(RectTransform resizableQRCodePanel)
    {
        yield return new WaitForSeconds(0.1f);

        yield return new WaitUntil(() => resizableQRCodePanel.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name == "NormalState");

        resizableQRCodePanel.gameObject.SetActive(false);
        resizableQRCodePanel.gameObject.SetActive(false);
    }

    public void OnQRHyperlinkButtonPressed()
    {
        HyperlinkCopyButton.gameObject.SetActive(false);

        HyperlinkCopiedNotificationPanel.SetActive(true);

        CopyToClipboard(ESRLink);

        StartCoroutine(ToggleHyperlinkCopyButton_Delayed());
    }

    private IEnumerator ToggleHyperlinkCopyButton_Delayed()
    {
        yield return new WaitForSeconds(3.5f);
        HyperlinkCopyButton.gameObject.SetActive(true);

        HyperlinkCopiedNotificationPanel.SetActive(false);
    }

    public void OnLaunchAnchorButtonPressed()
    {

        Debug.LogWarning("Launch Anchor Button pressed!");
        Process.Start(Ps);  // Temp call. Correct call is in Evans branch
    }

    public void OnCloseLoadingScreenButtonPressed()
    {
        Debug.LogWarning("Close loading screen button has been pressed!");
    }

    public void OnCloseTimeoutScreenButtonPressed()
    {
        Debug.LogWarning("Close timeout screen button has been pressed!");
    }

    public void StartTimer()
    {

        if (counterCoroutine != null)
            StopCoroutine(counterCoroutine);

        SignPanel.SetActive(true);
        CountdownTextGUI.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
        counterCoroutine = StartCoroutine(CountdownTimer(this, 2));
    }

    public void OnSignManuallyButtonPressed()
    {
        Debug.LogWarning("Sign manually button has been pressed!");
    }

    public void OnCloseSignScreenButtonPressed()
    {
        Debug.LogWarning("Close sign screen button has been pressed!");
    }

    public void OnCloseSuccessScreenButtonPressed()
    {
        Debug.LogWarning("Close success screen button has been pressed!");

        SuccessPanel.SetActive(false);

        LoginPanel.SetActive(false);

        CustomTransferPanel.SetActive(true);
    }

    public void OnCloseFailureScreenButtonPressed()
    {
        Debug.LogWarning("Close failure screen button has been pressed!");

        FailurePanel.SetActive(false);
    }
    #endregion
}

