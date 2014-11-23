﻿using System;
using System.Linq;

using Xunit;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk.Query;

using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Tests
{
    public class FakeXrmEasyTestRetrieve
    {
        [Fact]
        public void When_retrieve_is_invoked_with_an_empty_logical_name_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve(null,Guid.Empty,new ColumnSet()));
            Assert.Equal(ex.Message, "The entity logical name must not be null or empty.");

            ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve("", Guid.Empty, new ColumnSet()));
            Assert.Equal(ex.Message, "The entity logical name must not be null or empty.");

            ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve("     ", Guid.Empty, new ColumnSet()));
            Assert.Equal(ex.Message, "The entity logical name must not be null or empty.");
        }

        [Fact]
        public void When_retrieve_is_invoked_with_an_empty_guid_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve("account", Guid.Empty, new ColumnSet()));
            Assert.Equal(ex.Message, "The id must not be empty.");
        }

        [Fact]
        public void When_retrieve_is_invoked_with_a_null_columnset_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve("account", Guid.NewGuid(), null));
            Assert.Equal(ex.Message, "The columnset parameter must not be null.");
        }

        [Fact]
        public void When_retrieve_is_invoked_with_a_non_existing_logical_name_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
                
            var service = context.GetFakedOrganizationService();

            var ex = Assert.Throws<InvalidOperationException>(() => service.Retrieve("account", Guid.NewGuid(), null));
            Assert.Equal(ex.Message, "The columnset parameter must not be null.");
        }

        [Fact]
        public void When_retrieve_is_invoked_with_non_existing_entity_null_is_returned()
        {
            var context = new XrmFakedContext();

            //Initialize the context with a single entity
            var guid = Guid.NewGuid();
            var data = new List<Entity>() {
                new Entity("account") { Id = guid }
            }.AsQueryable();

            context.Initialize(data);

            var service = context.GetFakedOrganizationService();

            var result = service.Retrieve("account", Guid.NewGuid(), new ColumnSet());
            Assert.Equal(result, null);
        }

        [Fact]
        public void When_retrieve_is_invoked_with_an_existing_entity_that_entity_is_returned()
        {
            var context = new XrmFakedContext();

            //Initialize the context with a single entity
            var guid = Guid.NewGuid();
            var data = new List<Entity>() {
                new Entity("account") { Id = guid }
            }.AsQueryable();

            context.Initialize(data);

            var service = context.GetFakedOrganizationService();

            var result = service.Retrieve("account", guid, new ColumnSet());
            Assert.Equal(result.Id, data.FirstOrDefault().Id);
        }

        [Fact]
        public void When_retrieve_is_invoked_with_an_existing_entity_and_all_columns_the_exact_entity_is_returned()
        {
            var context = new XrmFakedContext();

            //Initialize the context with a single entity
            var guid = Guid.NewGuid();
            var data = new List<Entity>() {
                new Entity("account") { Id = guid }
            }.AsQueryable();

            context.Initialize(data);

            var service = context.GetFakedOrganizationService();

            var result = service.Retrieve("account", guid, new ColumnSet(true));
            Assert.Equal(result, data.FirstOrDefault());
        }

        [Fact]
        public void When_retrieve_is_invoked_with_an_existing_entity_and_only_one_column_only_that_one_is_retrieved()
        {
            var context = new XrmFakedContext();

            //Initialize the context with a single entity
            var guid = Guid.NewGuid();
            var entity = new Entity("account") { Id = guid };
            entity["name"] = "Test account";
            entity["createdon"] = DateTime.UtcNow;

            var data = new List<Entity>() { entity }.AsQueryable();
            context.Initialize(data);

            var service = context.GetFakedOrganizationService();

            var result = service.Retrieve("account", guid, new ColumnSet(new string [] {"name"}));
            Assert.Equal(result.Id, data.FirstOrDefault().Id);
            Assert.True(result.Attributes.Count == 1);
            Assert.Equal(result["name"], "Test account");
        }
    }
}
