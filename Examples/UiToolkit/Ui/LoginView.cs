using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Examples.UiToolkit.Ui
{
    public class LoginView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _loginButton;


        /*
         * Fields, Properties
         */
        [SerializeField]internal UiToolkitExample UiToolkitExample;
        [SerializeField]internal MainView MainView;


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");

            BindButtons();
            Show();
        }

        #region Button Binding
        private void BindButtons()
        { 
            _loginButton.clickable.clicked +=  async () =>
            {
                try
                {
                    Hide();
                    await UiToolkitExample.StartSession(); 
                    MainView.Show();
                    MainView.Rebind();
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };
        }
        #endregion

    }
}
