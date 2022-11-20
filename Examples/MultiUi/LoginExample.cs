using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Exceptions;
using Newtonsoft.Json;
using UnityEngine;
using Action = EosSharp.Core.Api.v1.Action;

public class LoginExample : MonoBehaviour
{
    // app identifier, should be set to the eosio contract account if applicable
    private const string Identifier = "example";

    // initialize the link
    private AnchorLink _link;

    // the session instance, either restored using link.restoreSession() or created with link.login()
    private LinkSession _session;

    // Assign UnityTransport through the Editor
    [SerializeField] internal UnityTransport Transport;

    public void Awake()
    {
        _link = new AnchorLink(new LinkOptions
        {
            Transport = Transport,
            //ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", 
            //Rpc = "https://eos.greymass.com",
            ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            Rpc = "https://wax.greymass.com",
            ZlibProvider = new NetZlibProvider(),
            Storage = new JsonLocalStorage()
            //chains: [{
            //    chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
            //    nodeUrl: 'https://eos.greymass.com',
            //}]
        });
    }

    // tries to restore session, called when document is loaded
    public async Task RestoreSession()
    {
        var restoreSessionResult = await _link.RestoreSession(Identifier);
        _session = restoreSessionResult;
        if (_session != null)
            DidLogin();
    }

    // login and store session if sucessful
    public async Task Login()
    {
        var loginResult = await _link.Login(Identifier);
        _session = loginResult.Session;
        DidLogin();
    }

    // logout and remove session from storage
    public async Task Logout()
    {
        await _session.Remove();
    }

    // called when session was restored or created
    public void DidLogin()
    {
        Debug.Log($"{_session.Auth.actor} logged-in");
    }

    // transfer tokens using a session
    public async Task Transfer()
    {
        try
        {
            var action = new Action
            {
                account = "eosio.token",
                name = "transfer",
                authorization = new List<PermissionLevel> { _session.Auth },
                data = new Dictionary<string, object>
                {
                    { "from", _session.Auth.actor },
                    { "to", "test2.liq" },
                    { "quantity", "0.00010000 WAX" },
                    { "memo", "Test transfer from test1.liq to test2.liq" }
                }
            };

            var transactResult = await _session.Transact(new TransactArgs { Action = action });
            Debug.Log($"Transaction broadcast! {transactResult.Processed}");
        }
        catch (ApiErrorException ae)
        {
            Debug.Log(ae.message);
            Debug.Log(ae.error.name + ae.error.what);
            foreach (var apiErrorDetail in ae.error.details) Debug.Log(apiErrorDetail.message);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}