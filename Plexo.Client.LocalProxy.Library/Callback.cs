﻿using System;
using System.Threading.Tasks;
using Plexo.Client.LocalProxy.Library.JsonSerializer;
using Plexo.Client.SDK;
using Plexo.Client.SDK.Logging;
using Plexo.Exceptions;
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
                _restclient=NewtonsoftJsonSerializer.CreateClient(Properties.Settings.Default.CallbackUrl);

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
                    req.SetJsonContent(instrument);
                    IRestResponse<ClientResponse> resp = await _restclient.ExecuteTaskAsync<ClientResponse>(req);
                    if (resp.Data == null)
                        throw new ResultCodeException(ResultCodes.ClientServerError, ("en", $"Error executing callback: {resp.StatusCode} {resp.ErrorMessage ?? ""}"), ("es", $"Error ejecutando callback: {resp.StatusCode} {resp.ErrorMessage ?? ""}"));
                    return resp.Data;
                }
                throw new ResultCodeException(ResultCodes.SystemError, ("en", "Error executing callback, callback is not configured properly"), ("es", "Error ejecutando callback, el callback no esta configurado correctamente"));
             }
            catch (Exception e)
            {
                ClientResponse r = new ClientResponse();
                r.PopulateFromException(e,Logger);
                return r;
            }
        }

        public async Task<ClientResponse> Payment(TransactionCallback transaction)
        {
            try
            {

                if (_wsdlclient != null)
                {
                    return await _wsdlclient.Payment(transaction);
                }
                if (_restclient != null)
                {
                    RestRequest req = new RestRequest(Method.POST);
                    req.SetJsonContent(transaction);
                    IRestResponse<ClientResponse> resp = await _restclient.ExecuteTaskAsync<ClientResponse>(req);
                    if (resp.Data == null)
                        throw new ResultCodeException(ResultCodes.ClientServerError, ("en", $"Error executing callback: {resp.StatusCode} {resp.ErrorMessage ?? ""}"), ("es", $"Error ejecutando callback: {resp.StatusCode} {resp.ErrorMessage ?? ""}"));
                    return resp.Data;
                }
                throw new ResultCodeException(ResultCodes.SystemError, ("en", "Error executing callback, callback is not configured properly"), ("es", "Error ejecutando callback, el callback no esta configurado correctamente"));
            }
            catch (Exception e)
            {
                ClientResponse r = new ClientResponse();
                r.PopulateFromException(e, Logger);
                return r;
            }
        }
    }
}