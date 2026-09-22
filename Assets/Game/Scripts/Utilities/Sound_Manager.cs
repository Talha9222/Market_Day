using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using AppsFlyerSDK;

public class Sound_Manager : MonoBehaviour
{
    [HideInInspector] public UniWebView webView;
     [HideInInspector] public string place = null; // Change this
    Coroutine injection;
    private float waitTime;
    private bool atLoginPanel = false;
    private bool atRegisterPanel = false;
    private string previousData;
    private string lastEvent;
    public void StartSavingData()
    {
        waitTime = 7;
        webView = gameObject.AddComponent<UniWebView>();
        webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
       // webView.Frame = new Rect(0, Screen.height / 2, Screen.width, Screen.height / 2);
        webView.SetBackButtonEnabled(false);
        webView.SetBouncesEnabled(false);

        // Add URL Scheme
        webView.AddUrlScheme("uniwebview");

        webView.OnPageFinished += OnPageFinished;
        webView.OnMessageReceived += OnMessageReceived;
        if (place != null)
        {
            Debug.Log("Call"+place);
            webView.Load(place);
            webView.Show();

            //Dictionary<string, string> appFlyerID = new Dictionary<string, string>();
            //string appsFlyerId = AppsFlyer.getAppsFlyerId();
            //appFlyerID.Add("AppFlyerID", appsFlyerId);

           // AppsFlyer.sendEvent(appsFlyerId, appFlyerID);

           // Debug.Log("AppsFlyer ID: " + appsFlyerId);

        }else
        {
            Debug.Log("Null Place");
        }    
    }

    void OnPageFinished(UniWebView _view, int statusCode, string url)
    {
        if (previousData != null && previousData == _view.Url)
        {
            return;
        }
        previousData = _view.Url;
        if (atLoginPanel)
        {
            SendButtonClickEventToAppsFlyer("logined");
            atLoginPanel = false;
            atRegisterPanel = false;
        }
        else if (atRegisterPanel)
        {
            SendButtonClickEventToAppsFlyer("registered");
            atRegisterPanel = false;
            atLoginPanel = false;
        }

        if (injection != null)
        {
            StopCoroutine(injection);
        }
        injection = StartCoroutine(Injection());
    }

    IEnumerator Injection()
    {
        yield return new WaitForSeconds(waitTime);
        waitTime = 1;

        string jsCode = @"
        (function() {
            function trackButtons() {
                document.querySelectorAll('div').forEach(div => {
                    let buttonTextEl = div.querySelector('span.ui-button__text');

                    if (buttonTextEl) {
                        if (!div.dataset.tracked) {
                            div.dataset.tracked = 'true'; // Prevent multiple listeners

                            div.addEventListener('click', function(event) {
                                //event.stopPropagation(); // Prevent multiple alerts

                                // Ignore if clicking on the Close button
                                if (event.target.closest('.close-button')) {
                                    return; // Stop execution
                                }

                                // Get fresh text every time
                                let freshText = buttonTextEl.innerText.trim(); 

                                if (freshText === 'Login' || freshText === 'Register') {
                                    window.location.href = 'uniwebview://buttonClick?data=' + encodeURIComponent(freshText);
                                }
                            });
                        }
                    }
                });
            }

            // Run initially (for already loaded buttons)
            trackButtons();

            // Observe DOM changes for dynamically added buttons
            let observer = new MutationObserver(trackButtons);
            observer.observe(document.body, { childList: true, subtree: true });

        })();
        ";

        // Inject JavaScript into UniWebView
        webView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("JavaScript Injected: " + result);
        });


        // JavaScript buttom buttons delection code to inject
        jsCode = @"
    (function() {
        // Track bottom navigation clicks
        document.querySelectorAll('.ui-tab').forEach(tab => {
            tab.addEventListener('click', function() {
                // Get the tab text
                const textElement = this.querySelector('._text_45uqu_93');
                const tabText = textElement ? textElement.textContent.trim() : 'Unknown Button';
                
                // Send button name to Unity via UniWebView
                window.location.href = 'uniwebview://buttonClick?data=' + encodeURIComponent(tabText);
            });
        });
    })();
    ";

        // Evaluate JavaScript in the web view
        webView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("JavaScript Evaluated: " + result);
        });

        //deposited amount code.
        jsCode = @"(function() {
    let selectedAmount = 0;
    let customAmount = 0;
    
    // Track amount button clicks
    document.addEventListener('click', function(e) {
        const amountBtn = e.target.closest('.recommend-amount_on8n1_25 .ui-badge_wrapper');
        if(amountBtn) {
            // Preserve original behavior by clicking the amount label
            const label = amountBtn.querySelector('._label_1vke1_36');
            if(label) label.click();
            
            // Clear custom input
            const customInput = document.querySelector('input.ui-input__input');
            if(customInput) {
                customInput.value = '';
                customInput.dispatchEvent(new Event('input'));
            }
            
            // Get button amount
            selectedAmount = parseInt(label.textContent);
            customAmount = 0;
        }
    });

    // Track custom input changes
    document.addEventListener('input', function(e) {
        if(e.target.matches('input.ui-input__input')) {
            // Clear button selection visually
            document.querySelectorAll('.recommend-amount_on8n1_25 .ui-badge_wrapper').forEach(btn => {
                btn.classList.remove('selected');
            });
            selectedAmount = 0;
            customAmount = parseInt(e.target.value) || 0;
        }
    });

    // Handle deposit click
    document.addEventListener('click', function(e) {
        if(e.target.closest('button.ui-button--default')) {
            const currentInputValue = parseInt(document.querySelector('input.ui-input__input')?.value) || 0;
            const finalAmount = currentInputValue || customAmount || selectedAmount;
            
            if(finalAmount > 0) {
                // Send message to Unity
                window.location.href = 'uniwebview://depositAmount?amount=' + encodeURIComponent(finalAmount);
            } else {
                // Send error message to Unity
                window.location.href = 'uniwebview://depositError?message=' + encodeURIComponent(""Please select or enter a valid amount first!"");
            }
        }
    });

    // Add visual feedback for selected amounts
    const style = document.createElement('style');
    style.textContent = `
        .ui-badge__wrapper.selected ._gou_1vke1_77 {
            display: inline-flex !important;
        }
        .ui-badge__wrapper:not(.selected) ._gou_1vke1_77 {
            display: none !important;
        }
    `;
    document.head.appendChild(style);
})();
";
        webView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("JavaScript Evaluated: " + result);
        });



        // exit button code
        jsCode = @"(function() {
    // Track clicks on the Confirm Exit button
    document.addEventListener('click', function(e) {
        // Find the clicked Confirm Exit button using its unique classes and text
        const confirmExitBtn = e.target.closest('button.ui-dialog__cancel');
        
        if(confirmExitBtn) {
            // Verify the button text to avoid false positives
            const buttonText = confirmExitBtn.querySelector('.ui-button__text')?.textContent?.trim();
            
            if(buttonText === 'Confirm exit') {
                // Send message to Unity using UniWebView
                window.location.href = ""uniwebview://buttonClick?exitbutton=ConfirmExit"";
            }
        }
    });
})();
";

        webView.EvaluateJavaScript(jsCode, (result) =>
        {
            Debug.Log("JavaScript Evaluated: " + result);
        });

    }


    void OnMessageReceived(UniWebView _view, UniWebViewMessage message)
    {
        string fullUrl = message.RawMessage;

        // Print full URL
        // Parse the URL to get the button text
        string buttonText = message.RawMessage.Split('?')[1].Split('=')[1];
        if (lastEvent != null && lastEvent == buttonText)
        {
            return;
        }
        lastEvent = buttonText;

        if (fullUrl.Contains("amount="))
        {
            SendButtonClickEventToAppsFlyer($"amount_deposit_{buttonText}");
            SendingRevinue(buttonText);
        }
        else if (fullUrl.Contains("ConfirmExit"))
        {
            SendButtonClickEventToAppsFlyer("logout");
        }
        else if (fullUrl.Contains("buttonClick?data"))
        {
            SendButtonClickEventToAppsFlyer($"bottom_clicked_{buttonText}");
        }
        else if (fullUrl.Contains("buttonClick?button"))
        {
            SendButtonClickEventToAppsFlyer($"bottom_clicked_{buttonText}");

        }

        
        if(buttonText=="Login")
        {
            atLoginPanel = true;
            atRegisterPanel = false;
        }

        if (buttonText=="Register")
        {
            atRegisterPanel = true;
            atLoginPanel = false;
        }
    }
    public void SendButtonClickEventToAppsFlyer(string buttonName)
    {
        Debug.Log(buttonName);
        // Create a dictionary to hold event parameters
        var eventValues = new Dictionary<string, string>
        {
            { buttonName, buttonName },   // Example: "Log in", "Sign Up", etc.  // Timestamp of the event
        };

        // Send event to AppsFlyer
       // AppsFlyer.sendEvent(buttonName, eventValues); // Replace "button_click_event" with your event name

    }
    public void SendingRevinue(string revenueAmount)
    {
      
        var eventValues = new Dictionary<string, string>
        {
            { "af_revenue", revenueAmount }, // Revenue amount
            { "af_currency", "USD" }                    // Currency (optional, specify as needed)
        };

        // Track event using Appsflyer's sendEvent method (updated method)
       // AppsFlyer.sendEvent("in_app_purchase", eventValues);
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
