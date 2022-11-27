using System;
using System.Collections;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosioSigningRequest;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    [RequireComponent(typeof(SuccessPanel))]
    public class SuccessPanel : PanelBase
    {
        /*
         * Child-Controls
         */
        private Label _subTitleLabel;

        void Start()
        {
            _subTitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            OnStart();
        }

        #region Rebind

        public void Rebind(SigningRequest request)
        {
            if (request.IsIdentity())
            {
                _subTitleLabel.text = "Login completed.";
            }
            else _subTitleLabel.text = "Transaction signed";

            StartCoroutine(SetTimeout());
        }

        #endregion

        #region other

        private IEnumerator SetTimeout(float counterDuration = 0.5f)
        {
            float _newCounter = 0;
            while (_newCounter < counterDuration * 2)
            {
                _newCounter += Time.deltaTime;
                yield return null;
            }
            this.Hide();
        }
        #endregion
    }
}
