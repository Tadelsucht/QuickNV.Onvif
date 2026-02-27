using QuickNV.Onvif.Factorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace QuickNV.Onvif
{
    public class OnvifClient : IDisposable
    {
        public OnvifClientOptions Options { get; private set; }
        public Uri DeviceServiceAddressUri { get; private set; }

        public Device.GetDeviceInformationResponse DeviceInformation { get; private set; }
        public Device.Capabilities Capabilities { get; private set; }
        public Device.DeviceClient DeviceClient { get; private set; }
        public ClientFactory ClientFactory { get; private set; }
        public OnvifClient(OnvifClientOptions options)
        {
            this.Options = options;
        }

        public string CorrectUri(string addr, bool includePort = true)
        {
            var uriBuilder = new UriBuilder(addr);
            uriBuilder.Path = NormalizeOnvifServicePath(uriBuilder.Path);
            if (uriBuilder.Host != Options.Host)
                uriBuilder.Host = Options.Host;
            if (includePort)
            {
                // Keep explicit non-default service ports from capabilities;
                // only force the device port when endpoint does not advertise one.
                var hasExplicitNonDefaultPort = !uriBuilder.Uri.IsDefaultPort;
                if (!hasExplicitNonDefaultPort && uriBuilder.Port != Options.Port)
                    uriBuilder.Port = Options.Port;
            }
            return uriBuilder.Uri.ToString();
        }

        private static string NormalizeOnvifServicePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var segments = path
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 4)
                return path;
            if (!segments[0].Equals("onvif", StringComparison.OrdinalIgnoreCase))
                return path;
            if (!segments[1].StartsWith("ver", StringComparison.OrdinalIgnoreCase))
                return path;
            if (!segments[^1].Equals("wsdl", StringComparison.OrdinalIgnoreCase))
                return path;

            var serviceName = segments[^2];
            if (string.IsNullOrWhiteSpace(serviceName))
                return path;

            return $"/onvif/{serviceName}_service";
        }

        private void handleCapabilitiesItem<T>(T t, Func<T, string> getter, Action<T, string> setter)
            where T : class, new()
        {
            if (t == null)
                return;
            var v = getter(t);
            if (!string.IsNullOrEmpty(v))
                setter(t, CorrectUri(v));
        }

        private T handleCapabilitiesExtensionItem<T>(T t, string propertyName, Action<T, string> setter)
            where T : class, new()
        {
            if (t == null)
            {
                var tmp = GetExtensionXAddr(propertyName);
                if (string.IsNullOrEmpty(tmp))
                    return null;
                t = new T();
                setter(t, tmp);
            }
            return t;
        }

        public async Task ConnectAsync()
        {
            ClientFactory = ClientFactory.GetClientFactory(
                Options.Scheme,
                Options.UserName,
                Options.Password,
                Options.ClientCredentialType);
            var deviceServicePaths = new[] { "/onvif/device_service", "/onvif/service" };
            Exception lastException = null;
            foreach (var deviceServicePath in deviceServicePaths)
            {
                try
                {
                    var deviceClientUri = CreateDeviceClientUri(deviceServicePath);
                    DeviceServiceAddressUri = new Uri(deviceClientUri);
                    DeviceClient = new Device.DeviceClient(ClientFactory, deviceClientUri);
                    //Get Capabilities
                    {
                        var rep = await DeviceClient.GetCapabilitiesAsync(new Device.CapabilityCategory[]
                        {
                          Device.CapabilityCategory.All
                        });
                        Capabilities = rep.Capabilities;
                        handleCapabilitiesItem(Capabilities.Analytics, t => t.XAddr, (t, v) => t.XAddr = v);
                        handleCapabilitiesItem(Capabilities.Device, t => t.XAddr, (t, v) => t.XAddr = v);
                        handleCapabilitiesItem(Capabilities.Events, t => t.XAddr, (t, v) => t.XAddr = v);
                        handleCapabilitiesItem(Capabilities.Imaging, t => t.XAddr, (t, v) => t.XAddr = v);
                        handleCapabilitiesItem(Capabilities.Media, t => t.XAddr, (t, v) => t.XAddr = v);
                        handleCapabilitiesItem(Capabilities.PTZ, t => t.XAddr, (t, v) => t.XAddr = v);

                        if (Capabilities.Extension.Any != null)
                        {
                            for (int i = 0; i < Capabilities.Extension.Any.Length; i++)
                            {
                                var element = Capabilities.Extension.Any[i];
                                if (element == null)
                                {
                                    continue;
                                }
                                foreach (var child in element.ChildNodes)
                                {
                                    XmlElement elXAddr = child as XmlElement;
                                    if (elXAddr == null)
                                        continue;
                                    if (elXAddr.LocalName != "XAddr")
                                        continue;
                                    if (!string.IsNullOrEmpty(elXAddr.InnerText))
                                        elXAddr.InnerText = CorrectUri(elXAddr.InnerText);
                                }
                            }
                        }

                        Capabilities.Extension.AnalyticsDevice =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.AnalyticsDevice,
                            nameof(Capabilities.Extension.AnalyticsDevice), (t, v) => t.XAddr = v);
                        Capabilities.Extension.DeviceIO =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.DeviceIO,
                            nameof(Capabilities.Extension.DeviceIO), (t, v) => t.XAddr = v);
                        Capabilities.Extension.DeviceIO =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.DeviceIO,
                            nameof(Capabilities.Extension.DeviceIO), (t, v) => t.XAddr = v);
                        Capabilities.Extension.Display =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.Display,
                            nameof(Capabilities.Extension.Display), (t, v) => t.XAddr = v);
                        Capabilities.Extension.Receiver =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.Receiver,
                            nameof(Capabilities.Extension.Receiver), (t, v) => t.XAddr = v);
                        Capabilities.Extension.Recording =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.Recording,
                            nameof(Capabilities.Extension.Recording), (t, v) => t.XAddr = v);
                        Capabilities.Extension.Replay =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.Replay,
                            nameof(Capabilities.Extension.Replay), (t, v) => t.XAddr = v);
                        Capabilities.Extension.Search =
                            handleCapabilitiesExtensionItem(Capabilities.Extension.Search,
                            nameof(Capabilities.Extension.Search), (t, v) => t.XAddr = v);
                    }

                    // Some cameras reject GetDeviceInformation with HTTP 400 although
                    // capabilities/media/PTZ endpoints are fully usable.
                    try
                    {
                        DeviceInformation = await DeviceClient.GetDeviceInformationAsync(new Device.GetDeviceInformationRequest());
                    }
                    catch
                    {
                        DeviceInformation = new Device.GetDeviceInformationResponse();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    try
                    {
                        DeviceClient?.Abort();
                    }
                    catch
                    {
                    }
                    DeviceClient = null;
                }
            }

            throw lastException ?? new InvalidOperationException("Failed to connect to ONVIF device service.");
        }

        private string CreateDeviceClientUri(string deviceServicePath)
        {
            UriBuilder deviceClientUriBuilder = new UriBuilder();
            deviceClientUriBuilder.Scheme = Options.Scheme;
            deviceClientUriBuilder.Host = Options.Host;
            deviceClientUriBuilder.Port = Options.Port;
            deviceClientUriBuilder.Path = deviceServicePath;
            return deviceClientUriBuilder.Uri.ToString();
        }

        public string GetExtensionXAddr(string name)
        {
            if (Capabilities.Extension.Any == null)
            {
                return null;
            }
            var xmlElement = Capabilities.Extension.Any.FirstOrDefault(t => t.LocalName == name);
            if (xmlElement == null)
                return null;
            foreach (var child in xmlElement.ChildNodes)
            {
                XmlElement elXAddr = child as XmlElement;
                if (elXAddr == null)
                    continue;
                if (elXAddr.LocalName != "XAddr")
                    continue;
                return elXAddr.InnerText;
            }
            return null;
        }

        public void Dispose()
        {
            DeviceInformation = null;
            Capabilities = null;
            if (DeviceClient != null)
            {
                using (DeviceClient)
                    DeviceClient.Close();
                DeviceClient = null;
            }
            ClientFactory = null;
        }
    }
}
