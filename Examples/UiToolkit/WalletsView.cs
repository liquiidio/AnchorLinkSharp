using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.UI.ScriptsAndUxml
{
    public class WalletsView : ScreenBase
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
        private Button _anchorWalletButton;
        private Button _waxWalletButton;
        private Button _combatWalletButton;


        /*
         * Fields, Properties
         */
        internal UIDocument Screen;
        internal VisualElement Root;


        void Start()
        {
            //Grab a reference to the root element that is drawn
            Root = GetComponent<UIDocument>().rootVisualElement;

            _closeViewButton = Root.Q<Button>("close-view-button");
            _anchorWalletButton = Root.Q<Button>("anchor-wallet-button");
            _waxWalletButton = Root.Q<Button>("wax-wallet-button");

            BindButtons();
        }


        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += () =>
            {
                //this.Hide();
                Root.style.visibility = Visibility.Hidden;
                Root.style.display = DisplayStyle.None;
            };

            _anchorWalletButton.clickable.clicked += () =>
            {

            };
        }
        #endregion
    }
}
