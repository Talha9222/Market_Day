using AdjustSdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;

//using AppsFlyerSDK;
using Unity.VisualScripting;
using UnityEngine;
using static System.Net.WebRequestMethods;

public class GameManager_User : MonoBehaviour
{

    [HideInInspector] public UniWebView userView;
    public string place = null; // Change this
    Coroutine injection;
    string lastEvent = null;
    string last = "A";
    bool firstTime = true;
    int timeOfCorotine = 5;


    public bool orientationUpdated = false;
    private void Start()
    {
        //adjust initialize
        StartCoroutine(Delay());
    }

    int lastWidth = -1, lastHeight = -1;

    private void Update()
    {
        if (userView == null) return;

        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            userView.Frame = new Rect(0, 0, Screen.width, Screen.height);
        }
    }

    //webview on
    public void StartSavingData()
    {
        UniWebView.SetAllowJavaScriptOpenWindow(true);
        userView = gameObject.AddComponent<UniWebView>();
        SetPortrait();

        userView.Frame = new Rect(0, 0, Screen.width, Screen.height);
        // userView.Frame = new Rect(0, Screen.height / 2, Screen.width, Screen.height / 2);
        userView.AddUrlScheme("uniwebview");
        userView.OnPageFinished += OnPageFinished;
        userView.OnMessageReceived += OnMessageReceived;
        userView.SetSupportMultipleWindows(true, true);
        userView.SetAllowHTTPAuthPopUpWindow(true);
        userView.SetOpenLinksInExternalBrowser(true);
        if (place != null)
        {

            Debug.Log("Call" + place);
            userView.Load(place);
            userView.Show();


            //App Flyer
            /* Dictionary<string, string> appFlyerID = new Dictionary<string, string>();
             string appsFlyerId = AppsFlyer.getAppsFlyerId();
             appFlyerID.Add("AppFlyerID", appsFlyerId);

             AppsFlyer.sendEvent(appsFlyerId, appFlyerID);

             Debug.Log("AppsFlyer ID: " + appsFlyerId);
             */

        }
        else
        {
            Debug.LogError("Null Place");
        }


    }

    public void SetPortrait()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }

    //new
    void Injection()
    {
        string jsCode = @"
    (function() {
    if (window.__unityRouteWatcherActive) return;
    window.__unityRouteWatcherActive = true;

    function reportRoute() {
        var route = window.location.hash || window.location.pathname;
        window.location.href = 'uniwebview://routeChange?data=' + encodeURIComponent(route);
    }

    // catches back/forward and hash-based router navigation
    window.addEventListener('hashchange', reportRoute);

    // catches SPA navigation done via pushState/replaceState (not all frameworks fire hashchange for this)
    var _pushState = history.pushState;
    var _replaceState = history.replaceState;
    history.pushState = function() { _pushState.apply(history, arguments); reportRoute(); };
    history.replaceState = function() { _replaceState.apply(history, arguments); reportRoute(); };
    window.addEventListener('popstate', reportRoute);

    // report current route immediately (covers the very first OnPageFinished)
    reportRoute();
})();
    ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Click watcher injected: " + result);
        });
    }

    void InjectionDebugClicks()
    {
        string jsCode = @"
    (function() {
        if (window.__unityDebugClickActive) return;
        window.__unityDebugClickActive = true;

        document.addEventListener('click', function(e) {
            var el = e.target.closest('a, button, div, span, [role=button], input') || e.target;
            var text = (el.innerText || el.value || '').trim().substring(0, 40);
            var cls = (typeof el.className === 'string') ? el.className : '';
            var info = el.tagName + ' | text=' + text + ' | id=' + (el.id || '') + ' | class=' + cls;
            window.location.href = 'uniwebview://debugClick?data=' + encodeURIComponent(info);
        }, true);
    })();
    ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Debug click watcher injected: " + result);
        });
    }



    void CodeForApiInterception()
    {
        string jsCode = @"
(function() {
    if (window.__unityApiInterceptActive) return;
    window.__unityApiInterceptActive = true;

    function report(kind) {
        var iframe = document.createElement('iframe');
        iframe.style.display = 'none';
        iframe.src = 'uniwebview://apiCall?data=' + kind;
        document.body.appendChild(iframe);
        setTimeout(function() { iframe.remove(); }, 100);
    }

    function checkUrl(url, method) {
        var u = (url || '').toLowerCase();
        var m = (method || 'GET').toUpperCase();
        if (m !== 'POST') return; // only real submissions, not page-load data fetches

        if (u.indexOf('recharge') !== -1 || u.indexOf('deposit') !== -1) {
            report('deposit');
        } else if (u.indexOf('withdraw') !== -1 || u.indexOf('cash') !== -1) {
            report('withdraw');
        }
    }

    var _fetch = window.fetch;
    window.fetch = function(input, init) {
        var url = (typeof input === 'string') ? input : (input && input.url);
        var method = (init && init.method) || (input && input.method) || 'GET';
        checkUrl(url, method);
        return _fetch.apply(this, arguments);
    };

    var _open = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function(method, url) {
        checkUrl(url, method);
        return _open.apply(this, arguments);
    };
})();
";
        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("API interception injected: " + result);
        });
    }

    void CodeForcommercialization()
    {
        string jsCode = @"
       (function() {
           if (window.__unityDepositWithdrawClickActive) return;
           window.__unityDepositWithdrawClickActive = true;

           document.addEventListener('click', function(e) {
               var path = e.composedPath ? e.composedPath() : [e.target];

               var el = path.find(function(node) {
                   return node.classList &&
                       (node.classList.contains('bottom_btn') || node.classList.contains('submit-btn'));
               });
               if (!el) return;

               var text = (el.innerText || '').replace(/\s+/g, ' ').trim().toLowerCase();

               if (text === 'deposit') {
                   window.location.href = 'uniwebview://depositAmount?data=clicked';
               } else if (text === 'withdraw now') {
                   window.location.href = 'uniwebview://withdrawClick?data=clicked';
               }
           }, true); // capture:true - required to see clicks from inside (nested) shadow DOM
       })();
       ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Deposit/Withdraw click watcher injected: " + result);
        });
    }

    IEnumerator InjectcommercializationWatcherRetry()
    {
        for (int i = 0; i < 6; i++) // retry for ~3 seconds
        {
            CodeForcommercialization();
            yield return new WaitForSeconds(0.5f);
        }
    }

    void ClearStaleOverlays()
    {
        string jsCode = @"
    (function() {
        document.querySelectorAll('ion-modal, .overlay-hidden').forEach(function(el) {
            el.style.pointerEvents = 'none';
            el.style.display = 'none';
        });
    })();
    ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Stale overlay cleanup: " + result);
        });
    }

    void CodeForAccount()
    {
        string jsCode = @"
    (function() {
        if (window.__unityLoginRegisterClickActive) return;
        window.__unityLoginRegisterClickActive = true;

        document.addEventListener('click', function(e) {
            var path = e.composedPath ? e.composedPath() : [e.target];

            var el = path.find(function(node) {
                if (!node.classList) return false;
                var text = (node.innerText || '').replace(/\s+/g, ' ').trim().toLowerCase();
                var isHeaderLink = node.classList.contains('login') || node.classList.contains('register');
                var isSubmitLike = (node.tagName === 'BUTTON' || node.tagName === 'DIV') &&
                                    (text === 'log in' || text === 'register');
                return isHeaderLink || isSubmitLike;
            });
            if (!el) return;

            var text = (el.innerText || '').replace(/\s+/g, ' ').trim().toLowerCase();
            var isHeader = el.classList.contains('login') || el.classList.contains('register');

            if (text === 'log in') {
                window.location.href = 'uniwebview://loginClick?data=' + (isHeader ? 'opened' : 'submitted');
            } else if (text === 'register') {
                window.location.href = 'uniwebview://registerClick?data=' + (isHeader ? 'opened' : 'submitted');
            }
        }, true);
    })();
    ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Login/Register click watcher injected: " + result);
        });
    }


    void OnPageFinished(UniWebView _view, int statusCode, string url)
    {

        Debug.Log($"[T={Time.time:F2}] OnPageFinished - url={url}");

        Injection();
        InjectionDebugClicks();
        // JsCodeForDepositWithdrawClicks();
        CodeForAccount();
        CodeForApiInterception();

        Debug.Log("injectd!!");

    }
    void RemoveBackBtn()
    {
        string jsCode = @"(function() {
    // Remove the left navigation button container
    var leftButton = document.querySelector('.navbar__content-left');
    if (leftButton) {
        leftButton.style.display = 'none';
    }

    // Alternatively, hide the icon directly or remove its container
    var leftIcon = document.querySelector('.van-icon-arrow-left');
    if (leftIcon) {
        leftIcon.style.display = 'none';
        // Or remove the whole navbar section
        // leftIcon.closest('.navbar__content-left')?.remove();
    }
})();


";
        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("JavaScript Evaluated: " + result);
        });

    }
    void injectionAccount()
    {
        string forRegistrationandlogin = @"
        (function() {
            document.querySelectorAll('a, div, span, input[type = 'button'], button',[role=button]').forEach(button => {
                button.addEventListener('click', function() {
                    var buttonText = encodeURIComponent(button.innerText || 'no-text');
                    // Use custom URL scheme to send data back to Unity
                    window.location.href = 'uniwebview://buttonClick?data=' + buttonText;
                });
            });
        })();
   ";

        // Evaluate JavaScript in the web view
        userView.EvaluateJavaScript(forRegistrationandlogin, (result) =>
        {
            Debug.Log("JavaScript Evaluated: " + result);
        });
    }
    void Codecommercialization()
    {
        string jsCode = @"
        (function() {
            const depositBtn = document.querySelector('.Recharge__container-rechageBtn');
            if (!depositBtn) return;
            
            // Store original form submission function
            const originalFormSubmit = depositBtn.onclick;
            
            depositBtn.addEventListener('click', function(e) {
                // First capture the amount data
                let selectedAmount = null;
                const activeItem = document.querySelector('.Recharge__content-paymoney__money-list__item.active');
                
                if (activeItem) {
                    selectedAmount = activeItem.innerText.trim();
                } else {
                    const inputValue = document.querySelector('.amount-input input').value.trim();
                    if (inputValue) {
                        selectedAmount = inputValue;
                    }
                }
                
                // Create a hidden iframe to send data to Unity without interrupting flow
                const iframe = document.createElement('iframe');
                iframe.style.display = 'none';
                iframe.src = 'uniwebview://depositAmount?data=' + encodeURIComponent(selectedAmount || 'no-amount');
                document.body.appendChild(iframe);
                setTimeout(() => iframe.remove(), 100);
                
                // If there was original click handler, call it after a small delay
                if (originalFormSubmit) {
                    setTimeout(() => originalFormSubmit.call(this, e), 50);
                }
                
                return true;
            });
        })();



    ";

        userView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("Deposit form JS evaluated: " + result);
        });
    }

    void OnMessageReceived(UniWebView _view, UniWebViewMessage message)
    {
        Debug.Log("RAW MESSAGE: " + message.RawMessage);
        string fullUrl = message.RawMessage;
        if (fullUrl.Contains("routeChange"))
        {
            string encodedRoute = fullUrl.Split(new[] { "data=" }, StringSplitOptions.None)[1];
            string route = Uri.UnescapeDataString(encodedRoute);
            Debug.Log("RAW MESSAGE: " + route);
            HandleRouteChange(route);
            return;
        }


        if (fullUrl.Contains("loginClick"))
        {
            SendAdjustEvent("logined");
            return;
        }

        if (fullUrl.Contains("registerClick"))
        {
            SendAdjustEvent("registered");
            return;
        }

        if (fullUrl.Contains("apiCall"))
        {
            string encoded = fullUrl.Split(new[] { "data=" }, StringSplitOptions.None)[1];
            string data = Uri.UnescapeDataString(encoded);
            TimeSpan diff = DateTime.UtcNow - walletEnteredAt;
            Debug.Log($"apiCall={data} - lastRouteEvent={lastRouteEvent} - diff={diff.TotalSeconds:F2}s");
            if (data == "deposit" && lastRouteEvent == "wallet")
            {
                if (diff < WalletLoadGrace)
                {
                    Debug.Log($"IGNORED deposit - diff {diff.TotalSeconds:F2}s < grace {WalletLoadGrace.TotalSeconds}s");
                    return;
                }
                Debug.Log("FIRING deposit event");
                SendAdjustEvent("deposit");
            }
            else if (data == "withdraw" && lastRouteEvent == "withdraw")
            {
                SendAdjustEvent("withdraw");
            }
            return;
        }


    }

    string lastRouteEvent = null;
    private DateTime walletEnteredAt = DateTime.MinValue;
    private static readonly TimeSpan WalletLoadGrace = TimeSpan.FromSeconds(2);
    void HandleRouteChange(string route)
    {
        route = route.ToLower();
        Debug.Log($"routeChange - route={route} - lastRouteEvent={lastRouteEvent}");
        if (route.Contains("/main/wallet/recharge/false") || route.Contains("wallet/recharge_two"))
        {
            if (lastRouteEvent != "wallet")
            {
                walletEnteredAt = DateTime.UtcNow;
                Debug.Log($"walletEnteredAt RESET to {walletEnteredAt:HH:mm:ss.fff}");
            }
            FireRouteEvent("wallet");
        }
        else if (route.Contains("wallet/cash"))
            FireRouteEvent("withdraw");
        else if (route.Contains("/main/home"))
            FireRouteEvent("home");
        else if (route.Contains("/main/activity"))
            FireRouteEvent("promotion");   // /main/activity is actually your "promotion" section
        else if (route.Contains("/main/promotion"))
            FireRouteEvent("activity");     // /main/promotion is actually "invites" - needs its own event
                                            // fallback for generic wallet landing page
    }

    void FireRouteEvent(string eventName)
    {
        if (lastRouteEvent == eventName) return; // debounce repeats of the same screen
        lastRouteEvent = eventName;
        SendButtonClickEventToAppsFlyer(eventName);
    }







    public void SendButtonClickEventToAppsFlyer(string buttonName)
    {
        //AppFlyer
        /*Debug.Log(buttonName);
        var eventValues = new Dictionary<string, string>
     {
         { buttonName, buttonName },   // Example: "Log in", "Sign Up", etc.  // Timestamp of the event
     };
        // AppsFlyer.sendEvent(buttonName, eventValues); // Replace "button_click_event" with your event name
        */

        //Adjust
        SendAdjustEvent(buttonName);

    }
    public int ExtractAmountBeforeK(string input)
    {
        // Find the pattern: any digits right before 'K' that come after letters
        Match match = Regex.Match(input, @"([a-zA-Z]+)(\d+)([kK])");

        if (match.Success)
        {
            string numberBeforeK = match.Groups[2].Value;
            if (int.TryParse(numberBeforeK, out int amount))
            {
                return amount * 1000; // Multiply by 1000 for 'K'
            }
        }

        return 0; // Return 0 if no match found
    }
    public void SendRevenue(int revenueAmount, string currency = "USD")
    {
        // Validate input parameters
        if (revenueAmount < 0)
        {
            Debug.LogWarning("Revenue amount cannot be negative. Value will be converted to positive.");
            revenueAmount = Mathf.Abs(revenueAmount);
        }

        if (string.IsNullOrEmpty(currency) || currency.Length != 3)
        {
            Debug.LogWarning("Invalid currency code. Defaulting to USD.");
            currency = "USD";
        }

        // Create and send the revenue event
        var eventValues = new Dictionary<string, string>
  {
      { "af_revenue", revenueAmount.ToString() },  // Convert int to string
      { "af_currency", currency.ToUpper() }        // Ensure uppercase currency code
  };
        //AppFlyer
        // Track revenue event
        /* AppsFlyer.sendEvent("af_purchase", eventValues);

         // Optional: Also track as standard in-app purchase
          AppsFlyer.sendEvent("in_app_purchase", eventValues);

          Debug.Log($"Revenue tracked: {revenueAmount} {currency}");
          */
    }
    public int ExtractLastNumberBeforeAlphabet(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        // Reverse the string to find the last number before alphabet
        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        string reversed = new string(charArray);

        // Extract digits and decimal points until we hit a letter
        string numericString = string.Empty;
        foreach (char c in reversed)
        {
            if (char.IsLetter(c))
                break; // Stop when we hit an alphabet character

            if (char.IsDigit(c) || c == '.')
                numericString += c;
        }

        if (numericString.Length == 0)
            return 0;

        // Reverse back to get original number order
        char[] numArray = numericString.ToCharArray();
        Array.Reverse(numArray);
        string finalNumber = new string(numArray);

        // Handle thousand indicator (if present in original string)
        bool isThousand = input.ToLower().Contains("k") &&
                          input.IndexOf("k") > input.IndexOf(finalNumber);

        double value = double.Parse(finalNumber, CultureInfo.InvariantCulture);
        return (int)(isThousand ? value * 1000 : value);
    }


    //ADJUST Initialization
    private void AdjustInitialization()
    {
#if UNITY_ANDROID
        string appToken = "xv1o8juu40lc";
#elif UNITY_IOS
        string appToken = "xv1o8juu40lc";
#elif UNITY_EDITOR
        string appToken = "xv1o8juu40lc";
#endif

        AdjustConfig config = new AdjustConfig(appToken, AdjustEnvironment.Production);

        config.LogLevel = AdjustLogLevel.Verbose;
        Adjust.InitSdk(config);


    }

    private IEnumerator Delay()
    {
        AdjustInitialization();

        yield return new WaitForSeconds(2f);

    }

    // Adjust Event Tracking

    public void SendAdjustEvent(string eventName)

    {
        Debug.Log("Adjust Event Called: " + eventName);

        switch (eventName)
        {
            case "logined":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Logined));
                break;

            case "registered":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Registered));
                break;

            case "logout":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Logout));
                break;

            case "home":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Home));
                break;

            case "activity":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Activity));
                break;

            case "promotion":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Promotion));
                break;

            case "wallet":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Wallet));
                break;

            case "main":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Main));
                break;

            case "recharge":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Recharge));
                break;

            case "withdraw":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.Withdraw));
                break;
            case "deposit":
                Adjust.TrackEvent(new AdjustEvent(AdjustEvents.deposit));
                break;

            default:
                Debug.LogWarning($"No Adjust token configured for event: {eventName}");
                break;
        }
    }




    void Function1()
{
    Debug.Log("Function 1 started");
    Debug.Log("Doing some work");
    Debug.Log("Function 1 completed");
}

void Function2()
{
    Vector3 position = transform.position;
    position.x += 1f;
    transform.position = position;
    Debug.Log("Object moved");
}

void Function3()
{
    Vector3 position = transform.position;
    position.y += 1f;
    transform.position = position;
    Debug.Log("Object moved upward");
}

void Function4()
{
    transform.Rotate(0f, 90f, 0f);
    Debug.Log("Object rotated");
    Debug.Log("Rotation completed");
}

void Function5()
{
    bool active = gameObject.activeSelf;
    Debug.Log("Object active: " + active);
    Debug.Log("Checking object state");
}

void Function6()
{
    transform.localScale = Vector3.one;
    Debug.Log("Scale reset");
    Debug.Log("Object returned to normal size");
}

void Function7()
{
    transform.position = Vector3.zero;
    transform.rotation = Quaternion.identity;
    Debug.Log("Transform reset");
}

void Function8()
{
    string objectName = gameObject.name;
    Debug.Log("Object name: " + objectName);
    Debug.Log("Object information checked");
}
}