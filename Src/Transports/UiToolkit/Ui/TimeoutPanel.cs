using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class TimeoutPanel : PanelBase
    {
        /**
         * Child-Controls
         */
        private Button _signManuallyButton;

        /**
         * Fields, Properties
         */
        [SerializeField] internal QrCodePanel QrCodePanel;

        private void Start()
        {
            _signManuallyButton = Root.Q<Button>("sign-manually-button");

            OnStart();
            BindButtons();
        }

        #region Button Binding

        /// <summary>
        /// Assign UI toolkit interaction events
        /// </summary>
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