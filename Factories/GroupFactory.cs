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
    public class GroupFactory : GenericFactory<group>
    {
        protected override string singularEntityName { get { return "group"; } }
        protected override string pluralEntityName { get { return "groups"; } }

        public GroupFactory(string BaseUrl, string AuthenticationKey)
            : base(BaseUrl, AuthenticationKey)
        {
        }
    }
}
