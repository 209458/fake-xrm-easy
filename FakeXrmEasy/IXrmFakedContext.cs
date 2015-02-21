﻿using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FakeXrmEasy
{
    public interface IXrmFakedContext
    {
        /// <summary>
        /// Receives a list of entities, that are used to initialize the context with those
        /// </summary>
        /// <param name="entities"></param>
        void Initialize(IEnumerable<Entity> entities);

        /// <summary>
        /// Returns a faked organization service that will execute CRUD in-memory operations and other requests against this faked context 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        IOrganizationService GetFakedOrganizationService();

        /// <summary>
        /// Receives a strong-typed entity type and returns a Queryable of that type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> CreateQuery<T>() where T : Entity;

        /// <summary>
        /// Returns a faked plugin that will be executed against this faked context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IPlugin ExecutePluginWithTarget<T>(Entity target) where T : IPlugin, new();
    }

  
}
