﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class AssociateRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AssociateRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var associateRequest = request as AssociateRequest;
            var service = ctx.GetFakedOrganizationService();

            if (associateRequest == null)
            {
                throw new Exception("Only associate request can be processed!");
            }

            var associateRelationship = associateRequest.Relationship;
            var relationShipName = associateRelationship.SchemaName;
            var fakeRelationShip = ctx.GetRelationship(relationShipName);

            if (fakeRelationShip == null)
            {
                throw new Exception(string.Format("Relationship {0} does not exist in the metadata cache", relationShipName));
            }

            if (associateRequest.Target == null)
            {
                throw new Exception("Association without target is invalid!");
            }
           


            foreach (var relatedEntityReference in associateRequest.RelatedEntities)
            {
                if (fakeRelationShip.RelationshipType == XrmFakedRelationship.enmFakeRelationshipType.ManyToMany)
                {
                    //Check records exist
                    var targetExists = ctx.CreateQuery(fakeRelationShip.Entity1LogicalName)
                                                .Where(e => e.Id == associateRequest.Target.Id)
                                                .FirstOrDefault() != null;

                    if(!targetExists)
                    {
                        throw new Exception(string.Format("{0} with Id {1} doesn't exist", fakeRelationShip.Entity1LogicalName, associateRequest.Target.Id.ToString()));
                    }

                    var relatedExists = ctx.CreateQuery(fakeRelationShip.Entity2LogicalName)
                                                .Where(e => e.Id == relatedEntityReference.Id)
                                                .FirstOrDefault() != null;

                    if (!relatedExists)
                    {
                        throw new Exception(string.Format("{0} with Id {1} doesn't exist", fakeRelationShip.Entity2LogicalName, relatedEntityReference.Id.ToString()));
                    }

                    var association = new Entity(fakeRelationShip.IntersectEntity)
                    {
                        Attributes = new AttributeCollection
                        {
                            { fromAttribute, associateRequest.Target.Id },
                            { toAttribute, relatedEntityReference.Id }
                        }
                    };

                    service.Create(association);
                }
                else
                {
                    //One to many
                    //Get entity to update
                    var entityToUpdate = new Entity(relatedEntityReference.LogicalName)
                    {
                        Id = relatedEntityReference.Id
                    };

                    entityToUpdate[fakeRelationShip.Entity2Attribute] = associateRequest.Target;
                    service.Update(entityToUpdate);
                }


            }

            return new AssociateResponse ();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(AssociateRequest);
        }
    }
}
