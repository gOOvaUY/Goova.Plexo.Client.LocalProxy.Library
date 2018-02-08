using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using Plexo.Client.SDK;
using Plexo.Client.SDK.Logging;
using Plexo.Exceptions;

namespace Plexo.Client.LocalProxy.Library
{
    public class PaymentGateway : IPaymentGateway
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly PaymentGatewayClient _cl = Properties.Settings.Default.IsIssuerClient ? PaymentGatewayClientFactory.GetIssuer(Properties.Settings.Default.ClientName) : PaymentGatewayClientFactory.GetClient(Properties.Settings.Default.ClientName) ;

        public async Task<ServerResponse<Session>> Authorize(Authorization authorization)
        {
            return await OnlyRunOnIntranet(() => _cl.Authorize(authorization));
        }

        public async Task<ServerResponse> DeleteInstrument(DeleteInstrumentRequest info)
        {
            return await OnlyRunOnIntranet(() => _cl.DeleteInstrument(info));
        }

        public async Task<ServerResponse<PaymentInstrument>> CreateBankInstrument(CreateBankInstrumentRequest request)
        {
            return await OnlyRunOnIntranet(() => _cl.CreateBankInstrument(request));
        }

        public async Task<ServerResponse<List<IssuerInfo>>> GetSupportedIssuers()
        {
            return await OnlyRunOnIntranet(() => _cl.GetSupportedIssuers());
        }

        public async Task<ServerResponse<List<Commerce>>> GetCommerces()
        {
            return await OnlyRunOnIntranet(() => _cl.GetCommerces());
        }

        public async Task<ServerResponse<Commerce>> AddCommerce(CommerceRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.AddCommerce(commerce));
        }

        public async Task<ServerResponse<Commerce>> ModifyCommerce(CommerceModifyRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.ModifyCommerce(commerce));
        }

        public async Task<ServerResponse> DeleteCommerce(CommerceIdRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.DeleteCommerce(commerce));
        }

        public async Task<ServerResponse> SetDefaultCommerce(CommerceIdRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.SetDefaultCommerce(commerce));
        }

        public async Task<ServerResponse<List<IssuerData>>> GetCommerceIssuers(CommerceIdRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.GetCommerceIssuers(commerce));
        }

        public async Task<ServerResponse<IssuerData>> AddIssuerCommerce(IssuerData commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.AddIssuerCommerce(commerce));
        }

        public async Task<ServerResponse> DeleteIssuerCommerce(CommerceIssuerIdRequest commerce)
        {
            return await OnlyRunOnIntranet(() => _cl.DeleteIssuerCommerce(commerce));
        }

        public async Task<ServerResponse<TransactionCursor>> ObtainTransactions(TransactionQuery query)
        {
            return await OnlyRunOnIntranet(() => _cl.ObtainTransactions(query));
        }

        public async Task<ServerResponse<string>> ObtainCSVTransactions(TransactionQuery query)
        {
            return await OnlyRunOnIntranet(() => _cl.ObtainCSVTransactions(query));
        }

        public async Task<ServerResponse<Transaction>> CodeAction(CodeRequest request)
        {
            return await OnlyRunOnIntranet(() => _cl.CodeAction(request));
        }

        public async Task<ServerResponse<Transaction>> Purchase(PaymentRequest payment)
        {
            return await OnlyRunOnIntranet(() => _cl.Purchase(payment));
        }

        public async Task<ServerResponse<Transaction>> Cancel(CancelRequest payment)
        {
            return await OnlyRunOnIntranet(() => _cl.Cancel(payment));
        }

        public async Task<ServerResponse<Transaction>> StartReserve(ReserveRequest payment)
        {
            return await OnlyRunOnIntranet(() => _cl.StartReserve(payment));
        }

        public async Task<ServerResponse<Transaction>> EndReserve(Reserve reserve)
        {
            return await OnlyRunOnIntranet(() => _cl.EndReserve(reserve));

        }

        public async Task<ServerResponse<Transaction>> Status(Reference payment)
        {
            return await OnlyRunOnIntranet(() => _cl.Status(payment));
        }

        public async Task<ServerResponse<List<PaymentInstrument>>> GetInstruments(AuthorizationInfo info)
        {
            return await OnlyRunOnIntranet(() => _cl.GetInstruments(info));
        }

        private async Task<ServerResponse<T>> OnlyRunOnIntranet<T>(Func<Task<ServerResponse<T>>> func)
        {
            if (Properties.Settings.Default.EnableTesting)
                return await func();
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
                    throw new ResultCodeException(ResultCodes.SystemError,("en", "Unable to get incoming ip"), ("es", "No puedo obtener el ip de origen"));
                if (IsIntranet(ip))
                    return await func();
                throw new ResultCodeException(ResultCodes.Forbidden, ("en", "Not allowed to request from Internet"), ("es", "No se puede llamar a este servicio desde internet"));
            }
            catch (Exception e)
            {
                ServerResponse<T> s = new ServerResponse<T>();
                s.PopulateFromException(e,Logger);
                return s;
            }

        }
        private async Task<ServerResponse> OnlyRunOnIntranet(Func<Task<ServerResponse>> func)
        {
            if (Properties.Settings.Default.EnableTesting)
                return await func();
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
                    throw new ResultCodeException(ResultCodes.SystemError, ("en", "Unable to get incoming ip"), ("es", "No puedo obtener el ip de origen"));
                if (IsIntranet(ip))
                    return await func();
                throw new ResultCodeException(ResultCodes.Forbidden, ("en", "Not allowed to request from Internet"), ("es", "No se puede llamar a este servicio desde internet"));
            }
            catch (Exception e)
            {
                ServerResponse s = new ServerResponse();
                s.PopulateFromException(e, Logger);
                return s;
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
