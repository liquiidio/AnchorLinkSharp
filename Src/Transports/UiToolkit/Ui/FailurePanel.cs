using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class FailurePanel : PanelBase
    {
        /*
         * Child-Controls
         */

        private Label _titleLabel;
        private Label _subtitleLabel;

        /*
         * Fields, Properties
         */    
        public bool IsWhiteTheme;
        [SerializeField] internal StyleSheet DarkTheme;
        [SerializeField] internal StyleSheet WhiteTheme;


        private void Start()
        {
            _titleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            OnStart();
            CheckTheme();
        }

        #region others

        public void SetExceptionText(Exception exception)
        {
            _titleLabel.text = "Transaction Error";
            _subtitleLabel.text = exception.Message;
        }
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