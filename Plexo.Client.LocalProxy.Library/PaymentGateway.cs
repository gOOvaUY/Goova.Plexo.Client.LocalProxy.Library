using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using Plexo.Client.LocalProxy.Library.Logging;
using Plexo.Client.SDK;

namespace Plexo.Client.LocalProxy.Library
{
    public class PaymentGateway : IPaymentGateway
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly PaymentGatewayClient _cl = PaymentGatewayClientFactory.GetClient(Properties.Settings.Default.ClientName);

        public async Task<ServerResponse<string>> Authorize(Authorization authorization)
        {
            return await OnlyRunOnIntranet(() => _cl.Authorize(authorization));
        }

        public async Task<ServerResponse<List<IssuerInfo>>> GetSupportedIssuers()
        {
            return await OnlyRunOnIntranet(() => _cl.GetSupportedIssuers());
        }

        public async Task<ServerResponse<Transaction>> Purchase(PaymentRequest payment)
        {
            return await OnlyRunOnIntranet(() => _cl.Purchase(payment));
        }

        public async Task<ServerResponse<Transaction>> Cancel(CancelRequest payment)
        {
            return await OnlyRunOnIntranet(() => _cl.Cancel(payment));
        }

        private async Task<ServerResponse<T>> OnlyRunOnIntranet<T>(Func<Task<ServerResponse<T>>> func)
        {
            try
            {
                string ip = null;
                MessageProperties prop = OperationContext.Current?.IncomingMessageProperties;
                if (prop != null)
                {
                    if (prop.ContainsKey(RemoteEndpointMessageProperty.Name))
                    {
                        RemoteEndpointMessageProperty endpoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                        if (endpoint != null)
                            ip = endpoint.Address;
                    }
                }
                if (ip == null)
                    ip = HttpContext.Current.Request.UserHostAddress;
                if (ip == null)
                    return new ServerResponse<T> { ErrorMessage = "Unable to get incoming ip", ResultCode = ResultCodes.SystemError };
                if (IsIntranet(ip))
                    return await func();
                return new ServerResponse<T> { ErrorMessage = "Not allowed to request from Internet", ResultCode = ResultCodes.Forbidden };
            }
            catch (Exception e)
            {
                Logger.ErrorException("System Error",e);
                return new ServerResponse<T> {ErrorMessage = "System Error, see error log for more detailes", ResultCode = ResultCodes.SystemError};
            }

        }

        private bool IsIntranet(string ip)
        {
            if (ip.StartsWith("172."))
            {
                string[] parts = ip.Split('.');
                int sec;
                if (parts.Length > 1 && int.TryParse(parts[1], out sec))
                {
                    if (sec >= 16 && sec <= 32)
                        return true;
                }
            }
            if (ip.StartsWith("127.0.0.") || ip == "::1" || ip.StartsWith("10.") || ip.StartsWith("192.168."))
                return true;
            if (ip.Contains(":") && (ip.StartsWith("fc") || ip.StartsWith("fd")))
                return true;
            return false;
        }
    }
}
