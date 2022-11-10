using System.Collections;
using System.Collections.Generic;
using Assets.Packages.AnchorLinkTransportSharp;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.UI.ScriptsAndUxml
{
    public class LoadingOverlayView : ScreenBase
    {
        /*
         * Connected Views
         */

        
        /*
         * Cloneable Controls
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
            //Grab a reference to the root element that is drawn
            Root = GetComponent<UIDocument>().rootVisualElement;

            _closeViewButton = Root.Q<Button>("close-view-button");

            _versionLabel = Root.Q<Label>("version-label");

            //_versionLabel.text = Version;

            BindButtons();
        }


        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += () =>
            {
                this.Hide();
            };

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
