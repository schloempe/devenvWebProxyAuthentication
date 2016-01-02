using System;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace devenvWebProxyAuthentication
{
    public class devenvWebProxyModule : IWebProxy
    {
        private readonly string settingsFilename = "devenvWebProxyAuthentication.settings";

        private IWebProxy proxy = null;
        private Uri proxyUri = null;
        private bool successfullyLoggedIn = false;
        private Object thisLock = new Object();

        public devenvWebProxyModule()
        {
#if DEBUG
            LogSingleton.GetInstance.SetLogger(new FileLogger(Path.Combine(Path.GetTempPath(), string.Format("{0}.log", settingsFilename))));
#endif
            LogSingleton.GetInstance.BeginFunction("devenvWebProxyModule Constructor()");
            successfullyLoggedIn = false;
            LogSingleton.GetInstance.EndFunction("devenvWebProxyModule Constructor");
        }

        ~devenvWebProxyModule()
        {
            LogSingleton.GetInstance.BeginFunction("devenvWebProxyModule Destructor()");
            lock (thisLock)
            {
                if (proxyUri != null && !successfullyLoggedIn)
                {
                    try
                    {
                        XMLSettings settings = new XMLSettings(XMLSettingsContext.UserContextRoaming, settingsFilename);
                        settings.PutSettingEncrypted(string.Format("{0}/password", proxyUri.Host), string.Empty);
                        //settings.PutSetting(string.Format("{0}/password", proxyUri.Host), string.Empty);
                    }
                    catch (Exception ex)
                    {
                        LogSingleton.GetInstance.Exception(ex);
                    }
                }
            }
            LogSingleton.GetInstance.EndFunction("devenvWebProxyModule Destructor");
        }

        ICredentials credentials = null;
        public ICredentials Credentials
        {
            get
            {
                LogSingleton.GetInstance.BeginFunction("devenvWebProxyModule Credentials get");
                try
                {
                    lock (thisLock)
                    {
                        if (credentials == null)
                        {
                            LogSingleton.GetInstance.Message("credentials == null -> evaluate");
                            if (proxy == null || proxyUri == null)
                            {
                                LogSingleton.GetInstance.Message("proxy == null -> credentials not needed -> return null");
                                return null;
                            }

                            LogSingleton.GetInstance.Message(string.Format("open XMLSettings -> get settings username and password for proxyUri.Host={0}", proxyUri.Host));
                            XMLSettings settings = new XMLSettings(XMLSettingsContext.UserContextRoaming, settingsFilename);
                            string username = settings.GetSetting(string.Format("{0}/username", proxyUri.Host), string.Empty);
                            string password = settings.GetSettingEncrypted(string.Format("{0}/password", proxyUri.Host), string.Empty);
                            //string password = settings.GetSetting(string.Format("{0}/password", proxyUri.Host), string.Empty);

                            LogSingleton.GetInstance.Message(string.Format("username='{0}', password is null or empty? {1}", username, string.IsNullOrEmpty(password)));
                            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                            {
                                CredentialsForm form = new CredentialsForm();
                                form.txtUsername.Text = username;
                                LogSingleton.GetInstance.Message("CredentialsForm.ShowDialog() ->");
                                DialogResult result = form.ShowDialog();
                                if (result == DialogResult.OK)
                                {
                                    LogSingleton.GetInstance.Message("DialogResult.OK");
                                    username = form.txtUsername.Text;
                                    password = form.txtPassword.Text;

                                    LogSingleton.GetInstance.Message(string.Format("username='{0}', password is null or empty? {1}", username, string.IsNullOrEmpty(password)));
                                    if (form.ckbStoreCredentials.Checked)
                                    {
                                        LogSingleton.GetInstance.Message(string.Format("store credentials for host {0}", proxyUri.Host));
                                        settings.PutSetting(string.Format("{0}/username", proxyUri.Host), username);
                                        settings.PutSettingEncrypted(string.Format("{0}/password", proxyUri.Host), password);
                                        //settings.PutSetting(string.Format("{0}/password", proxyUri.Host), password);
                                    }
                                }
                            }

                            LogSingleton.GetInstance.Message("credentials = new NetworkCredentials(username, password)");
                            credentials = new NetworkCredential(username, password);
                        }
                        else
                        {
                            LogSingleton.GetInstance.Message("credentials != null -> so return them");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogSingleton.GetInstance.Exception(ex);
                }

                LogSingleton.GetInstance.EndFunction("devenvWebProxyModule Credentials get");
                return credentials;
            }
            set
            {
                LogSingleton.GetInstance.BeginFunction("devenvWebProxyModule Credentials set");
                LogSingleton.GetInstance.Message(string.Format("value = {0}", value));
                credentials = value;
                LogSingleton.GetInstance.EndFunction("devenvWebProxyModule Credentials");
            }
        }

        public Uri GetProxy(Uri destination)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("devenvWebProxyModule GetProxy(Uri destination={0})", destination.ToString()));

            try
            {
                if (destination.AbsoluteUri.Contains("/Avatar"))
                {
                    LogSingleton.GetInstance.Message("check, if destination contains '/Avatar' -> means successful logon to Microsoft developer network -> successfullyLoggedIn = true");
                    successfullyLoggedIn = true;
                }
                else if (destination.AbsoluteUri.Contains("/ShippedFlights.json"))
                {
                    LogSingleton.GetInstance.Message("check, if destination contains '/ShippedFlights.json' (VS 2015) -> means successful logon to Microsoft developer network -> successfullyLoggedIn = true");
                    successfullyLoggedIn = true;
                }

                if (proxy == null)
                {
                    LogSingleton.GetInstance.Message("proxy == null -> get WebRequest.GetSystemWebProxy() and store to proxy");
                    proxy = WebRequest.GetSystemWebProxy();
                }

                LogSingleton.GetInstance.Message(string.Format("proxy is '{0}'", proxy.ToString()));

                if (proxy != null)
                {
                    proxyUri = proxy.GetProxy(destination);
                    LogSingleton.GetInstance.Message(string.Format("ask 'inner' proxy.GetProxy for destination -> {0}", proxyUri));
                }

                if (destination == proxyUri)
                {
                    proxyUri = null;
                    LogSingleton.GetInstance.Message("destination is equal to ask proxyUri -> no proxy needed -> proxyUri = null");
                }
            }
            catch (Exception ex)
            {
                LogSingleton.GetInstance.Exception(ex);
            }

            LogSingleton.GetInstance.EndFunction(string.Format("devenvWebProxyModule GetProxy(proxyUri={0})", proxyUri));
            return proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            LogSingleton.GetInstance.BeginFunction(string.Format("devenvWebProxyModule IsBypassed(Uri host={0})", host.ToString()));
            bool isBypassed = false;

            if (proxy != null)
            {
                isBypassed = proxy.IsBypassed(host);
                LogSingleton.GetInstance.Message(string.Format("ask 'inner' proxy.IsBypassed(host) -> {0}", isBypassed));
            }

            LogSingleton.GetInstance.EndFunction(string.Format("devenvWebProxyModule IsBypassed(isBypassed={0})", isBypassed));
            return isBypassed;
        }

    }
}
