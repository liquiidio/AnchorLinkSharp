using System;
using System.Collections;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosSharp.Core.Api.v1;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UIElements;
using Action = System.Action;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    [RequireComponent(typeof(UIDocument))]
    public class ScreenBase : MonoBehaviour
    {

        internal UIDocument Screen;
        internal VisualElement Root;

        void Awake()
        {
            Screen = GetComponent<UIDocument>();
            Root = Screen.rootVisualElement;

            Hide();
        }

        //show the view
        public void Show()
        {
            Root.style.visibility = Visibility.Visible;
            Root.style.display = DisplayStyle.Flex;
        }

        //hide the view
        public void Hide()
        {
            Root.style.visibility = Visibility.Hidden;
            Root.style.display = DisplayStyle.None;
        }
    }

}
