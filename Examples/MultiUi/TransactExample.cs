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

    private void Awake()
    {
        _link = new AnchorLink(new LinkOptions
        {
            Transport = Transport,
            ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            Rpc = "https://api.wax.liquidstudios.io",
            ZlibProvider = new NetZlibProvider(),
            Storage = new PlayerPrefsStorage()
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
    internal void Vote()
    {
        _link.Transact(new TransactArgs() { Action = Action }).ContinueWith(transactTask =>
        {
            Console.WriteLine($"Thank you {transactTask.Result.Signer.actor}");
        });
    }
}
