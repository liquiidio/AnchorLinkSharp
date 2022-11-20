using UnityEngine;
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
        public bool IsWhiteTheme;
        public QrCodePanel QrCodePanel;
        [SerializeField] internal StyleSheet DarkTheme;
        [SerializeField] internal StyleSheet WhiteTheme;

        private void Start()
        {
            _signManuallyButton = Root.Q<Button>("sign-manually-button");

            OnStart();
            BindButtons();
            CheckTheme();
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

        #region Others
        private void CheckTheme()
        {
            Root.styleSheets.Clear();

            if (IsWhiteTheme)
            {
                Root.styleSheets.Remove(DarkTheme);
                Root.styleSheets.Add(WhiteTheme);
            }
            else
            {

                Root.styleSheets.Remove(WhiteTheme);
                Root.styleSheets.Add(DarkTheme);
            }
        }
        #endregion
    }
}