using System.Collections;
using System.Collections.Generic;
using Assets.Packages.AnchorLinkTransportSharp;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class TimeoutOverlayView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _closeViewButton;
        private Button _signManuallyButton;

        private Label _versionLabel;



        /*
         * Fields, Properties
         */


        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _signManuallyButton = Root.Q<Button>("sign-manually-button");

            _versionLabel = Root.Q<Label>("version-label");

            _versionLabel.text = Version;

            BindButtons();
        }


        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _signManuallyButton.clickable.clicked += () =>
            {
                Hide();
                //QrCodeOverlayView.Show();
                //QrCodeOverlayView.SignManual();
            };

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });

        }
        #endregion
    }
}
