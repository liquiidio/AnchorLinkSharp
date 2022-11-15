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

        public LinkSession Session;

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

        //public void Show()
        //{
        //    Root.Show(this);
        //    OnShow?.Invoke();
        //    IsScreenVisible = true;
        //}

        //public void Hide()
        //{
        //    Root.Hide();
        //    OnHide?.Invoke();
        //    IsScreenVisible = false;
        //}

        ///// <summary>
        ///// Extension-method to show an UI Element (set it to visible)
        ///// </summary>
        ///// <param name="element"></param>
        //public void Show(VisualElement element)
        //{
        //    if (element == null)
        //        return;

        //    element.style.visibility = Visibility.Visible;
        //    element.style.display = DisplayStyle.Flex;
        //}

        ///// <summary>
        ///// Extension-method to hide an UI Element (set it to invisible)
        ///// </summary>
        ///// <param name="element"></param>
        //public void Hide(VisualElement element)
        //{
        //    if (element == null)
        //        return;

        //    element.style.visibility = Visibility.Hidden;
        //    element.style.display = DisplayStyle.None;
        //}
    }

}
