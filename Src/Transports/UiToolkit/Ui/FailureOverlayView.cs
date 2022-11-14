using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
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

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(UnityUiToolkitTransport.VersionUrl);
            });
        }
        #endregion

        #region other

        #endregion

    }
}
