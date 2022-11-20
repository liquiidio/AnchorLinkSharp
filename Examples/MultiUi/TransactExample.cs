using System;
using System.Collections;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using EosSharp.Core.Api.v1;
using UnityEngine;

public class TransactExample : MonoBehaviour
{
    // Assign UnityTransport through the Editor
    [SerializeField] internal UnityTransport Transport;

    // initialize the link
    private AnchorLink _link;

    public void Start()
    {
        _link = new AnchorLink(new LinkOptions()
        {
            Transport = this.Transport,
            ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
            Rpc = "https://eos.greymass.com",
            //ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            //Rpc = "https://wax.greymass.com",
            ZlibProvider = new NetZlibProvider(),
            Storage = new JsonLocalStorage()
            //chains: [{
            //    chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
            //    nodeUrl: 'https://eos.greymass.com',
            //}]
        });
    }

    // the EOSIO action we want to sign and broadcast
    private EosSharp.Core.Api.v1.Action Action = new()
    {
        account = "eosio",
        name = "voteproducer",
        authorization = new List<PermissionLevel>()
        {
            new PermissionLevel()
            {
                actor = "............1", // ............1 will be resolved to the signing accounts permission
                permission = "............2" // ............2 will be resolved to the signing accounts authority
            }
        },
        data = new Dictionary<string, object>()
        {
            { "voter", "............1" },
            { "proxy", "coredevproxy" },
            { "producers", Array.Empty<object>() },
        }
    };

    // ask the user to sign the transaction and then broadcast to chain
    public void Vote()
    {
        _link.Transact(new TransactArgs() { Action = Action }).ContinueWith(transactTask =>
        {
            Console.WriteLine($"Thank you {transactTask.Result.Signer.actor}");
        });
    }
}
