


<div align="center">
 <img src="https://github.com/liquiidio/Misc/blob/main/LiquiidDropLogo.gif?raw=true" align="center"
     alt="Liquiid logo">
</div>

# Anchor Link Sharp
A native integration compatible with Unity3D and C# allowing users and developers to connect and communicate with Anchor Wallet and ESR-based applications. The Anchor & ESR Integration consists of multiple libraries for the ESR-Protocol, the Anchor-integration, Transports etc. which will be included via Submodules while being packaged and published as a single Package.

Link to >>AnchorLinkTransportSharp<< and >>ESR Sharp<<

# Installation (!TODO!)
**_Requires Unity 2019.1+ with .NET 4.x+ Runtime_**

This package can be included into your project by either:

 1. Installing the package via Unity's Package Manager (UPM) in the editor (recommended).
 2. Importing the .unitypackage which you can download here.
 3. Manually add the files in this repo.
 4. Installing it via NuGet. (for Standard .NET users)

### Dependencies
TODO, add WebSocket-Package (if not already installed)
- Via Upm
- clone Repo

### 1. Installing via Unity Package Manager (UPM).
In your Unity project:
 1. Open the Package Manager Window/Tab
 2. Click Add Package From Git URL
 3. Enter URL:  `https://github.com/endel/NativeWebSocket.git#upm`
---
### 2. Importing the Unity Package.
Download the UnityPackage here. Then in your Unity project:

 1. Open up the import a custom package window
 2. Navigate to where you downloaded the file and open it.
 3. Check all the relevant files needed (if this is a first time import, just select ALL) and click on import.
---
### 3. Install manually.
Download this project there here. Then in your Unity project:

 1. Copy the sources from `NativeWebSocket/Assets/WebSocket` into your `Assets` directory. // Corvin: We should hava a dependencies-section showing how to install dependencies in general, non of our packages includes the WebSocket-Package

---
### 4. Install via NuGet (for Standard .NET users only - No Unity3D)
Black magic

---
## Usage (!TODO!)

.NET and Unity3D-compatible (Desktop, Mobile, WebGL) ApiClient for the different APIs. 
Endpoints have its own set of parameters that you may build up and pass in to the relevant function.

### Examples

#### Quick Start

 1. Add one of the Transport-Prefabs (UiToolkitTransport or CanvasTransport) to your Scene.
 2. Instantiate a new AnchorLink-object in one of your scripts, assign the Transport and configure your AnchorLink for the usage with WAX and the endpoints of your choice.

 
```csharp
   [SerializeField]
   internal UnityTransport Transport;
   internal AnchorLink myLink;

   public void Start(){
	   myLink = new AnchorLink(new LinkOptions()
	   {
		   Transport = this.Transport,
		   ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
		   Rpc = "https://api.wax.liquidstudios.io",
	  });
  }
```

#### Login

 1. Add a method calling the Login(identifier)-Method of the previously instantiated AnchorLink.
 2. When this Method is called from your UI, the Transport will show and Ask the User to Open Anchor to Login.
 3. After succesfull Login, data about the Account selected and additional session-data is returned your Application, available for use within the session-variable returned.
 4. Storing the Session will allow you to to promt the User with a Transact-Request without the user having to Login again.

```csharp

internal LinkSession mySession;
internal async Task LoginAndStoreSession()
{
	try
	{
		var loginResult = await myLink.Login(Identifier);
		mySession = loginResult.Session;
		Debug.Log($"{mySession.Auth.actor} logged-in");
	}
	catch (Exception ex)
	{
		Debug.LogError(ex.Message);
	}
}
```

#### Transact (without Session)

 1. To transact without a Session, create a Method and pass a EosSharp Action-Object to it.
 2. Call await AnchorLink.Transact(new TransactArgs(){Action = action); to transact.
 3. When this Method is called from your UI or other Code, the Transport will show and Ask the User to Open Anchor to Login and afterwards to Sign a Transaction containing the Action passed.

```csharp
internal async Task TransactWithoutSession(EosSharp.Core.Api.v1.Action action)
{
	var transactResult = await myLink.Transact(new TransactArgs() { Action = action });
	Debug.Log($"Transaction broadcast! {transactResult.Processed}");
}
```

#### Transact (with Session)
 1. To transact without a Session, create a Method and pass a EosSharp Action-Object to it.
 2. Call await LinkSession.Transact(new TransactArgs(){Action = action); to transact.
 3. When this Method is called from your UI or other Code, the Transport will show and Ask the User to Sign a Transaction containing the Action passed (without requiring the User to Login again).
```csharp
internal async Task TransactWithoutSession(EosSharp.Core.Api.v1.Action action)
{
	var transactResult = await mySession.Transact(new TransactArgs() { Action = action });
	Debug.Log($"Transaction broadcast! {transactResult.Processed}");
}
```



#### Token Transfer 

 1. Following Example shows how a Token Transfer Action could be created and passed to the Transact-Method using a Session.

```csharp
	    // transfer tokens using a session
        private async Task Transfer(string frmAcc, string toAcc, string qnty, string memo)
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "eosio.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() { _session.Auth },
                data = new Dictionary<string, object>
                {
                    {"from", frmAcc},
                    {"to", toAcc},
                    {"quantity", qnty},
                    {"memo", memo}
                }
            };

            try
            {
                var transactResult = await mySession.Transact(new TransactArgs() { Action = action });
                Debug.Log($"Transaction broadcast! {transactResult.Processed}");

                waitCoroutine = StartCoroutine(SwitchPanels(Transport.currentPanel, CustomActionsPanel, 1.5f));

            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }
```

Link? (!TODO!)

- --
- NFT Transfer - link
- Create Permission - link
- Get Balanaces - link



[build-badge]: https://github.com/mkosir/react-parallax-tilt/actions/workflows/build.yml/badge.svg
[build-url]: https://github.com/mkosir/react-parallax-tilt/actions/workflows/build.yml
[test-badge]: https://github.com/mkosir/react-parallax-tilt/actions/workflows/test.yml/badge.svg
[test-url]: https://github.com/mkosir/react-parallax-tilt/actions/workflows/test.yml
