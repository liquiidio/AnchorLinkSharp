using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Exceptions;
using UnityEngine;

public class LoginExample : MonoBehaviour
{
    // app identifier, should be set to the eosio contract account if applicable
    private const string Identifier = "example";

    // Assign UnityTransport through the Editor
    [SerializeField] internal UnityTransport Transport;

    // initialize the link
    private AnchorLink _link;

    // the session instance, either restored using link.restoreSession() or created with link.login()
    private LinkSession _session;

    public void Awake()
    {
        _link = new AnchorLink(new LinkOptions()
        {
            Transport = this.Transport,
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
        Console.WriteLine($"{_session.Auth.actor} logged-in");
    }

    // transfer tokens using a session
    public async Task Transfer()
    {
        try
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "eosio.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() {_session.Auth},
                data = new Dictionary<string, object>()
                {
                    {"from", _session.Auth.actor},
                    {"to", "test2.liq"},
                    {"quantity", "0.0001 WAX"},
                    {"memo", "Test transfer from test1.liq to test2.liq"}
                }
            };

            var transactResult = await _session.Transact(new TransactArgs() {Action = action});
            Console.WriteLine($"Transaction broadcast! {transactResult.Processed}");
        }
        catch (ApiErrorException ae)
        {
            Console.WriteLine(ae.message);
            Console.WriteLine(ae.error.name + ae.error.what);
            foreach (var apiErrorDetail in ae.error.details)
            {
                Console.WriteLine(apiErrorDetail.message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
}
