using System;
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

        private void Start()
        {
            _titleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            OnStart();
        }

        #region others

        public void SetExceptionText(Exception exception)
        {
            _titleLabel.text = "Transaction Error";
            _subtitleLabel.text = exception.Message;
        }

        #endregion
    }
}