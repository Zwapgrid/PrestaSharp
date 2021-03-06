﻿using Bukimedia.PrestaSharp.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Bukimedia.PrestaSharp.Factories
{
    public class CountryFactory : GenericFactory<country>
    {
        protected override string singularEntityName { get { return "country"; } }
        protected override string pluralEntityName { get { return "countries"; } }

        public CountryFactory(string BaseUrl, string AuthenticationKey)
            : base(BaseUrl, AuthenticationKey)
        {
        }
    }
}
