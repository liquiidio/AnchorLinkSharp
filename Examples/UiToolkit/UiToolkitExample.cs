using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using UnityEngine;

public class UiToolkitExample : MonoBehaviour
{
    // app identifier, should be set to the eosio contract account if applicable
    private const string Identifier = "example";

    // Assign UnityTransport through the Editor
    [SerializeField] internal UnityUiToolkitTransport Transport;

    // initialize the link
    private AnchorLink _link;

    // the session instance, either restored using link.restoreSession() or created with link.login()
    private LinkSession _session;

    public void Start()
    {
        _link = new AnchorLink(new LinkOptions()
        {
            Transport = this.Transport,
            ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
            Rpc = "https://eos.greymass.com",
            ZlibProvider = new NetZlibProvider(),
            Storage = new JsonLocalStorage()
            //chains: [{
            //    chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
            //    nodeUrl: 'https://eos.greymass.com',
            //}]
        });
    }
}
