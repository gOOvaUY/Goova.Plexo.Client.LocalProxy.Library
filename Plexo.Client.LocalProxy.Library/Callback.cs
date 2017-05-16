using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Plexo.Client.LocalProxy.Library.Logging;
using RestSharp;

// ReSharper disable InconsistentNaming

namespace Plexo.Client.LocalProxy.Library
{
    internal class Callback : ICallback //This will be loaded/injected by reflection
    {
        private readonly WSDLCallback _wsdlclient;
        private readonly RestClient _restclient;

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();


        public Callback()
        {
            if (Properties.Settings.Default.CallbackType == 0) //webservice based callback
            {
                _wsdlclient = WSDLCallback.Create();
                _restclient = null;
            }
            else //Post based callback
            {
                _wsdlclient = null;
                _restclient=new RestClient(Properties.Settings.Default.CallbackUrl);
            }

        }

        public async Task<ClientResponse> Instrument(IntrumentCallback instrument)
        {
            try
            {

                if (_wsdlclient != null)
                {
                    return await _wsdlclient.Instrument(instrument);
                }
                if (_restclient != null)
                {
                    RestRequest req = new RestRequest(Method.POST);
                    req.AddJsonBody(instrument);
                    req.RequestFormat = DataFormat.Json;
                    IRestResponse<ClientResponse> resp = await _restclient.ExecuteTaskAsync<ClientResponse>(req);
                    if (resp.Data == null)
                        return new ClientResponse {ErrorMessage = "Error executing callback: " + (resp.ErrorMessage ?? "Unknown Error"), ResultCode = ResultCodes.ClientServerError};
                    return resp.Data;
                }
                return new ClientResponse {ErrorMessage = "Error executing callback, callback is not configured properly", ResultCode = ResultCodes.SystemError};
            }
            catch (Exception e)
            {
                Logger.ErrorException("Error executing callback", e);
                return new ClientResponse { ErrorMessage = "Internal Error executing callback", ResultCode = ResultCodes.SystemError };
            }
        }


    }

    public class WSDLCallback : ClientBase<ICallback>, ICallback
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private WSDLCallback(ServiceEndpoint enp) : base(enp)
        {
        }
        public Task<ClientResponse> Instrument(IntrumentCallback instrument)
        {
            return Channel.Instrument(instrument);
        }
        public static WSDLCallback Create()
        {

            try
            {
                int timeout = Properties.Settings.Default.Timeout;
                WebHttpBinding binding = new WebHttpBinding();
                binding.OpenTimeout = TimeSpan.FromSeconds(timeout);
                binding.CloseTimeout = TimeSpan.FromSeconds(timeout);
                binding.SendTimeout = TimeSpan.FromSeconds(timeout);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(timeout);
                if (Properties.Settings.Default.CallbackUrl.StartsWith("https"))
                    binding.Security.Mode = WebHttpSecurityMode.Transport;

                ServiceEndpoint svc = new ServiceEndpoint(ContractDescription.GetContract(typeof(ICallback)),
                    binding, new EndpointAddress(Properties.Settings.Default.CallbackUrl));
                WebHttpBehavior behavior = new WebHttpBehavior
                {
                    DefaultBodyStyle = WebMessageBodyStyle.Bare,
                    DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                    DefaultOutgoingResponseFormat = WebMessageFormat.Json,
                    HelpEnabled = true
                };
                svc.Behaviors.Add(behavior);
                return new WSDLCallback(svc);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Unable to create WSDLCallbackClient", e);
                throw;
            }

        }
    }
}