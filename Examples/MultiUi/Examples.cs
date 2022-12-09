using System;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.Canvas;
using UnityEngine;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Newtonsoft.Json;


public class Examples : MonoBehaviour
{
    [SerializeField] internal UnityUiToolkitTransport UnityUiToolkitTransport;

    [SerializeField] internal UnityCanvasTransport UnityCanvasTransport;

    [SerializeField] internal LoginExample LoginExample;

    [SerializeField] internal TransactExample TransactExample;

    private UnityTransport _transport;

    // toggle between UIToolkit and Canvas displays
    [SerializeField] internal bool UseCanvas;

    private void Awake()
    {
        // Assign appropriate transport according to the display used
        if (UseCanvas)
            _transport = UnityCanvasTransport;
        else
            _transport = UnityUiToolkitTransport;

        LoginExample.Transport = _transport;
        TransactExample.Transport = _transport;
    }

    // When the level loads, login and attempt a predefined transfer of tokens
    private async void Start()
    {
        try
        {
            await LoginExample.Login();
            try
            {
                // throws if the account doesn't have enough CPU
                await LoginExample.Transfer();
            }
            catch (Exception e)
            {
                Debug.Log(JsonConvert.SerializeObject(e));
            }
            // logout removes the session so it's not restorable
            //                await loginExample.logout();
            await LoginExample.RestoreSession();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}
