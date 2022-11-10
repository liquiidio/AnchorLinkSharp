using System;
using System.Collections;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.UI.ScriptsAndUxml
{
    public class FailureOverlayView : ScreenBase
    {
        /*
         * Connected Views
         */


        /*
         * Child-Controls
         */

        private Button _closeViewButton;

        private Label _versionLabel;

        /*
         * Fields, Properties
         */

        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _versionLabel = Root.Q<Label>("version-label");

            _versionLabel.text = Version;

            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });
        }
        #endregion

        #region other

        #endregion

    }
}
