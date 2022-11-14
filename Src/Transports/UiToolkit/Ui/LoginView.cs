using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class LoginView : ScreenBase
    {
        /*
         * Connected Views
         */

        public QrCodeOverlayView QrCodeOverlayView;

        /*
         * Cloneable Controls
         */


        /*
         * Child-Controls
         */

        private Button _loginButton;

        private Label _versionLabel;


        /*
         * Fields, Properties
         */


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");
            _versionLabel = Root.Q<Label>("version-label");

            BindButtons();
            Show(); 
        }


        #region Button Binding
        private void BindButtons()
        {
            _loginButton.clickable.clicked += () =>
            { 
                QrCodeOverlayView.Show();
                QrCodeOverlayView.Rebind();
                this.Hide();
            };

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(UnityUiToolkitTransport.VersionUrl);
            });
        }
        #endregion

        #region Rebind

        public void Rebind()
        {
            _versionLabel.text = UnityUiToolkitTransport.Version;
        }

        #endregion

        #region other


        #endregion
    }
}
