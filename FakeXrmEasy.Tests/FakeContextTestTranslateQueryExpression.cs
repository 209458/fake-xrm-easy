﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FakeItEasy;
using FakeXrmEasy;
using Xunit;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Crm;
using Microsoft.Xrm.Sdk.Messages;  //TypedEntities generated code for testing

namespace FakeXrmEasy.Tests
{
    public class FakeContextTestTranslateQueryExpression
    {
        [Fact]
        public void When_translating_a_null_query_expression_the_linq_query_is_also_null()
        {
            var context = new XrmFakedContext();
            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, null);
            Assert.True(result == null);
        }

        [Fact]
        public void When_translating_a_query_from_a_non_existing_entity_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            IQueryable<Entity> data = new List<Entity>() {
                new Entity("account") { Id = guid1 },
                new Entity("account") { Id = guid2 },
                new Entity("contact") { Id = guid3 }
            }.AsQueryable();

            context.Initialize(data);
            var qe = new QueryExpression() { EntityName = "nonexistingentityname"};
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateQueryExpressionToLinq(context, qe));
        }

        

        [Fact]
        public void When_doing_a_crm_linq_query_a_retrievemultiple_with_a_queryexpression_is_called()
        {
            var fakedContext = new XrmFakedContext();
            var guid = Guid.NewGuid();
            fakedContext.Initialize(new List<Entity>() {
                new Contact() { Id = guid, FirstName = "Jordi" }
            });

            var service = fakedContext.GetFakedOrganizationService();

            using(XrmServiceContext ctx = new XrmServiceContext(service)) {
                var contact = (from c in ctx.CreateQuery<Contact>()
                             where c.FirstName.Equals("Jordi")
                            select c).FirstOrDefault();

                
            }
            A.CallTo(() => service.Execute(A<OrganizationRequest>.That.Matches(x => x is RetrieveMultipleRequest && ((RetrieveMultipleRequest) x).Query is QueryExpression))).MustHaveHappened(); 
        }
        [Fact]
        public void When_executing_a_query_expression_with_a_simple_join_right_result_is_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2";
            
            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2});

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner,
                    Columns = new ColumnSet(new string[] { "name" })
                }
            );
            qe.ColumnSet = new ColumnSet(new string[] { "fullname", "parentcustomerid" });

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2); //2 Contacts related to the same account
            var firstContact = result.FirstOrDefault();
            var secondContact = result.LastOrDefault();

            Assert.True(firstContact.Attributes.Count == 3);
            Assert.True(secondContact.Attributes.Count == 3);

            Assert.True(firstContact["fullname"].Equals(contact1["fullname"]));
            Assert.True((firstContact["account.name"] as AliasedValue).Value.Equals(account["name"]));

            Assert.True(secondContact["fullname"].Equals(contact2["fullname"]));
            Assert.True((secondContact["account.name"] as AliasedValue).Value.Equals(account["name"]));

        }
        [Fact]
        public void When_executing_a_query_expression_with_a_left_join_all_left_hand_side_elements_are_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            //Contact3 doesnt have a parent customer, but must be returned anyway (left outer)

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.LeftOuter,
                    Columns = new ColumnSet(new string[] { "name" })
                }
            );
            qe.ColumnSet = new ColumnSet(new string[] { "fullname", "parentcustomerid" });

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 3); //2 Contacts related to the same account + 1 contact without parent account
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact["fullname"].Equals(contact1["fullname"]));
            Assert.True(lastContact["fullname"].Equals(contact3["fullname"]));
        }

        [Fact]
        public void When_executing_a_query_expression_join_with_orphans_these_are_not_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            //Contact3 doesnt have a parent customer, but must be returned anyway (left outer)

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner,
                    Columns = new ColumnSet(new string[] { "name" })
                }
            );
            qe.ColumnSet = new ColumnSet(new string[] { "fullname", "parentcustomerid" });

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2); 
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact["fullname"].Equals(contact1["fullname"]));
            Assert.True(lastContact["fullname"].Equals(contact2["fullname"]));
        }

        [Fact]
        public void When_executing_a_query_expression_only_the_selected_columns_in_the_columnset_are_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner,
                    Columns = new ColumnSet(new string[] { "name" })
                }
            );

            //We only select fullname and parentcustomerid, firstname should not be included
            qe.ColumnSet = new ColumnSet(new string[] { "fullname", "parentcustomerid" });

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2);
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.False(firstContact.Attributes.ContainsKey("firstname"));
            Assert.False(lastContact.Attributes.ContainsKey("firstname"));
        }

        [Fact]
        public void When_executing_a_query_expression_with_all_attributes_all_of_them_are_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner
                }
            );

            //We only select fullname and parentcustomerid, firstname should not be included
            qe.ColumnSet = new ColumnSet(true);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2);
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact.Attributes.Count == 3); //Contact 1
            Assert.True(lastContact.Attributes.Count == 3);  //Contact 2
        }

        [Fact]
        public void When_executing_a_query_expression_without_columnset_no_attributes_are_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner
                }
            );

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2);
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact.Attributes.Count == 0); //Contact 1
            Assert.True(lastContact.Attributes.Count == 0);  //Contact 2
        }

        [Fact]
        public void When_executing_a_query_expression_with_a_columnset_in_a_linkedentity_attribute_is_returned_with_a_prefix()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner,
                    Columns = new ColumnSet(new string[] { "name" } )
                }
            );

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2);
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact.Attributes.Count == 1); //Contact 1
            Assert.True(lastContact.Attributes.Count == 1);  //Contact 2

            Assert.True(firstContact.Attributes.ContainsKey("account.name")); //Contact 1
            Assert.True(lastContact.Attributes.ContainsKey("account.name"));  //Contact 2

            Assert.True(firstContact.Attributes["account.name"] is AliasedValue); //Contact 1
            Assert.True(lastContact.Attributes["account.name"] is AliasedValue);  //Contact 2
        }

        [Fact]
        public void When_executing_a_query_expression_with_a_columnset_in_a_linkedentity_attribute_is_returned_with_an_alias()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner,
                    Columns = new ColumnSet(new string[] { "name" }),
                    EntityAlias = "myCoolAlias"
                }
            );

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 2);
            var firstContact = result.FirstOrDefault();
            var lastContact = result.LastOrDefault();

            Assert.True(firstContact.Attributes.Count == 1); //Contact 1
            Assert.True(lastContact.Attributes.Count == 1);  //Contact 2

            Assert.True(firstContact.Attributes.ContainsKey("myCoolAlias.name")); //Contact 1
            Assert.True(lastContact.Attributes.ContainsKey("myCoolAlias.name"));  //Contact 2

            Assert.True(firstContact.Attributes["myCoolAlias.name"] is AliasedValue); //Contact 1
            Assert.True(lastContact.Attributes["myCoolAlias.name"] is AliasedValue);  //Contact 2
        }


        #region Filters
        [Fact]
        public void When_executing_a_query_expression_with_simple_equals_condition_expression_returns_right_number_of_results()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Contact 3"; contact3["firstname"] = "First 3";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Account 1";

            contact1["parentcustomerid"] = account.ToEntityReference(); //Both contacts are related to the same account
            contact2["parentcustomerid"] = account.ToEntityReference();

            context.Initialize(new List<Entity>() { account, contact1, contact2, contact3 });

            var qe = new QueryExpression() { EntityName = "contact" };
            qe.LinkEntities.Add(
                new LinkEntity()
                {
                    LinkFromEntityName = "contact",
                    LinkToEntityName = "account",
                    LinkFromAttributeName = "parentcustomerid",
                    LinkToAttributeName = "accountid",
                    JoinOperator = JoinOperator.Inner
                }
            );
            qe.ColumnSet = new ColumnSet(true);
            //Filter criteria
            qe.Criteria = new FilterExpression()
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("fullname", ConditionOperator.Equal, "Contact 1")
                }
            };

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe);

            Assert.True(result.Count() == 1);
            var firstContact = result.FirstOrDefault();
            Assert.True(firstContact["fullname"].Equals("Contact 1"));
        }
        #endregion

    }
}
