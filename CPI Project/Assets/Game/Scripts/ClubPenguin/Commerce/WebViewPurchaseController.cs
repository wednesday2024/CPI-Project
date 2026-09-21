using ClubPenguin.Analytics;
using ClubPenguin.ContentGates;
using ClubPenguin.Core;
using ClubPenguin.Net;
using Disney.Kelowna.Common;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using LitJson;
using System;
using System.Text;
using UnityEngine;

namespace ClubPenguin.Commerce
{
    public class WebViewPurchaseController
    {
        private const string purchaseTitle = "Membership.Purchase.CSG.WebviewerTitle";

        private const string purchaseURLParms = "?config={0}&planId={1}&pageType=checkout";

        private const string manageAccountTitle = "Membership.ManageAccount.CSG.WebviewerTitle";

        private const string manageAccountURLParms = "?config={0}&planId={1}&pageType=manageAccount";

        private const string javaScriptCommFunction = "CPI.Membership.WebviewEventComm";

        private const string javaScriptEnableCommFunction = "CPI.Membership.EnableWebviewEventComm";

        private bool closeUserInitiated = true;

        private CSGConfig csgConfig;

        private string planId;

        public void ShowPurchaseFlow(CSGConfig csgConfig, string planId)
        {
            this.csgConfig = csgConfig;
            this.planId = planId;
            string urlFormat = this.csgConfig.BaseUrl + purchaseURLParms;
            ShowFlow(urlFormat, purchaseTitle);
        }

        public void ReloadPurchaseFlow(CSGConfig csgConfig, string planId)
        {
            this.csgConfig = csgConfig;
            this.planId = planId;
            string urlFormat = this.csgConfig.BaseUrl + purchaseURLParms;
            ShowFlow(urlFormat, purchaseTitle);
        }

        public void ReloadManageAccountFlow(CSGConfig csgConfig)
        {
            this.csgConfig = csgConfig;
            planId = "";
            string urlFormat = this.csgConfig.BaseUrl + manageAccountURLParms;
            ShowFlow(urlFormat, manageAccountTitle);
        }

        public void ShowManageAccountFlow(CSGConfig csgConfig)
        {
            this.csgConfig = csgConfig;
            planId = "";
            string urlFormat = this.csgConfig.BaseUrl + manageAccountURLParms;
            ShowFlow(urlFormat, manageAccountTitle);
        }

        public void ShowFlow(string urlFormat, string title)
        {
        }

        private string setURLParameters(string urlFormat)
        {
            return string.Format(urlFormat, base64Encode(JsonMapper.ToJson(csgConfig)), planId);
        }

        private string base64Encode(string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        private string setURLManageAccountParameters(string urlFormat)
        {
            return string.Format(urlFormat, base64Encode(JsonMapper.ToJson(csgConfig)), planId, "manageAccount");
        }

        private void onWebViewFailed()
        {
        }

        private void onWebViewClosed()
        {
        }

        private void onManagaAccountWebViewFailed()
        {
        }

        private void onManagaAccountWebViewClosed()
        {
        }

        private void onReceivedMessage(JsonData message)
        {
        }

        private void sendBI(JsonData biContent)
        {
        }

        private string getJsonString(JsonData json, string key)
        {
            return json.Contains(key) ? json[key].ToString() : null;
        }
    }
}