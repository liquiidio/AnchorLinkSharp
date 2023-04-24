using System;
using AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorLinkTransportSharp.Examples.UiToolkit.Ui
{
    public class AnchorLoginPanel : ScreenBase
    {
        /**
         * Child-Controls
         */

        private Button _loginButton;


        /**
         * Fields, Properties
         */
        [SerializeField]internal UiToolkitExample UiToolkitExample;
        [SerializeField]internal AnchorExamplePanel AnchorExamplePanel;


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");

            BindButtons();
            Show();
        }

        #region Button Binding

        /// <summary>
        /// Assign UI toolkit interaction events
        /// </summary>
        private void BindButtons()
        { 
            _loginButton.clickable.clicked +=  async () =>
            {
                try
                {
                    Hide();
                    await UiToolkitExample.StartSession(); 
                    AnchorExamplePanel.Show();
                    AnchorExamplePanel.Rebind();
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
