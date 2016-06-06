﻿using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class PublishXmlRequestExecutor: IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is PublishXmlRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as PublishXmlRequest;

            if(string.IsNullOrWhiteSpace(req.ParameterXml))
            {
                throw new Exception(string.Format("ParameterXml property must not be blank."));
            }
            return new PublishXmlResponse()
            {

            };
        }
    }
}
