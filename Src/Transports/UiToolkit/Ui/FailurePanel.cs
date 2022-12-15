using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class FailurePanel : PanelBase
    {
        /*
         * Child-Controls
         */

        private Label _titleLabel;
        private Label _subtitleLabel;

        private void Start()
        {
            _titleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            OnStart();
        }
    }
}