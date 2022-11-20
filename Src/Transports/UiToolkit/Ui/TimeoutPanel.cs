using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class TimeoutPanel : PanelBase
    {
        /*
         * Child-Controls
         */
        private Button _signManuallyButton;


        /*
         * Fields, Properties
         */
        public QrCodePanel QrCodePanel;

        private void Start()
        {
            _signManuallyButton = Root.Q<Button>("sign-manually-button");

            OnStart();
            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _signManuallyButton.clickable.clicked += () =>
            {
                QrCodePanel.Rebind(true);
                StartCoroutine(UnityUiToolkitTransport.TransitionPanels(QrCodePanel));
            };
        }

        #endregion
    }
}