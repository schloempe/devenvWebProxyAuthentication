using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace devenvWebProxyAuthentication
{
    public enum XMLSettingsContext {UserContextLocal = 1, UserContextRoaming, IndividualContext};

    public class XMLSettings : IDisposable
    {
        private static readonly byte[] _aditionalEntropy = { 1, 9, 2, 1, 5 };

        private XmlDocument _xmlDocument = new XmlDocument();
        private string _sDocumentName;

        bool _isSaved = false;

        public XMLSettings(XMLSettingsContext context, string filename)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings Constructor(context={0}, filename={1})", context, filename));

            if (context == XMLSettingsContext.UserContextLocal)
            {
                _sDocumentName = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), filename);
            }
            else if (context == XMLSettingsContext.UserContextRoaming)
            {
                _sDocumentName = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), filename);
            }
            else
            {
                _sDocumentName = filename;
            }
            LogSingleton.GetInstance.Message(string.Format("_sDocumentName={0}", _sDocumentName));

            try
            {
                _xmlDocument.Load(_sDocumentName);
                LogSingleton.GetInstance.Message("_xmlDocument loaded...");
            }
            catch
            {
                LogSingleton.GetInstance.Warning("unable to load XML settings file -> create new empty file");
                _xmlDocument.LoadXml("<settings></settings>");
            }

            _isSaved = false;

            LogSingleton.GetInstance.EndFunction("XMLSettings Constructor");
        }


        public void Dispose()
        {
            LogSingleton.GetInstance.BeginFunction("XMLSettings Dispose()");

            if (!_isSaved)
            {
                try
                {
                    _xmlDocument.Save(_sDocumentName);
                    LogSingleton.GetInstance.Message("_xmlDocument saved ...");
                }
                catch (XmlException ex)
                {
                    LogSingleton.GetInstance.Exception(ex);
                }

                _isSaved = true;
            }
            else
            {
                LogSingleton.GetInstance.Message("already saved -> nothing to do");
            }

            LogSingleton.GetInstance.EndFunction("XMLSettings Dispose");
        }

        ~XMLSettings()
        {
            this.Dispose();
        }

        public int GetSetting(string xPath, int defaultValue)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings GetSetting(xPath={0}, int defaultValue={1})", xPath, defaultValue));

            int returnValue = Convert.ToInt32(GetSetting(xPath, Convert.ToString(defaultValue)));

            LogSingleton.GetInstance.EndFunction(string.Format("XMLSettings GetSetting(returnValue={0}", returnValue));
            return returnValue;
        }

        public string GetSettingBase64(string xPath, string defaultValue)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings GetSettingBase64(xPath={0}, string defaultValue={1})", xPath, defaultValue));

            string returnValue = GetSetting(xPath, defaultValue);
            if (returnValue.Equals(defaultValue))
            {
                PutSettingBase64(xPath, defaultValue);
            }
            else
            {
                byte[] based = Convert.FromBase64String(returnValue);
                returnValue = Encoding.UTF8.GetString(based);
            }

            LogSingleton.GetInstance.EndFunction(string.Format("XMLSettings GetSettingBase64(returnValue={0}", returnValue));
            return returnValue;
        }

        public string GetSettingEncrypted(string xPath, string defaultValue)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings GetSettingEncrypted(xPath={0}, string defaultValue=*)", xPath));

            string returnValue = string.Empty;

            XmlNode xmlNode = _xmlDocument.SelectSingleNode("settings/" + xPath);
            if (xmlNode != null)
            {
                returnValue = xmlNode.InnerText;
                byte[] decrypted = ProtectedData.Unprotect(Convert.FromBase64String(returnValue), _aditionalEntropy, DataProtectionScope.CurrentUser);
                returnValue = Encoding.UTF8.GetString(decrypted);
            }
            else
            {
                LogSingleton.GetInstance.Warning(string.Format("xPath not found -> create new node with default value"));
                PutSettingEncrypted(xPath, defaultValue);
                returnValue = defaultValue;
            }

            LogSingleton.GetInstance.EndFunction("XMLSettings GetSettingEncrypted(returnValue=*");
            return returnValue;
        }

        public string GetSetting(string xPath, string defaultValue)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings GetSetting(xPath={0}, int defaultValue={1})", xPath, defaultValue));

            string returnValue = string.Empty;

            XmlNode xmlNode = _xmlDocument.SelectSingleNode("settings/" + xPath);
            if (xmlNode != null)
            {
                returnValue = xmlNode.InnerText;
            }
            else
            {
                LogSingleton.GetInstance.Warning(string.Format("xPath not found -> create new node with default value"));
                PutSetting(xPath, defaultValue);
                returnValue = defaultValue;
            }

            LogSingleton.GetInstance.EndFunction(string.Format("XMLSettings GetSetting(returnValue={0}", returnValue));
            return returnValue;
        }

        public void PutSetting(string xPath, int value)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings PutSetting(xPath={0}, int value={1})", xPath, value));
            PutSetting(xPath, Convert.ToString(value));
            LogSingleton.GetInstance.EndFunction("XMLSettings PutSetting");
        }

        public void PutSettingBase64(string xPath, string value)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings PutSetting(xPath={0}, string value={1})", xPath, value));
            byte[] encbuff = Encoding.UTF8.GetBytes(value);
            PutSetting(xPath, Convert.ToBase64String(encbuff));
            LogSingleton.GetInstance.EndFunction("XMLSettings PutSettingBase64");
        }

        public void PutSettingEncrypted(string xPath, string value)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings PutSettingEncrypted(xPath={0}, string value=*)", xPath));
            XmlNode xmlNode = _xmlDocument.SelectSingleNode("settings/" + xPath);
            if (xmlNode == null)
            {
                LogSingleton.GetInstance.Message("setting not found -> create new XML node");
                xmlNode = CreateMissingNode("settings/" + xPath);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] encrypted = ProtectedData.Protect(bytes, _aditionalEntropy, DataProtectionScope.CurrentUser);
            xmlNode.InnerText = Convert.ToBase64String(encrypted);
            _isSaved = false;
            LogSingleton.GetInstance.EndFunction("XMLSettings PutSettingEncrypted");
        }

        public void PutSetting(string xPath, string value)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings PutSetting(xPath={0}, string value={1})", xPath, value));

            XmlNode xmlNode = _xmlDocument.SelectSingleNode("settings/" + xPath);
            if (xmlNode == null)
            {
                LogSingleton.GetInstance.Message("setting not found -> create new XML node");
                xmlNode = CreateMissingNode("settings/" + xPath);
            }

            xmlNode.InnerText = value;
            _isSaved = false;
            LogSingleton.GetInstance.EndFunction("XMLSettings PutSetting");
        }

        public XmlNode CreateMissingNode(string xPath)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("XMLSettings CreateMissingNode(xPath={0})", xPath));

            string[] xPathSections = xPath.Split('/');
            string currentXPath = null;
            XmlNode testNode = null;
            XmlNode currentNode = _xmlDocument.SelectSingleNode("settings");

            foreach (string xPathSection in xPathSections)
            {
                currentXPath += xPathSection;
                testNode = _xmlDocument.SelectSingleNode(currentXPath);
                if (testNode == null)
                {
                    currentNode.InnerXml += "<" + xPathSection + "></" + xPathSection + ">";
                }
                currentNode = _xmlDocument.SelectSingleNode(currentXPath);
                currentXPath += "/";
            }

            LogSingleton.GetInstance.EndFunction("XMLSettings CreateMissingNode");
            return currentNode;
        }
    }
}
