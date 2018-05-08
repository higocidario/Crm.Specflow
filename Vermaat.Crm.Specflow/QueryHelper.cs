﻿using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Vermaat.Crm.Specflow
{
    static class QueryHelper
    {
        public static bool HasOpenSystemJobs(Guid regardingId, CrmService service)
        {
            QueryExpression qe = new QueryExpression("asyncoperation")
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1
            };
            qe.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, regardingId);
            qe.Criteria.AddCondition("statuscode", ConditionOperator.NotIn, new object[] { 10, 30, 31, 32 });

            return service.RetrieveMultiple(qe).Entities.Count > 0;
        }

        public static QueryExpression CreateQueryExpressionFromTable(string entityName, Table criteria, CrmTestingContext context)
        {
            QueryExpression qe = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet()
            };

            foreach (var row in criteria.Rows)
            {
                var crmValue = ObjectConverter.ToCrmPrimitive(entityName, row["Property"], row["Value"], context);
                qe.Criteria.AddCondition(row["Property"], ConditionOperator.Equal, crmValue);
            }

            return qe;
        }
    }
}