﻿using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Vermaat.Crm.Specflow
{
    public static class ObjectConverter
    {
        public static object ToCrmObject(string entityName, string attributeName, string value, CrmTestingContext context)
        {
            return ToCrmObject(entityName, attributeName, value, context, false);
        }

        public static object ToCrmPrimitive(string entityName, string attributeName, string value, CrmTestingContext context)
        {
            return ToCrmObject(entityName, attributeName, value, context, true);
        }

        public static SetStateRequest ToSetStateRequest(EntityReference target, string desiredstatus, CrmTestingContext context)
        {
            var attributeMd = context.Metadata.GetAttributeMetadata(target.LogicalName, Constants.General.STATUSCODE) as StatusAttributeMetadata;
            var optionMd = attributeMd.OptionSet.Options.Where(o => o.Label.IsLabel(context.LanguageCode, desiredstatus)).FirstOrDefault() as StatusOptionMetadata;

            return new SetStateRequest()
            {
                EntityMoniker = target,
                State = new OptionSetValue(optionMd.State.Value),
                Status = new OptionSetValue(optionMd.Value.Value),
            };
        }




        private static object ToCrmObject(string entityName, string attributeName, string value, CrmTestingContext context, bool primitive)
        {
            var metadata = context.Metadata.GetAttributeMetadata(entityName, attributeName);
            switch (metadata.AttributeType)
            {
                case AttributeTypeCode.Boolean: return bool.Parse(value);
                case AttributeTypeCode.DateTime: return DateTime.Parse(value);
                case AttributeTypeCode.Double: return double.Parse(value);
                case AttributeTypeCode.Decimal: return decimal.Parse(value);
                case AttributeTypeCode.Integer: return int.Parse(value);
                case AttributeTypeCode.String: return value;

                case AttributeTypeCode.Money:
                    if (primitive)
                        return decimal.Parse(value);
                    else
                        return new Money(decimal.Parse(value));

                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    var optionSet = GetOptionSetValue(metadata, value, context);
                    if (primitive)
                        return optionSet.Value;
                    else
                        return optionSet;

                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    var lookup = GetLookupValue(metadata, value, context);
                    if (primitive)
                        return lookup.Id;
                    else
                        return lookup;

                

                default: throw new NotImplementedException(string.Format("Type {0} not implemented", metadata.AttributeType));
            }
        }

        private static EntityReference GetLookupValue(AttributeMetadata metadata, string alias, CrmTestingContext context)
        {
            var result = context.RecordCache.Get(alias);
            if (result != null)
                return result;

            var lookupMd = (LookupAttributeMetadata)metadata;
            string targetEntity = lookupMd.Targets[0];

            var targetMd = context.Metadata.GetEntityMetadata(targetEntity);

            QueryExpression qe = new QueryExpression(targetEntity)
            {
                ColumnSet = new ColumnSet(false)
            };
            qe.Criteria.AddCondition(targetMd.PrimaryNameAttribute, ConditionOperator.Equal, alias);
            var col = context.Service.RetrieveMultiple(qe);

            Assert.AreEqual(1, col.Entities.Count);
            return col.Entities.First().ToEntityReference();
        }

        private static OptionSetValue GetOptionSetValue(AttributeMetadata metadata, string value, CrmTestingContext context)
        {
            var optionMd = metadata as EnumAttributeMetadata;

            var option = optionMd.OptionSet.Options.Where(o => o.Label.IsLabel(context.LanguageCode, value)).FirstOrDefault();

            Assert.IsNotNull(option, $"Option {value} not found. AvailaleOptions: { string.Join(", ", optionMd.OptionSet.Options.Select(o => o.Label?.UserLocalizedLabel?.Label))}");
            Assert.IsTrue(option.Value.HasValue);

            return new OptionSetValue(option.Value.Value);
        }
    }
}
