﻿using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// This will test that a fetchxml is correctly translated into a QueryExpression
    /// which was already tested
    /// 
    /// We'll leave FetchXml aggregations for a later version
    /// </summary>
    public class FakeContextTestFetchXmlTranslation
    {
        [Fact]
        public void When_translating_a_fetch_xml_expression_fetchxml_must_be_an_xml()
        {
            var ctx = new XrmFakedContext();

            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "this is not an xml"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_first_node_must_be_a_fetch_element_otherwise_exception_is_thrown()
        {
            var ctx = new XrmFakedContext();

            Assert.DoesNotThrow(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'></entity></fetch>"));
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<attribute></attribute>"));
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<entity></entity>"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_entity_node_must_have_a_name_attribute()
        {
            var ctx = new XrmFakedContext();

            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity></entity></fetch>"));
            Assert.DoesNotThrow(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'></entity></fetch>"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_attribute_node_must_have_a_name_attribute()
        {
            var ctx = new XrmFakedContext();

            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><attribute></attribute></entity></fetch>"));
            Assert.DoesNotThrow(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><attribute name='firstname'></attribute></entity></fetch>"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_order_node_must_have_2_attributes()
        {
            var ctx = new XrmFakedContext();

            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><order></order></entity></fetch>"));
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><order attribute=''></order></entity></fetch>"));
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><order descending=''></order></entity></fetch>"));
            Assert.DoesNotThrow(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<fetch><entity name='contact'><order attribute='firstname' descending='true'></order></entity></fetch>"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_unknown_elements_throw_an_exception()
        {
            var ctx = new XrmFakedContext();
            Assert.Throws<Exception>(() => XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, "<thisdoesntexist></thisdoesntexist>"));
        }
        
        [Fact]
        public void When_translating_a_fetch_xml_expression_queryexpression_name_matches_entity_node()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                              </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.EntityName.Equals("contact"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_attributes_are_translated_to_a_list_of_columns()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                              </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.ColumnSet != null);
            Assert.Equal(false, query.ColumnSet.AllColumns);
            Assert.Equal(3, query.ColumnSet.Columns.Count);
            Assert.True(query.ColumnSet.Columns.Contains("fullname"));
            Assert.True(query.ColumnSet.Columns.Contains("telephone1"));
            Assert.True(query.ColumnSet.Columns.Contains("contactid"));
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_all_attributes_is_translated_to_a_columnset_with_all_columns()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <all-attributes />
                              </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.ColumnSet != null);
            Assert.Equal(true, query.ColumnSet.AllColumns);
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_orderby_ascending_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                    <order attribute='fullname' descending='false' />
                              </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Orders != null);
            Assert.Equal(1, query.Orders.Count);
            Assert.Equal("fullname", query.Orders[0].AttributeName);
            Assert.Equal(Microsoft.Xrm.Sdk.Query.OrderType.Ascending, query.Orders[0].OrderType);
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_orderby_descending_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                    <order attribute='fullname' descending='true' />
                              </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Orders != null);
            Assert.Equal(1, query.Orders.Count);
            Assert.Equal("fullname", query.Orders[0].AttributeName);
            Assert.Equal(Microsoft.Xrm.Sdk.Query.OrderType.Descending, query.Orders[0].OrderType);
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_2_orderby_elements_are_translated_correctly()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                    <order attribute='fullname' descending='true' />
                                    <order attribute = 'telephone1' descending = 'false' />
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Orders != null);
            Assert.Equal(2, query.Orders.Count);
            Assert.Equal("fullname", query.Orders[0].AttributeName);
            Assert.Equal(OrderType.Descending, query.Orders[0].OrderType);
            Assert.Equal("telephone1", query.Orders[1].AttributeName);
            Assert.Equal(OrderType.Ascending, query.Orders[1].OrderType);
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_equal_operator_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='eq' value='Messi' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.Equal, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_not_equal_operator_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='ne' value='Messi' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.NotEqual, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_contains_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='like' value='%Messi%' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.Like, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_beginswith_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='like' value='Messi%' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.BeginsWith, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_endswith_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='like' value='%Messi' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.EndsWith, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_does_not_contain_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='not-like' value='%Messi%' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.NotLike, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_does_not_beginwith_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='not-like' value='Messi%' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.DoesNotBeginWith, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_condition_with_does_not_end_with_operator_result_is_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='not-like' value='%Messi' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("fullname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.DoesNotEndWith, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Messi", query.Criteria.Conditions[0].Values[0].ToString());
        }

        [Fact]
        public void When_translating_a_fetch_xml_expression_nested_filters_are_correct()
        {
            var ctx = new XrmFakedContext();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='fullname' operator='not-like' value='%Messi' />
                                                <filter type='or'>
                                                    <condition attribute='telephone1' operator='eq' value='123' />
                                                    <condition attribute='telephone1' operator='eq' value='234' />
                                                </filter>
                                        </filter>
                                  </entity>
                            </fetch>";

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal(1, query.Criteria.Filters.Count);
            Assert.Equal(LogicalOperator.Or, query.Criteria.Filters[0].FilterOperator);
            Assert.Equal(2, query.Criteria.Filters[0].Conditions.Count);
        }

        [Fact]
        public void When_executing_fetchxml_right_result_is_returned()
        {
            //This will test a query expression is generated and executed

            var ctx = new XrmFakedContext();
            ctx.Initialize(new List<Entity>()
            {
                new Contact() {Id = Guid.NewGuid(), FirstName = "Leo Messi", Telephone1 = "123" }, //should be returned
                new Contact() {Id = Guid.NewGuid(), FirstName = "Leo Messi", Telephone1 = "234" }, //should be returned
                new Contact() {Id = Guid.NewGuid(), FirstName = "Leo", Telephone1 = "789" }, //shouldnt
                new Contact() {Id = Guid.NewGuid(), FirstName = "Andrés", Telephone1 = "123" }, //shouldnt
            });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='telephone1' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='firstname' operator='like' value='%Leo%' />
                                                <filter type='or'>
                                                    <condition attribute='telephone1' operator='eq' value='123' />
                                                    <condition attribute='telephone1' operator='eq' value='234' />
                                                </filter>
                                        </filter>
                                  </entity>
                            </fetch>";


            var retrieveMultiple = new RetrieveMultipleRequest()
            {
                Query = new FetchExpression(fetchXml)
            };

            var service = ctx.GetFakedOrganizationService();
            var response = service.Execute(retrieveMultiple) as RetrieveMultipleResponse;

            Assert.Equal(2, response.EntityCollection.Entities.Count);
        }


    }
}
