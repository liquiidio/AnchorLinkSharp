using System.Collections;
using System.Collections.Generic;
using Assets.Packages.AnchorLinkTransportSharp;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class LoadingPanel : PanelBase
    {
        public bool IsWhiteTheme;
        [SerializeField] internal StyleSheet DarkTheme;
        [SerializeField] internal StyleSheet WhiteTheme;

        void Start()
        {
            OnStart();
            CheckTheme();
        }

        #region others

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
