using QuickNV.Onvif.Factorys;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace QuickNV.Onvif.PTZ
{
    public partial class PTZClient
    {
        private readonly OnvifClient _client;

        public PTZClient(OnvifClient client)
            : this(client.ClientFactory, client.Capabilities.PTZ.XAddr)
        {
            _client = client;
        }

        public PTZClient(ClientFactory factory, string url)
            : base(
                  factory.Binding,
                  new EndpointAddress(url))
        {
            factory.InitClient(this);
        }

        public async Task<PTZStatus> QuickOnvif_GetStatusAsync(string profileToken, CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetStatusAsync(profileToken);
            }
            catch (Exception ex) when (ShouldTryRawFallback(ex))
            {
                if (_client == null)
                    throw;

                var escapedProfileToken = EscapeXml(profileToken);
                var operationBody = $@"
                        <tptz:GetStatus xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                        </tptz:GetStatus>";
                await SendRawPtzSoapAsync(operationBody, cancellationToken);
                return new PTZStatus();
            }
        }

        public async Task QuickOnvif_StopAsync(
            string profileToken,
            bool panTilt,
            bool zoom,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await StopAsync(profileToken, panTilt, zoom);
            }
            catch (Exception ex) when (ShouldTryRawFallback(ex))
            {
                if (_client == null)
                    throw;

                var escapedProfileToken = EscapeXml(profileToken);
                var operationBody = $@"
                        <tptz:Stop xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                            <tptz:PanTilt>{panTilt.ToString().ToLowerInvariant()}</tptz:PanTilt>
                            <tptz:Zoom>{zoom.ToString().ToLowerInvariant()}</tptz:Zoom>
                        </tptz:Stop>";
                await SendRawPtzSoapAsync(operationBody, cancellationToken);
            }
        }

        public async Task QuickOnvif_RelativeMoveAsync(
            string profileToken,
            PTZVector translation,
            PTZSpeed speed,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await RelativeMoveAsync(profileToken, translation, speed);
            }
            catch (Exception ex) when (ShouldTryRawFallback(ex))
            {
                if (_client == null)
                    throw;

                var escapedProfileToken = EscapeXml(profileToken);
                var pan = translation?.PanTilt?.x ?? 0f;
                var tilt = translation?.PanTilt?.y ?? 0f;
                var zoom = translation?.Zoom?.x ?? 0f;
                var speedPan = speed?.PanTilt?.x ?? 0f;
                var speedTilt = speed?.PanTilt?.y ?? 0f;
                var speedZoom = speed?.Zoom?.x ?? 0f;
                var operationBody = $@"
                        <tptz:RelativeMove xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                            <tptz:Translation>
                                <tt:PanTilt x=""{ToInvariant(pan)}"" y=""{ToInvariant(tilt)}"" />
                                <tt:Zoom x=""{ToInvariant(zoom)}"" />
                            </tptz:Translation>
                            <tptz:Speed>
                                <tt:PanTilt x=""{ToInvariant(speedPan)}"" y=""{ToInvariant(speedTilt)}"" />
                                <tt:Zoom x=""{ToInvariant(speedZoom)}"" />
                            </tptz:Speed>
                        </tptz:RelativeMove>";
                await SendRawPtzSoapAsync(operationBody, cancellationToken);
            }
        }

        public async Task QuickOnvif_AbsoluteMoveAsync(
            string profileToken,
            PTZVector position,
            PTZSpeed speed,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AbsoluteMoveAsync(profileToken, position, speed);
            }
            catch (Exception ex) when (ShouldTryRawFallback(ex))
            {
                if (_client == null)
                    throw;

                var escapedProfileToken = EscapeXml(profileToken);
                var pan = position?.PanTilt?.x ?? 0f;
                var tilt = position?.PanTilt?.y ?? 0f;
                var zoom = position?.Zoom?.x ?? 0f;
                var speedPan = speed?.PanTilt?.x ?? 0f;
                var speedTilt = speed?.PanTilt?.y ?? 0f;
                var speedZoom = speed?.Zoom?.x ?? 0f;
                var richOperationBody = $@"
                        <tptz:AbsoluteMove xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                            <tptz:Position>
                                <tt:PanTilt x=""{ToInvariant(pan)}"" y=""{ToInvariant(tilt)}"" space=""http://www.onvif.org/ver10/tptz/PanTiltSpaces/PositionGenericSpace"" />
                                <tt:Zoom x=""{ToInvariant(zoom)}"" />
                            </tptz:Position>
                            <tptz:Speed>
                                <tt:PanTilt x=""{ToInvariant(speedPan)}"" y=""{ToInvariant(speedTilt)}"" />
                                <tt:Zoom x=""{ToInvariant(speedZoom)}"" />
                            </tptz:Speed>
                        </tptz:AbsoluteMove>";
                try
                {
                    await SendRawPtzSoapAsync(richOperationBody, cancellationToken);
                }
                catch (ProtocolException richRequestException) when (IsParameterInvalidFault(richRequestException))
                {
                    var simplifiedOperationBody = $@"
                        <tptz:AbsoluteMove xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                            <tptz:Position>
                                <tt:PanTilt x=""{ToInvariant(pan)}"" y=""{ToInvariant(tilt)}"" space=""http://www.onvif.org/ver10/tptz/PanTiltSpaces/PositionGenericSpace"" />
                            </tptz:Position>
                        </tptz:AbsoluteMove>";
                    await SendRawPtzSoapAsync(simplifiedOperationBody, cancellationToken);
                }
            }
        }

        public async Task QuickOnvif_ContinuousMoveAsync(
            string profileToken,
            PTZSpeed velocity,
            string timeout,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await ContinuousMoveAsync(profileToken, velocity, timeout);
            }
            catch (Exception ex) when (ShouldTryRawFallback(ex))
            {
                if (_client == null)
                    throw;

                var escapedProfileToken = EscapeXml(profileToken);
                var pan = velocity?.PanTilt?.x ?? 0f;
                var tilt = velocity?.PanTilt?.y ?? 0f;
                var zoom = velocity?.Zoom?.x ?? 0f;
                var timeoutElement = string.IsNullOrWhiteSpace(timeout)
                    ? string.Empty
                    : $"<tptz:Timeout>{EscapeXml(timeout)}</tptz:Timeout>";
                var operationBody = $@"
                        <tptz:ContinuousMove xmlns:tptz=""http://www.onvif.org/ver20/ptz/wsdl"">
                            <tptz:ProfileToken>{escapedProfileToken}</tptz:ProfileToken>
                            <tptz:Velocity>
                                <tt:PanTilt x=""{ToInvariant(pan)}"" y=""{ToInvariant(tilt)}"" />
                                <tt:Zoom x=""{ToInvariant(zoom)}"" />
                            </tptz:Velocity>
                            {timeoutElement}
                        </tptz:ContinuousMove>";
                await SendRawPtzSoapAsync(operationBody, cancellationToken);
            }
        }

        private async Task<string> SendRawPtzSoapAsync(string operationBody, CancellationToken cancellationToken)
        {
            if (_client == null)
                throw new InvalidOperationException("Raw PTZ fallback requires an OnvifClient instance.");

            var ptzAddress = _client.Capabilities?.PTZ?.XAddr;
            if (string.IsNullOrWhiteSpace(ptzAddress))
                throw new InvalidOperationException("PTZ service address is not available.");

            var requestUri = _client.CorrectUri(ptzAddress);
            var envelope = BuildSoapEnvelope(
                operationBody,
                _client.Options.UserName,
                _client.Options.Password,
                DateTime.UtcNow);

            using var handler = new HttpClientHandler();
            if (!string.IsNullOrWhiteSpace(_client.Options.UserName))
                handler.Credentials = new NetworkCredential(_client.Options.UserName, _client.Options.Password);
            handler.PreAuthenticate = true;

            using var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
            using var content = new StringContent(envelope, Encoding.UTF8, "application/xml");
            using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new ProtocolException(
                    $"QuickOnvif raw PTZ request failed: {(int)response.StatusCode} ({response.StatusCode}). Body={CreateSnippet(responseBody)}");

            if (HasSoapFault(responseBody, out var faultReason))
                throw new ProtocolException(
                    $"QuickOnvif raw PTZ response contains SOAP fault: {faultReason}");

            return responseBody;
        }

        private static string BuildSoapEnvelope(
            string operationBody,
            string username,
            string password,
            DateTime utcNow)
        {
            var created = utcNow
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var nonceBytes = RandomNumberGenerator.GetBytes(16);
            var nonce = Convert.ToBase64String(nonceBytes);

            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            var digestInput = new byte[nonceBytes.Length + createdBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(nonceBytes, 0, digestInput, 0, nonceBytes.Length);
            Buffer.BlockCopy(createdBytes, 0, digestInput, nonceBytes.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, digestInput, nonceBytes.Length + createdBytes.Length, passwordBytes.Length);
            var passwordDigest = Convert.ToBase64String(SHA1.HashData(digestInput));

            return $@"
                <s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:tds=""http://www.onvif.org/ver10/device/wsdl"" xmlns:tt=""http://www.onvif.org/ver10/schema"">
                    <s:Header xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
                        <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
                            <wsse:UsernameToken>
                                <wsse:Username>{EscapeXml(username)}</wsse:Username>
                                <wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"">{passwordDigest}</wsse:Password>
                                <wsse:Nonce>{nonce}</wsse:Nonce>
                                <wsu:Created>{created}</wsu:Created>
                            </wsse:UsernameToken>
                        </wsse:Security>
                    </s:Header>
                    <s:Body>
                        {operationBody}
                    </s:Body>
                </s:Envelope>";
        }

        private static bool ShouldTryRawFallback(Exception ex)
        {
            if (ex is ProtocolException || ex is FaultException)
                return true;

            var exText = ex.ToString();
            return exText.Contains("Bad Request", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("(400)", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("ActionNotSupported", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("HTTP 400", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("400 Bad Request", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsParameterInvalidFault(ProtocolException protocolException)
        {
            var exText = protocolException.ToString();
            return exText.Contains("Parameters Invalid", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("parameter invalid", StringComparison.OrdinalIgnoreCase)
                || exText.Contains("invalid parameter", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSoapFault(string responseBody, out string faultReason)
        {
            faultReason = string.Empty;
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            try
            {
                var doc = XDocument.Parse(responseBody);
                var faultNode = doc
                    .Descendants()
                    .FirstOrDefault(element => element.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
                if (faultNode == null)
                    return false;

                faultReason = faultNode
                    .Descendants()
                    .FirstOrDefault(element => element.Name.LocalName.Equals("Text", StringComparison.OrdinalIgnoreCase))
                    ?.Value ?? "Unknown SOAP fault";
                return true;
            }
            catch
            {
                return responseBody.Contains("<Fault", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string CreateSnippet(string value, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "<empty>";

            var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= maxLength)
                return normalized;

            return normalized[..maxLength] + "...";
        }

        private static string ToInvariant(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string EscapeXml(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
        }
    }
}
