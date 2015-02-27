﻿using FakeItEasy;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using System.Dynamic;
using System.Linq.Expressions;
using FakeXrmEasy.Extensions;

namespace FakeXrmEasy
{
    public partial class XrmFakedContext : IXrmFakedContext
    {

        #region CRUD
        /// <summary>
        /// A fake retrieve method that will query the FakedContext to retrieve the specified
        /// entity and Guid, or null, if the entity was not found
        /// </summary>
        /// <param name="context">The faked context</param>
        /// <param name="fakedService">The faked service where the Retrieve method will be faked</param>
        /// <returns></returns>
        protected static void FakeRetrieve(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Retrieve(A<string>._, A<Guid>._, A<ColumnSet>._))
                .ReturnsLazily((string entityName, Guid id, ColumnSet columnSet) =>
                {
                    if (string.IsNullOrWhiteSpace(entityName))
                    {
                        throw new InvalidOperationException("The entity logical name must not be null or empty.");
                    }

                    if (id == Guid.Empty)
                    {
                        throw new InvalidOperationException("The id must not be empty.");
                    }

                    if (columnSet == null)
                    {
                        throw new InvalidOperationException("The columnset parameter must not be null.");
                    }

                    if (!context.Data.ContainsKey(entityName))
                        throw new InvalidOperationException(string.Format("The entity logical name {0} is not valid.", entityName));

                    //Entity logical name exists, so , check if the requested entity exists
                    if (context.Data[entityName] != null
                        && context.Data[entityName].ContainsKey(id))
                    {
                        //Entity found => return only the subset of columns specified or all of them
                        if (columnSet.AllColumns)
                            return context.Data[entityName][id];
                        else
                        {
                            //Return the subset of columns requested only
                            var newEntity = new Entity(entityName);
                            newEntity.Id = id;

                            //Add attributes
                            var foundEntity = context.Data[entityName][id];
                            foreach (var column in columnSet.Columns)
                            {
                                newEntity[column] = foundEntity[column];
                            }
                            return newEntity;
                        }
                    }
                    else
                    {
                        //Entity not found in the context => return null
                        return null;
                    }
                });
        }
        /// <summary>
        /// Fakes the Create message
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fakedService"></param>
        protected static void FakeCreate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Create(A<Entity>._))
                .ReturnsLazily((Entity e) =>
                {
                    if (e == null)
                    {
                        throw new InvalidOperationException("The entity must not be null");
                    }

                    if (e.Id != Guid.Empty)
                    {
                        throw new InvalidOperationException("The Id property must not be initialized");
                    }

                    if (string.IsNullOrWhiteSpace(e.LogicalName))
                    {
                        throw new InvalidOperationException("The LogicalName property must not be empty");
                    }

                    //Add entity to the context
                    e.Id = Guid.NewGuid();
                    context.AddEntity(e);

                    return e.Id;
                });

        }

        protected static void FakeUpdate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Update(A<Entity>._))
                .Invokes((Entity e) =>
                {
                    if (e == null)
                    {
                        throw new InvalidOperationException("The entity must not be null");
                    }

                    if (e.Id == Guid.Empty)
                    {
                        throw new InvalidOperationException("The Id property must not be empty");
                    }

                    if (string.IsNullOrWhiteSpace(e.LogicalName))
                    {
                        throw new InvalidOperationException("The LogicalName property must not be empty");
                    }

                    //The entity record must exist in the context
                    if (context.Data.ContainsKey(e.LogicalName) &&
                        context.Data[e.LogicalName].ContainsKey(e.Id))
                    {
                        //Now the entity is the one passed
                        context.Data[e.LogicalName][e.Id] = e;
                    }
                    else
                    {
                        //The entity record was not found, return a CRM-ish update error message
                        throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), string.Format("{0} with Id {1} Does Not Exist", e.LogicalName, e.Id));
                    }
                });

        }

        /// <summary>
        /// Fakes the delete method. Very similar to the Retrieve one
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fakedService"></param>
        protected static void FakeDelete(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Delete(A<string>._, A<Guid>._))
                .Invokes((string entityName, Guid id) =>
                {
                    if (string.IsNullOrWhiteSpace(entityName))
                    {
                        throw new InvalidOperationException("The entity logical name must not be null or empty.");
                    }

                    if (id == Guid.Empty)
                    {
                        throw new InvalidOperationException("The id must not be empty.");
                    }

                    if (!context.Data.ContainsKey(entityName))
                        throw new InvalidOperationException(string.Format("The entity logical name {0} is not valid.", entityName));

                    //Entity logical name exists, so , check if the requested entity exists
                    if (context.Data[entityName] != null
                        && context.Data[entityName].ContainsKey(id))
                    {
                        //Entity found => return only the subset of columns specified or all of them
                        context.Data[entityName].Remove(id);
                    }
                    else
                    {
                        //Entity not found in the context => throw not found exception
                        //The entity record was not found, return a CRM-ish update error message
                        throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(),
                            string.Format("{0} with Id {1} Does Not Exist", entityName, id));
                    }
                });
        }
        #endregion

        #region Other protected methods
        protected void EnsureEntityNameExistsInMetadata(string sEntityName)
        {
            if (!Data.ContainsKey(sEntityName))
            {
                throw new Exception(string.Format("Entity {0} does not exist in the metadata cache", sEntityName));
            };
        }

        protected void ValidateEntity(Entity e)
        {
            //Validate the entity
            if (string.IsNullOrWhiteSpace(e.LogicalName))
            {
                throw new InvalidOperationException("An entity must not have a null or empty LogicalName property.");
            }

            if (e.Id == Guid.Empty)
            {
                throw new InvalidOperationException("An entity with an empty Id can't be added");
            }
        }


        protected internal void AddEntity(Entity e)
        {
            ValidateEntity(e);

            //Add the entity collection
            if (!Data.ContainsKey(e.LogicalName))
            {
                Data.Add(e.LogicalName, new Dictionary<Guid, Entity>());
            }

            if (Data[e.LogicalName].ContainsKey(e.Id))
            {
                Data[e.LogicalName][e.Id] = e;
            }
            else
            {
                Data[e.LogicalName].Add(e.Id, e);
            }

            //Update metadata for that entity
            if (!AttributeMetadata.ContainsKey(e.LogicalName))
                AttributeMetadata.Add(e.LogicalName, new Dictionary<string, string>());

            //Update attribute metadata
            foreach (var attKey in e.Attributes.Keys)
            {
                if (!AttributeMetadata[e.LogicalName].ContainsKey(attKey))
                    AttributeMetadata[e.LogicalName].Add(attKey, attKey);
            }
        }

        protected internal bool AttributeExistsInMetadata(string sEntityName, string sAttributeName)
        {
            return AttributeMetadata.ContainsKey(sEntityName) &&
                    AttributeMetadata[sEntityName].ContainsKey(sAttributeName);
        }
#endregion

    }
}